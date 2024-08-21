using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Experimental.GameManagement.Installer;
using Nrk.FluentCore.Experimental.GameManagement.Installer.Data;
using Nrk.FluentCore.Utils;
using System.Text.Json;
using System.Text.Json.Nodes;

var httpClient = HttpUtils.HttpClient;
string requestUrl = "http://launchermeta.mojang.com/mc/game/version_manifest_v2.json";

using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
using var responseMessage = await httpClient.SendAsync(requestMessage);
responseMessage.EnsureSuccessStatusCode();

string jsonContent = await responseMessage.Content.ReadAsStringAsync();
VersionManifestJsonObject versionManifest = JsonNode.Parse(jsonContent).Deserialize<VersionManifestJsonObject>()!;

var vanillaInstanceInstaller = new VanillaInstanceInstaller()
{
    DownloadMirror = DownloadMirrors.BmclApi,
    McVersionManifestItem = versionManifest.Versions.First(),
    MinecraftFolder = @"D:\Minecraft\Test\.minecraft"
};

try
{
    await vanillaInstanceInstaller.InstallAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}