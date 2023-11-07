using Nrk.FluentCore.Classes.Enums;
using Nrk.FluentCore.GameResources.Parsing;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Classes.Datas.Install;

public record FabricInstallBuild
{
    [JsonPropertyName("intermediary")]
    public FabricMavenItem Intermediary { get; set; }

    [JsonPropertyName("loader")]
    public FabricMavenItem Loader { get; set; }

    [JsonPropertyName("launcherMeta")]
    public FabricLauncherMeta LauncherMeta { get; set; }

    public string McVersion => Intermediary.Version;

    public string DisplayVersion => $"{McVersion}-{Loader.Version}";

    public string BuildVersion => Loader.Version;

    public ModLoaderType ModLoaderType => ModLoaderType.Fabric;
}

public record FabricLauncherMeta
{
    [JsonPropertyName("mainClass")]
    public JsonNode MainClass { get; set; }

    [JsonPropertyName("libraries")]
    public Dictionary<string, List<LibraryJsonNode>> Libraries { get; set; }
}

public record FabricMavenItem
{
    [JsonPropertyName("separator")]
    public string Separator { get; set; }

    [JsonPropertyName("maven")]
    public string Maven { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }
}