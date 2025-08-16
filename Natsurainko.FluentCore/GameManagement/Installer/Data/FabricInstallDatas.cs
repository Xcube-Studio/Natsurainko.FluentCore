using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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

public static class FabricInstallDataApi
{
    public static async Task<FabricInstallData[]> GetFabricInstallDataAsync(string mcVersion,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        return JsonSerializer.Deserialize(
            await httpClient.GetStringAsync($"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}", cancellationToken),
            FabricInstallerJsonSerializerContext.Default.FabricInstallDataArray)
            ?? throw new InvalidDataException();
    }
}