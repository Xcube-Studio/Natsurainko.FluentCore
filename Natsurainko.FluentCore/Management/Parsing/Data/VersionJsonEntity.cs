using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Management.Parsing;

/// <summary>
/// version.json 对应的实体类
/// </summary>
public record VersionJsonEntity
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("mainClass")]
    public required string MainClass { get; set; }

    [JsonPropertyName("minecraftArguments")]
    public required string MinecraftArguments { get; set; }

    [JsonPropertyName("inheritsFrom")]
    public required string InheritsFrom { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("assets")]
    public required string Assets { get; set; }

    [JsonPropertyName("arguments")]
    public required ArgumentsJsonNode Arguments { get; set; }

    [JsonPropertyName("assetIndex")]
    public required AssstIndexJsonNode AssetIndex { get; set; }
}

/// <summary>
/// version.json 下 arguments 键 对应的实体类
/// </summary>
public class ArgumentsJsonNode
{
    [JsonPropertyName("game")]
    public required IEnumerable<JsonElement> Game { get; set; }

    [JsonPropertyName("jvm")]
    public required IEnumerable<JsonElement> Jvm { get; set; }
}

/// <summary>
/// version.json 下 assetIndex 键 对应的实体类
/// </summary>
public class AssstIndexJsonNode
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("sha1")]
    public required string Sha1 { get; set; }
}