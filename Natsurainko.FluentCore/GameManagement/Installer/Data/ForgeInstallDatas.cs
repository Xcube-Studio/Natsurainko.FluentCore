using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.GameManagement.Installer;

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
    [JsonRequired]
    public string Jar { get; set; } = null!;

    [JsonPropertyName("classpath")]
    [JsonRequired]
    public IEnumerable<string> Classpath { get; set; } = null!;

    [JsonPropertyName("args")]
    [JsonRequired]
    public IEnumerable<string> Args { get; set; } = null!;

    [JsonPropertyName("outputs")]
    public Dictionary<string, string> Outputs { get; set; } = [];
}