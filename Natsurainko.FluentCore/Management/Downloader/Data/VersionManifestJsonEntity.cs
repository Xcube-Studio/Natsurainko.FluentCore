using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Management.Downloader.Data;

public record VersionManifestJsonEntity
{
    [JsonPropertyName("latest")]
    public required Dictionary<string, string> Latest { get; set; }

    [JsonPropertyName("versions")]
    public required IEnumerable<VersionManifestItem> Versions { get; set; }
}

public record VersionManifestItem
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