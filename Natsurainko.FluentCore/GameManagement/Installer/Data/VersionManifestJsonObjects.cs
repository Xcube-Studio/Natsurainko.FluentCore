using Nrk.FluentCore.GameManagement.Downloader;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.Installer;

public class VersionManifestJsonObject
{
    [JsonPropertyName("latest")]
    public required Dictionary<string, string> Latest { get; set; }

    [JsonPropertyName("versions")]
    public required VersionManifestItem[] Versions { get; set; }
}

public class VersionManifestItem
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("time")]
    public required string Time { get; set; }

    [JsonPropertyName("releaseTime")]
    public required string ReleaseTime { get; set; }
}

public static class VersionManifestApi
{ 
    public static async Task<VersionManifestJsonObject> GetVersionManifestAsync(HttpClient httpClient,
        IDownloadMirror? downloadMirror = null,
        CancellationToken cancellationToken = default)
    {
        string requestUrl = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";

        if (downloadMirror != null) 
            requestUrl = downloadMirror.GetMirrorUrl(requestUrl);

        return JsonSerializer.Deserialize(
            await httpClient.GetStringAsync(requestUrl, cancellationToken),
            MinecraftJsonSerializerContext.Default.VersionManifestJsonObject)
            ?? throw new InvalidDataException();
    }
}