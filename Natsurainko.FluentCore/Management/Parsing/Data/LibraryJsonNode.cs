using Nrk.FluentCore.Management.Downloader.Data;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Management.Parsing;

public record LibraryJsonNode
{
    [JsonPropertyName("downloads")]
    public DownloadsJsonNode? Downloads { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("natives")]
    public Dictionary<string, string> Natives { get; set; } = new();
}

public class RuleModel
{
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    [JsonPropertyName("os")]
    public Dictionary<string, string>? System { get; set; }
}