using System.Text.Json.Serialization;

namespace Nrk.FluentCore.GameManagement.Installer;

public class FabricInstallData
{
    [JsonPropertyName("intermediary")]
    public required MavenItemJsonObject Intermediary { get; set; }

    [JsonPropertyName("loader")]
    public required MavenItemJsonObject Loader { get; set; }

    //[JsonPropertyName("launcherMeta")]
    //public required FabricLauncherMeta LauncherMeta { get; set; }
}

//public class FabricLauncherMeta
//{
//    [JsonPropertyName("mainClass")]
//    public required JsonNode MainClass { get; set; }

//    [JsonPropertyName("libraries")]
//    public required Dictionary<string, List<MinecraftLibrary>> Libraries { get; set; }
//}
