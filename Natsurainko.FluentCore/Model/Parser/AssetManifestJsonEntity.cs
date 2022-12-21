using Newtonsoft.Json;
using System.Collections.Generic;

namespace Natsurainko.FluentCore.Model.Parser;

public class AssetManifestJsonEntity
{
    [JsonProperty("objects")]
    public Dictionary<string, AssetJsonEntity> Objects { get; set; }
}

public class AssetJsonEntity
{
    [JsonProperty("hash")]
    public string Hash { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }
}
