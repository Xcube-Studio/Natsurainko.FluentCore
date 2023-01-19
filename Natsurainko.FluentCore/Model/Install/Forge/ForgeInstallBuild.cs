using Natsurainko.FluentCore.Interface;
using Newtonsoft.Json;
using System;

namespace Natsurainko.FluentCore.Model.Install.Forge;

public class ForgeInstallBuild : IModLoaderInstallBuild
{
    [JsonProperty("branch")]
    public string Branch { get; set; }

    [JsonProperty("build")]
    public int Build { get; set; }

    [JsonProperty("mcversion")]
    public string McVersion { get; set; }

    [JsonProperty("version")]
    public string ForgeVersion { get; set; }

    [JsonProperty("modified")]
    public DateTime ModifiedTime { get; set; }

    public string DisplayVersion => $"{McVersion}-{ForgeVersion}";

    public string BuildVersion => ForgeVersion;

    public ModLoaderType ModLoaderType => ModLoaderType.Forge;
}
