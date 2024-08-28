using System.Text.Json.Serialization;

namespace Nrk.FluentCore.GameManagement.Installer;

public class OptiFineInstallData
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("patch")]
    public required string Patch { get; set; }

    [JsonPropertyName("filename")]
    public required string FileName { get; set; }

    [JsonPropertyName("forge")]
    public required string ForgeVersion { get; set; }
}
