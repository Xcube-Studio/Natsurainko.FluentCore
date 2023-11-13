using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Management.Parsing;

public record AssetJsonNode
{
    [JsonPropertyName("hash")]
    public required string Hash { get; set; }
}
