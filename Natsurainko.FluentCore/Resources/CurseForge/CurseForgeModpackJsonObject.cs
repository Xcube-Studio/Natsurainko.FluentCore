using Nrk.FluentCore.Experimental.GameManagement.ModLoaders;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources.CurseForge;

/// <summary>
/// Data structure of manifest.json in CurseForge format modpack
/// </summary>
internal class CurseForgeModpackJsonObject
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("overrides")]
    public required string OverridesPath { get; set; }

    [JsonPropertyName("files")]
    public required IEnumerable<ModFileJsonObject> ModFiles { get; set; }

    [JsonPropertyName("minecraft")]
    public required InstanceInfomationJsonObject InstanceInfomation { get; set; }

    //[JsonPropertyName("manifestType")]
    //public required string ManifestType { get; set; }

    //[JsonPropertyName("manifestVersion")]
    //public required int ManifestVersion { get; set; }

    public class ModFileJsonObject
    {
        [JsonPropertyName("projectID")]
        public required string ProjectId { get; set; }

        [JsonPropertyName("fileID")]
        public required string FileId { get; set; }

        [JsonPropertyName("required")]
        public required bool IsRequired { get; set; }
    }

    public class InstanceInfomationJsonObject
    {
        [JsonPropertyName("version")]
        public required string McVersion { get; set; }

        // Are there two or more items here?
        [JsonPropertyName("modLoaders")]
        public required IEnumerable<ModLoaderJsonObject> ModLoader { get; set; }

        public class ModLoaderJsonObject
        {
            // like <ModLoaderType>-<Version>
            [JsonPropertyName("id")]
            public required string Id { get; set; }

            // should it always be true?
            [JsonPropertyName("primary")]
            public required bool Primary { get; set; }

            public ModLoaderInfo GetLoaderInfo()
            {
                string[] items = Id.Split('-');

                if (items.Length != 2)
                    throw new ArgumentException(nameof(Id), "Unexpected Id value");

                if (!Enum.TryParse(items[0], true, out ModLoaderType result))
                    throw new InvalidOperationException("An unrecognized loader type was encountered");

                return new ModLoaderInfo
                {
                    Type = result,
                    Version = items[1]
                };
            }
        }
    }
}
