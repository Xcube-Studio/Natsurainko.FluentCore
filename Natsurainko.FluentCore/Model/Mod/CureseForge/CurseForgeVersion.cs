using Newtonsoft.Json;
using System.Collections.Generic;

namespace Natsurainko.FluentCore.Model.Mod.CureseForge;

public class CurseForgeVersion
{
    [JsonProperty("type")]
    public int Type { get; set; }

    [JsonProperty("versions")]
    public IEnumerable<string> Versions { get; set; }
}

public class CurseForgeVersionType
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("gameId")]
    public int GameId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("slug")]
    public string Slug { get; set; }
}