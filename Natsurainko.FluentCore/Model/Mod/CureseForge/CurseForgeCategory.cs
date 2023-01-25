using Newtonsoft.Json;
using System;

namespace Natsurainko.FluentCore.Model.Mod.CureseForge;

public class CurseForgeCategory
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("gameId")]
    public int GameId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("slug")]
    public string Slug { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("iconUrl")]
    public string IconUrl { get; set; }

    [JsonProperty("dateModified")]
    public DateTime DateModified { get; set; }

    [JsonProperty("classId")]
    public int ClassId { get; set; }

    [JsonProperty("parentCategoryId")]
    public int ParentCategoryId { get; set; }

    [JsonProperty("displayIndex")]
    public int DisplayIndex { get; set; }

    [JsonProperty("isClass")]
    public bool IsClass { get; set; }
}
