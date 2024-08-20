using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer.Data;

public class QuiltInstallData
{
    [JsonPropertyName("intermediary")]
    public required MavenItemJsonObject Intermediary { get; set; }

    [JsonPropertyName("loader")]
    public required MavenItemJsonObject Loader { get; set; }

    [JsonPropertyName("launcherMeta")]
    public required QuiltLauncherMeta LauncherMeta { get; set; }

    public string McVersion => Intermediary.Version;

    public string DisplayVersion => $"{McVersion}-{Loader.Version}";

    public string BuildVersion => Loader.Version;
}

public class QuiltLauncherMeta
{
    [JsonPropertyName("mainClass")]
    public required Dictionary<string, string> MainClass { get; set; }
}
