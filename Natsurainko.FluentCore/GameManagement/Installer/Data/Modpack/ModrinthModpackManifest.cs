using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.GameManagement.Installer.Modpack;

public class ModrinthModpackManifest
{
    [JsonRequired]
    [JsonPropertyName("dependencies")]
    public required Dictionary<string, string> Dependencies { get; set; }

    [JsonRequired]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonRequired]
    [JsonPropertyName("files")]
    public required ModrinthModpackFileJsonObject[] Files { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("game")]
    public string? Game { get; set; }

    [JsonPropertyName("versionId")]
    public string? VersionId { get; set; }

    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; }

    public class ModrinthModpackFileJsonObject
    {
        [JsonRequired]
        [JsonPropertyName("downloads")]
        public required string[] Downloads { get; set; }

        [JsonRequired]
        [JsonPropertyName("env")]
        public required Dictionary<string, string> Environment { get; set; }

        [JsonRequired]
        [JsonPropertyName("path")]
        public required string Path { get; set; }

        [JsonPropertyName("fileSize")]
        public int? FileSize { get; set; }

        [JsonPropertyName("hashes")]
        public Dictionary<string, string>? Hashes { get; set; }
    }
}
