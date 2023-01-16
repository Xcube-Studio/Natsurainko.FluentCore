using Natsurainko.FluentCore.Interface;
using Newtonsoft.Json;

namespace Natsurainko.FluentCore.Model.Install.OptiFine;

public class OptiFineInstallBuild : IModLoaderInstallBuild
{
    [JsonProperty("patch")]
    public string Patch { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("mcversion")]
    public string McVersion { get; set; }

    [JsonProperty("filename")]
    public string FileName { get; set; }

    public string DisplayVersion => $"{McVersion}_{Type}_{Patch}";
}
