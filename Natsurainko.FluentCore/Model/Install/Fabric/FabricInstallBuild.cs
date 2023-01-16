using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Natsurainko.FluentCore.Model.Install.Fabric;

public class FabricInstallBuild : IModLoaderInstallBuild
{
    [JsonProperty("intermediary")]
    public FabricMavenItem Intermediary { get; set; }

    [JsonProperty("loader")]
    public FabricMavenItem Loader { get; set; }

    [JsonProperty("launcherMeta")]
    public FabricLauncherMeta LauncherMeta { get; set; }

    public string McVersion => Intermediary.Version;

    public string DisplayVersion => $"{McVersion}-{Loader.Version}";
}

public class FabricLauncherMeta
{
    [JsonProperty("mainClass")]
    public JToken MainClass { get; set; }

    [JsonProperty("libraries")]
    public Dictionary<string, List<LibraryJsonEntity>> Libraries { get; set; }
}

public class FabricMavenItem
{
    [JsonProperty("separator")]
    public string Separator { get; set; }

    [JsonProperty("maven")]
    public string Maven { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; }
}