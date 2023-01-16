using Newtonsoft.Json;
using System.Collections.Generic;

namespace Natsurainko.FluentCore.Model.Install.Forge;

public class ForgeInstallProcessorModel
{
    [JsonProperty("sides")]
    public List<string> Sides { get; set; } = new ();

    [JsonProperty("jar")]
    public string Jar { get; set; }

    [JsonProperty("classpath")]
    public List<string> Classpath { get; set; }

    [JsonProperty("args")]
    public IEnumerable<string> Args { get; set; }

    [JsonProperty("outputs")]
    public Dictionary<string, string> Outputs { get; set; } = new();
}
