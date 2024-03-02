using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources;

public record DownloadsJsonNode
{
    [JsonPropertyName("artifact")]
    public FileJsonNode? Artifact { get; init; }

    [JsonPropertyName("classifiers")]
    public Dictionary<string, FileJsonNode>? Classifiers { get; init; }

}

public record FileJsonNode
{
    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; init; }

    [JsonPropertyName("size")]
    public int? Size { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    //for client-x.xx.xml
    [JsonPropertyName("id")]
    public string? Id { get; init; }
}

