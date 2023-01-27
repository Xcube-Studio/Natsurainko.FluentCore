using Natsurainko.FluentCore.Interface;
using Newtonsoft.Json;

/* 项目“Natsurainko.FluentCore (net6.0)”的未合并的更改
在此之前:
using Newtonsoft.Json;
在此之后:
using Newtonsoft.Json.Linq;
*/
using System.Collections.Generic;

namespace Natsurainko.FluentCore.Model.Install.Quilt;

public class QuiltInstallBuild : IModLoaderInstallBuild
{
    [JsonProperty("intermediary")]
    public QuiltMavenItem Intermediary { get; set; }

    [JsonProperty("loader")]
    public QuiltMavenItem Loader { get; set; }

    [JsonProperty("launcherMeta")]
    public QuiltLauncherMeta LauncherMeta { get; set; }

    public string McVersion => Intermediary.Version;

    public string DisplayVersion => $"{McVersion}-{Loader.Version}";

    public string BuildVersion => Loader.Version;

    public ModLoaderType ModLoaderType => ModLoaderType.Quilt;
}

public class QuiltLauncherMeta
{
    [JsonProperty("mainClass")]
    public Dictionary<string, string> MainClass { get; set; }
}

public class QuiltMavenItem
{
    [JsonProperty("separator")]
    public string Separator { get; set; }

    [JsonProperty("maven")]
    public string Maven { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; }
}