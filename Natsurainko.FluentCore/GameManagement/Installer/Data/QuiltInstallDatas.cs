using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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

public static class QuiltInstallDataApi
{
    public static async Task<QuiltInstallData[]> GetQuiltInstallDataAsync(string mcVersion,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        return JsonSerializer.Deserialize(
            await httpClient.GetStringAsync($"https://meta.quiltmc.org/v3/versions/loader/{mcVersion}", cancellationToken),
            QuiltInstallerJsonSerializerContext.Default.QuiltInstallDataArray)
            ?? throw new InvalidDataException();
    }
}