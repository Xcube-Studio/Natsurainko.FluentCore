using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.Installer;

public class OptiFineInstallData
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("patch")]
    public required string Patch { get; set; }

    [JsonPropertyName("filename")]
    public required string FileName { get; set; }

    [JsonPropertyName("forge")]
    public string? ForgeVersion { get; set; }
}

public class OptiFineInstallDataApi
{
    public static async Task<OptiFineInstallData[]> GetOptiFineInstallDataFromBmclApiAsync(string mcVersion,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        return JsonSerializer.Deserialize(
            await httpClient.GetStringAsync($"https://bmclapi2.bangbang93.com/optifine/{mcVersion}", cancellationToken),
            OptiFineInstallerJsonSerializerContext.Default.OptiFineInstallDataArray)
            ?? throw new InvalidDataException();
    }
}
