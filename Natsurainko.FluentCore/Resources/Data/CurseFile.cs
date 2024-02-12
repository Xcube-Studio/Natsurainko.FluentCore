using Nrk.FluentCore.Management.ModLoaders;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources;

public record CurseFile
{
    [JsonPropertyName("fileId")]
    public required int FileId { get; set; }

    [JsonPropertyName("gameVersion")]
    public required string McVersion { get; set; }

    [JsonPropertyName("filename")]
    public required string FileName { get; set; }

    [JsonPropertyName("modLoader")]
    public ModLoaderType ModLoaderType { get; set; }

    public int ModId { get; set; }

    public string DisplayDescription => $"{ModLoaderType} {McVersion}";
}
