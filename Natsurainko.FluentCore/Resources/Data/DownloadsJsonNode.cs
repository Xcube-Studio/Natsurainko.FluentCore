using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources;

public record DownloadsJsonNode
{
    [JsonPropertyName("artifact")]
    public required FileJsonNode Artifact { get; set; }

    [JsonPropertyName("classifiers")]
    public required Dictionary<string, FileJsonNode> Classifiers { get; set; }

}

public record FileJsonNode
{
    [JsonPropertyName("path")]
    public required string Path { get; set; }

    [JsonPropertyName("sha1")]
    public required string Sha1 { get; set; }

    [JsonPropertyName("size")]
    public required int Size { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    //for client-x.xx.xml
    [JsonPropertyName("id")]
    public required string Id { get; set; }
}

