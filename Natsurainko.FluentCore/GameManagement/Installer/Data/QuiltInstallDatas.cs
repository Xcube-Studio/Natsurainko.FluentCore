using System.Text.Json.Serialization;

namespace Nrk.FluentCore.GameManagement.Installer;

public class QuiltInstallData
{
    [JsonPropertyName("intermediary")]
    public required MavenItemJsonObject Intermediary { get; set; }

    [JsonPropertyName("loader")]
    public required MavenItemJsonObject Loader { get; set; }

    //[JsonPropertyName("launcherMeta")]
    //public required QuiltLauncherMeta LauncherMeta { get; set; }
}

//public class QuiltLauncherMeta
//{
//    [JsonPropertyName("mainClass")]
//    public required Dictionary<string, string> MainClass { get; set; }
//}
