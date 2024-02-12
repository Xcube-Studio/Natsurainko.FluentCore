using Nrk.FluentCore.Management.Parsing;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Management.ModLoaders;

public record FabricInstallBuild
{
    [JsonPropertyName("intermediary")]
    public required FabricMavenItem Intermediary { get; set; }

    [JsonPropertyName("loader")]
    public required FabricMavenItem Loader { get; set; }

    [JsonPropertyName("launcherMeta")]
    public required FabricLauncherMeta LauncherMeta { get; set; }

    public string McVersion => Intermediary.Version;

    public string DisplayVersion => $"{McVersion}-{Loader.Version}";

    public string BuildVersion => Loader.Version;

    public ModLoaderType ModLoaderType => ModLoaderType.Fabric;
}

public record FabricLauncherMeta
{
    [JsonPropertyName("mainClass")]
    public required JsonNode MainClass { get; set; }

    [JsonPropertyName("libraries")]
    public required Dictionary<string, List<LibraryJsonNode>> Libraries { get; set; }
}

public record FabricMavenItem
{
    [JsonPropertyName("separator")]
    public string Separator { get; set; }
            
    [JsonPropertyName("maven")]
    public required string Maven { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }
}