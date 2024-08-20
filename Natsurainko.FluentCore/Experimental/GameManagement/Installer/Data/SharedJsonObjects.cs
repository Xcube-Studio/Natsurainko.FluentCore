using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer.Data;

/// <summary>
/// Shared by Fabric, Quilt's installation Json
/// </summary>
public class MavenItemJsonObject
{
    [JsonPropertyName("separator")]
    public string? Separator { get; set; }

    [JsonPropertyName("maven")]
    public required string Maven { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }
}