using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.GameManagement.Installer.Data;

public class ForgeInstallData
{
    [JsonPropertyName("mcversion")]
    public required string McVersion { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("branch")]
    public string? Branch { get; set; }
}

public class ForgeProcessorData
{
    [JsonPropertyName("sides")]
    public List<string> Sides { get; set; } = [];

    [JsonPropertyName("jar")]
    public required string Jar { get; set; }

    [JsonPropertyName("classpath")]
    public required IEnumerable<string> Classpath { get; set; }

    [JsonPropertyName("args")]
    public required IEnumerable<string> Args { get; set; }

    [JsonPropertyName("outputs")]
    public Dictionary<string, string> Outputs { get; set; } = [];
}