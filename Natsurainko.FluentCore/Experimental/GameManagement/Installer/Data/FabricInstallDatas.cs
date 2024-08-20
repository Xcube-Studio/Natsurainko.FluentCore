using Nrk.FluentCore.Management.Parsing;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer.Data;

public class FabricInstallData
{
    [JsonPropertyName("intermediary")]
    public required MavenItemJsonObject Intermediary { get; set; }

    [JsonPropertyName("loader")]
    public required MavenItemJsonObject Loader { get; set; }

    [JsonPropertyName("launcherMeta")]
    public required FabricLauncherMeta LauncherMeta { get; set; }

    public string McVersion => Intermediary.Version;

    public string DisplayVersion => $"{McVersion}-{Loader.Version}";

    public string BuildVersion => Loader.Version;
}

public class FabricLauncherMeta
{
    [JsonPropertyName("mainClass")]
    public required JsonNode MainClass { get; set; }

    [JsonPropertyName("libraries")]
    public required Dictionary<string, List<LibraryJsonNode>> Libraries { get; set; }
}
