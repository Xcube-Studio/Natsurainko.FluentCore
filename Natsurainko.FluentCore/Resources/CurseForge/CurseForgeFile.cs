using Nrk.FluentCore.GameManagement.Installer;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources;

public record CurseForgeFile
{
    [JsonPropertyName("fileId")]
    [JsonRequired]
    public int FileId { get; set; }

    [JsonPropertyName("gameVersion")]
    [JsonRequired]
    public string McVersion { get; set; } = null!;

    [JsonPropertyName("filename")]
    [JsonRequired]
    public string FileName { get; set; } = null!;

    [JsonPropertyName("modLoader")]
    public ModLoaderType ModLoaderType { get; set; }

    public int ModId { get; set; }
}
