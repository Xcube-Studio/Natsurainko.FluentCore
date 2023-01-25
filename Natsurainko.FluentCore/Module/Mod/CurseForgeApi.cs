using Natsurainko.FluentCore.Model.Install;
using Natsurainko.FluentCore.Model.Mod.CureseForge;
using Natsurainko.Toolkits.Network;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Natsurainko.FluentCore.Module.Mod;

public static class CurseForgeApi
{
    public static Dictionary<string, string> Headers { get; private set; }

    public static readonly int GameId = 432;

    public static readonly string Api = "https://api.curseforge.com/v1/";

    public static void InitApiKey(string key) => Headers = new()
    {
        { "x-api-key", key }
    };

    public static async Task<IEnumerable<CurseForgeCategory>> GetCategories()
    {
        using var responseMessage = await HttpWrapper.HttpGetAsync(Api + "categories" +
            $"?gameId={GameId}", Headers);

        return JObject.Parse(await responseMessage.Content.ReadAsStringAsync())["data"]
            .ToObject<IEnumerable<CurseForgeCategory>>();
    }

    public static async Task<IEnumerable<CurseForgeCategory>> GetCategoriesMain()
        => (await GetCategories())
            .Where(x => x.ClassId.Equals(6))
            .Where(x => !x.Url.Contains("mc-addons"));

    public static async Task<IEnumerable<CurseForgeCategory>> GetCategoriesClassesOnly()
    {
        var categories = (await GetCategories()).Where(x => x.IsClass).ToList();
        categories.Sort((a, b) => a.Name.CompareTo(b.Name));

        return categories;
    }

    public static async Task<IEnumerable<CurseForgeVersionType>> GetVersionTypes()
    {
        using var responseMessage = await HttpWrapper.HttpGetAsync(Api + "games" +
            $"/{GameId}/version-types", Headers);
        responseMessage.EnsureSuccessStatusCode();

        return JObject.Parse(await responseMessage.Content.ReadAsStringAsync())["data"]
            .ToObject<IEnumerable<CurseForgeVersionType>>();
    }

    public static async Task<IEnumerable<CurseForgeVersion>> GetVersions()
    {
        using var responseMessage = await HttpWrapper.HttpGetAsync(Api + "games" +
            $"/{GameId}/versions", Headers);
        responseMessage.EnsureSuccessStatusCode();

        return JObject.Parse(await responseMessage.Content.ReadAsStringAsync())["data"]
            .ToObject<IEnumerable<CurseForgeVersion>>();
    }

    public static async Task<IEnumerable<string>> GetMinecraftVersions()
    {
        var versions = await GetVersions();
        var versionTypes = (await GetVersionTypes())
            .Where(x => x.Name.StartsWith("Minecraft") && !x.Name.EndsWith("Beta"))
            .ToList();
        var minecraftVersions = new List<string>();

        versionTypes.Sort((a, b) => -new Version(a.Name.Split(' ')[1]).CompareTo(new Version(b.Name.Split(' ')[1])));

        foreach (var versionType in versionTypes)
            minecraftVersions.AddRange(versions.Where(x => x.Type.Equals(versionType.Id)).First().Versions);

        return minecraftVersions;
    }

    public static async Task<IEnumerable<CurseForgeResource>> SearchResources(string searchFilter,
        int categoryId = default,
        string gameVersion = default,
        ModLoaderType modLoaderType = ModLoaderType.Any,
        string slug = default,
        int index = default,
        int pageSize = default)
    {
        var stringBuilder = new StringBuilder(Api);
        stringBuilder.Append($"mods/search?gameId={GameId}");
        stringBuilder.Append($"&searchFilter={HttpUtility.UrlEncode(searchFilter)}");
        stringBuilder.Append($"&sortOrder=desc");
        stringBuilder.Append($"&sortField=4");

        if (categoryId != default)
            stringBuilder.Append($"&categoryId={categoryId}");

        if (gameVersion != default)
            stringBuilder.Append($"&gameVersion={gameVersion}");

        if (modLoaderType != default)
            stringBuilder.Append($"&modLoaderType={(int)modLoaderType}");

        if (slug != default)
            stringBuilder.Append($"&slug={slug}");

        if (index != default)
            stringBuilder.Append($"&index={index}");

        if (pageSize != default)
            stringBuilder.Append($"&pageSize={pageSize}");

        using var responseMessage = await HttpWrapper.HttpGetAsync(stringBuilder.ToString(), Headers);
        responseMessage.EnsureSuccessStatusCode();

        return JObject.Parse(await responseMessage.Content.ReadAsStringAsync())["data"]
            .ToObject<IEnumerable<CurseForgeResource>>();
    }

    public static async Task<List<CurseForgeResource>> GetFeaturedResources()
    {
        var result = new List<CurseForgeResource>();

        var content = new JObject
        {
            { "gameId", GameId },
            { "excludedModIds", JToken.FromObject(new int[] { 0 }) },
            { "gameVersionTypeId", null }
        };

        try
        {
            using var responseMessage = await HttpWrapper.HttpPostAsync($"{Api}" +
                $"mods/featured", content.ToString(), Headers);
            responseMessage.EnsureSuccessStatusCode();

            var entity = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());

            result.AddRange(entity["data"]["popular"].ToObject<IEnumerable<CurseForgeResource>>());
            result.AddRange(entity["data"]["recentlyUpdated"].ToObject<IEnumerable<CurseForgeResource>>());
            result.AddRange(entity["data"]["featured"].ToObject<IEnumerable<CurseForgeResource>>());

            result.Sort((a, b) => a.GamePopularityRank.CompareTo(b.GamePopularityRank));

            return result;
        }
        catch { }

        return null;
    }

    public static async Task<string> GetResourceDescription(int modId)
    {
        using var responseMessage = await HttpWrapper.HttpGetAsync(Api + "mods" +
            $"/{modId}/description", Headers);

        return JObject.Parse(await responseMessage.Content.ReadAsStringAsync())["data"]
            .ToObject<string>();
    }
}
