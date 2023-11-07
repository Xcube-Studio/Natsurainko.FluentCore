using System.Text.Json.Serialization;

namespace Nrk.FluentCore.GameResources.Parsing;

public record AssetJsonNode
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; }
}
