using Nrk.FluentCore.GameResources.ThirdPartySources;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.GameResources.Parsing;

public record LibraryJsonNode
{
    [JsonPropertyName("downloads")]
    public DownloadsJsonNode Downloads { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("natives")]
    public Dictionary<string, string> Natives { get; set; }
}

public class RuleModel
{
    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("os")]
    public Dictionary<string, string> System { get; set; }
}