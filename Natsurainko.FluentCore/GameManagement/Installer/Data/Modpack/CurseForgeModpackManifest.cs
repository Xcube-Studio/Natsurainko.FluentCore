using System.Text.Json.Serialization;

namespace Nrk.FluentCore.GameManagement.Installer.Data.Modpack;

public class CurseForgeModpackManifest
{
    [JsonRequired]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonRequired]
    [JsonPropertyName("overrides")]
    public required string Overrides { get; set; }

    [JsonPropertyName("manifestType")]
    public string? ManifestType { get; set; }

    [JsonPropertyName("manifestVersion")]
    public int ManifestVersion { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonRequired]
    [JsonPropertyName("minecraft")]
    public required MinecraftInstanceJsonObject Minecraft { get; set; }

    [JsonRequired]
    [JsonPropertyName("files")]
    public required FileJsonObject[] Files { get; set; }

    public class FileJsonObject
    {
        [JsonRequired]
        [JsonPropertyName("projectID")]
        public int ProjectId { get; set; }

        [JsonRequired]
        [JsonPropertyName("fileID")]
        public int FileId { get; set; }

        [JsonRequired]
        [JsonPropertyName("required")]
        public required bool Required { get; set; }
    }

    public class MinecraftInstanceJsonObject
    {
        [JsonRequired]
        [JsonPropertyName("version")]
        public required string McVersion { get; set; }

        [JsonRequired]
        [JsonPropertyName("modLoaders")]
        public required ModLoaderJsonObject[] ModLoaders { get; set; }

        public class ModLoaderJsonObject
        {
            [JsonRequired]
            [JsonPropertyName("id")]
            public required string Id { get; set; }

            [JsonRequired]
            [JsonPropertyName("primary")]
            public required bool Primary { get; set; }
        }
    }
}
