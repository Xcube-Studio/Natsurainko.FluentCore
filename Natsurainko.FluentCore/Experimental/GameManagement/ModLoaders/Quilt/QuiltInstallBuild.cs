using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Experimental.GameManagement.ModLoaders.Quilt;

public record QuiltInstallBuild
{
    [JsonPropertyName("intermediary")]
    public required QuiltMavenItem Intermediary { get; set; }

    [JsonPropertyName("loader")]
    public required QuiltMavenItem Loader { get; set; }

    [JsonPropertyName("launcherMeta")]
    public required QuiltLauncherMeta LauncherMeta { get; set; }

    public string McVersion => Intermediary.Version;

    public string DisplayVersion => $"{McVersion}-{Loader.Version}";

    public string BuildVersion => Loader.Version;
}

public record QuiltLauncherMeta
{
    [JsonPropertyName("mainClass")]
    public required Dictionary<string, string> MainClass { get; set; }
}

public record QuiltMavenItem
{
    [JsonPropertyName("separator")]
    public string? Separator { get; set; }

    [JsonPropertyName("maven")]
    public required string Maven { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }
}
