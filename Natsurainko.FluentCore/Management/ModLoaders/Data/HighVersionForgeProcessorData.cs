using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Management.ModLoaders;

public record HighVersionForgeProcessorData
{
    [JsonPropertyName("sides")]
    public List<string> Sides { get; set; } = new();

    [JsonPropertyName("jar")]
    public required string Jar { get; set; }

    [JsonPropertyName("classpath")]
    public required IEnumerable<string> Classpath { get; set; }

    [JsonPropertyName("args")]
    public required IEnumerable<string> Args { get; set; }

    [JsonPropertyName("outputs")]
    public Dictionary<string, string> Outputs { get; set; } = new();
}
