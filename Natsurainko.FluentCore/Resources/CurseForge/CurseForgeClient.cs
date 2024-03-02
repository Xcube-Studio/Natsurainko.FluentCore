using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace Nrk.FluentCore.Resources;

public class CurseForgeClient
{
    public const string Host = "https://api.curseforge.com/v1/";
    public const int MinecraftGameId = 432;

    public required string ApiKey { get; init; }

    public required int GameId { get; init; }

    private Dictionary<string, string> Header => new() { { "x-api-key", ApiKey } };

    public IEnumerable<CurseForgeResource> SearchResources(
        string searchFilter,
        CurseForgeResourceType? resourceType = default,
        string? version = null)
    {
        var stringBuilder = new StringBuilder(Host);
        stringBuilder.Append($"mods/search?gameId={GameId}");
        stringBuilder.Append($"&sortField=Featured");
        stringBuilder.Append($"&sortOrder=desc");

        if (resourceType != null)
            stringBuilder.Append($"&categoryId=0&classId={(int)resourceType}");

        stringBuilder.Append($"&searchFilter={HttpUtility.UrlEncode(searchFilter)}");

        using var responseMessage = HttpUtils.HttpGet(stringBuilder.ToString(), Header);
        string responseJson = responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsString();

        return JsonNode.Parse(responseJson)?
            ["data"]?
            .AsArray()
            .WhereNotNull()
            .Select(x => ParseFromJsonNode(x))
            ?? throw new Exception("Error in parsing JSON response");
    }

    public string GetCurseFileDownloadUrl(CurseForgeFile file)
    {
        using var responseMessage = HttpUtils.HttpGet(Host + $"mods/{file.ModId}/files/{file.FileId}", Header);
        string responseJson = responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsString();

        return JsonNode.Parse(responseJson)?
            ["data"]?
            ["downloadUrl"]?
            .GetValue<string>()
            ?? throw new Exception("Error in parsing JSON response");
    }

    public void GetFeaturedResources(out IEnumerable<CurseForgeResource> mcMods, out IEnumerable<CurseForgeResource> modPacks)
    {
        using var responseMessage = HttpUtils.HttpPost(
            Host + "mods/featured",
            JsonSerializer.Serialize(new { gameId = 432 }),
            Header);

        string responseJson = responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsString();

        var json = JsonNode.Parse(responseJson)?["data"]
            ?? throw new Exception("Error in parsing JSON response");
        var featured = json["featured"]?.AsArray().WhereNotNull();
        var popular = json["popular"]?.AsArray().WhereNotNull();

        var resources = new List<JsonNode>();
        if (featured != null)
            resources.AddRange(featured);
        if (popular != null)
            resources.AddRange(popular);

        var mcModsList = new List<CurseForgeResource>();
        var modPacksList = new List<CurseForgeResource>();
        mcMods = mcModsList;
        modPacks = modPacksList;

        foreach (var node in resources)
        {
            if (node["classId"] is not JsonNode classIdNode)
                continue;

            int classId = classIdNode.GetValue<int>();
            if (classId.Equals((int)CurseForgeResourceType.ModPack))
                modPacksList.Add(ParseFromJsonNode(node));
            else if (classId.Equals((int)CurseForgeResourceType.McMod))
                mcModsList.Add(ParseFromJsonNode(node));
        }
    }

    public string GetResourceDescription(int resourceId)
    {
        using var responseMessage = HttpUtils.HttpGet(Host + $"mods/{resourceId}/description", Header);
        string responseJson = responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsString();

        return JsonNode.Parse(responseJson)?
            ["data"]?
            .GetValue<string>()
            ?? throw new Exception("Error in parsing JSON response");
    }

    public CurseForgeResource GetResource(int resourceId)
    {
        using var responseMessage = HttpUtils.HttpGet(Host + $"mods/{resourceId}", Header);
        string responseJson = responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsString();

        var node = JsonNode.Parse(responseJson)?
            ["data"]
            ?? throw new Exception("Error in parsing JSON response");

        return ParseFromJsonNode(node);
    }

    public string GetRawJsonSearchResources(string searchFilter, CurseForgeResourceType? resourceType = default)
    {
        var stringBuilder = new StringBuilder(Host);
        stringBuilder.Append($"mods/search?gameId={GameId}");
        stringBuilder.Append($"&sortField=Featured");
        stringBuilder.Append($"&sortOrder=desc");

        if (resourceType != null)
            stringBuilder.Append($"&categoryId=0&classId={(int)resourceType}");

        stringBuilder.Append($"&searchFilter={HttpUtility.UrlEncode(searchFilter)}");

        using var responseMessage = HttpUtils.HttpGet(stringBuilder.ToString(), Header);
        responseMessage.EnsureSuccessStatusCode();

        return responseMessage.Content.ReadAsString();
    }

    public string GetRawJsonCategories()
    {
        using var responseMessage = HttpUtils.HttpGet(Host + $"categories?gameId={GameId}", Header);
        return responseMessage.Content.ReadAsString();
    }

    private CurseForgeResource ParseFromJsonNode(JsonNode jsonNode)
    {
        var id = jsonNode["id"]?.GetValue<int>();
        var classId = jsonNode["classId"]?.GetValue<int>();
        var name = jsonNode["name"]?.GetValue<string>();
        var summary = jsonNode["summary"]?.GetValue<string>();
        var downloadCount = jsonNode["downloadCount"]?.GetValue<int>();
        var dateModified = jsonNode["dateModified"]?.GetValue<DateTime>();
        var categories = jsonNode["categories"]?
            .AsArray()
            .WhereNotNull()
            .Select(x => x["name"]?.GetValue<string>())
            .WhereNotNull();
        var authors = jsonNode["authors"]?
            .AsArray()
            .WhereNotNull()
            .Select(x => x["name"]?.GetValue<string>())
            .WhereNotNull();
        var screenshotUrls = jsonNode["screenshots"]?
            .AsArray()
            .WhereNotNull()
            .Select(x => x["url"]?.GetValue<string>())
            .WhereNotNull();
        var latestFilesIndexes = jsonNode["latestFilesIndexes"]?
            .AsArray()
            .WhereNotNull()
            .Select(x =>
            {
                var file = x.Deserialize<CurseForgeFile>() ?? throw new InvalidOperationException();
                file.ModId = id.GetValueOrDefault();
                return file;
            })
            .WhereNotNull();
        var websiteUrl = jsonNode["links"]?["websiteUrl"]?.GetValue<string>();
        var iconurl = jsonNode["logo"]?["url"]?.GetValue<string>();

        if (id is null
            || classId is null
            || name is null
            || summary is null
            || downloadCount is null
            || dateModified is null
            || latestFilesIndexes is null
            || categories is null
            || authors is null
            || screenshotUrls is null
            || websiteUrl is null
            || iconurl is null
        )
            throw new Exception("Error in parsing JSON response");

        // Create CurseForgeResource object
        var curseResource = new CurseForgeResource
        {
            Id = id.Value,
            ClassId = classId.Value,
            Name = name,
            Summary = summary,
            DownloadCount = downloadCount.Value,
            DateModified = dateModified.Value,
            Files = latestFilesIndexes,
            Categories = categories,
            Authors = authors,
            ScreenshotUrls = screenshotUrls,
            WebsiteUrl = websiteUrl,
            IconUrl = iconurl
        };
        curseResource.Files = curseResource.Files.Select(x =>
        {
            x.ModId = curseResource.Id;
            return x;
        });
        return curseResource;
    }
}
