using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Management.ModLoaders;

public record QuiltInstallBuild
{
    [JsonPropertyName("intermediary")]
    public QuiltMavenItem Intermediary { get; set; }

    [JsonPropertyName("loader")]
    public QuiltMavenItem Loader { get; set; }

    [JsonPropertyName("launcherMeta")]
    public QuiltLauncherMeta LauncherMeta { get; set; }

    public string McVersion => Intermediary.Version;

    public string DisplayVersion => $"{McVersion}-{Loader.Version}";

    public string BuildVersion => Loader.Version;
}

public record QuiltLauncherMeta
{
    [JsonPropertyName("mainClass")]
    public Dictionary<string, string> MainClass { get; set; }
}

public record QuiltMavenItem
{
    [JsonPropertyName("separator")]
    public string Separator { get; set; }

    [JsonPropertyName("maven")]
    public string Maven { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }
}
