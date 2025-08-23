using Nrk.FluentCore.GameManagement.Downloader;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.Installer;

public class ForgeInstallData
{
    [JsonPropertyName("mcversion")]
    public required string McVersion { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("branch")]
    public string? Branch { get; set; }

    [JsonPropertyName("build")]
    public long? Build { get; set; }
}

public static class ForgeInstallDataApi
{
    public static async Task<ForgeInstallData[]> GetForgeInstallDataAsync(string mcVersion,
        HttpClient? httpClient = null,
        IDownloadMirror? downloadMirror = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= new HttpClient();

        if (downloadMirror is BmclApiMirror)
            return await GetForgeInstallDataFromBmclApiAsync(mcVersion, false, httpClient, cancellationToken);

        string response = await httpClient.GetStringAsync("https://maven.minecraftforge.net/api/maven/versions/releases/net/minecraftforge/forge", cancellationToken);
        JsonNode jsonNode = JsonNode.Parse(response) ?? throw new InvalidDataException();

        List<ForgeInstallData> forgeInstallDatas = [];

        foreach (var node in jsonNode["versions"]?.AsArray()!)
        {
            string loaderVersion = node!.GetValue<string>();
            string[] identifiers = loaderVersion.Split('-');
            string? branch = identifiers.Length >= 3 ? identifiers[2] : null;

            if (!(loaderVersion.StartsWith(mcVersion)))
                continue;

            forgeInstallDatas.Add(new()
            {
                McVersion = mcVersion,
                Version = identifiers[1],
                Branch = branch,
            });
        }

        return [.. forgeInstallDatas];
    }

    public static async Task<ForgeInstallData[]> GetNeoForgeInstallDataAsync(string mcVersion,
        HttpClient? httpClient = null,
        IDownloadMirror? downloadMirror = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= new HttpClient();

        if (downloadMirror is BmclApiMirror)
            return await GetForgeInstallDataFromBmclApiAsync(mcVersion, true, httpClient, cancellationToken);

        string neoforgeResponse = await httpClient.GetStringAsync("https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge", cancellationToken);
        JsonArray neoforgeNodes = JsonNode.Parse(neoforgeResponse)?["versions"]?.AsArray() ?? throw new InvalidDataException();

        string forgeResponse = await httpClient.GetStringAsync("https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/forge", cancellationToken);
        JsonArray forgeNodes = JsonNode.Parse(forgeResponse)?["versions"]?.AsArray() ?? throw new InvalidDataException();

        List<ForgeInstallData> forgeInstallDatas = [];

        foreach (var node in forgeNodes)
        {
            string loaderVersion = node!.GetValue<string>();
            if (!loaderVersion.StartsWith(mcVersion)) continue;

            forgeInstallDatas.Add(new()
            {
                McVersion = mcVersion,
                Version = loaderVersion,
            });
        }

        string majorVersion = mcVersion[2..];

        foreach (var node in neoforgeNodes)
        {
            string loaderVersion = node!.GetValue<string>();

            if (!(loaderVersion.StartsWith(majorVersion) || loaderVersion.StartsWith($"0.{majorVersion}")))
                continue;

            forgeInstallDatas.Add(new()
            {
                McVersion = mcVersion,
                Version = loaderVersion,
            });
        }

        return [.. forgeInstallDatas];
    }

    private static async Task<ForgeInstallData[]> GetForgeInstallDataFromBmclApiAsync(string mcVersion,
        bool isNeoForge,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        string requestUrl = isNeoForge
            ? $"https://bmclapi2.bangbang93.com/neoforge/list/{mcVersion}"
            : $"https://bmclapi2.bangbang93.com/forge/minecraft/{mcVersion}";

        return JsonSerializer.Deserialize(
            await httpClient.GetStringAsync(requestUrl, cancellationToken),
            ForgeInstallerJsonSerializerContext.Default.ForgeInstallDataArray)
            ?? throw new InvalidDataException();
    }
}