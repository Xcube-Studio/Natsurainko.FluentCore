using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Classes.Datas.Install;

public record HighVersionForgeProcessorData
{
    [JsonPropertyName("sides")]
    public List<string> Sides { get; set; } = new();

    [JsonPropertyName("jar")]
    public string Jar { get; set; }

    [JsonPropertyName("classpath")]
    public IEnumerable<string> Classpath { get; set; }

    [JsonPropertyName("args")]
    public IEnumerable<string> Args { get; set; }

    [JsonPropertyName("outputs")]
    public Dictionary<string, string> Outputs { get; set; } = new();
}
