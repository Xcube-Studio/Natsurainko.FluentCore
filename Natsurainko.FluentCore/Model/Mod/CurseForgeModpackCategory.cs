﻿using Newtonsoft.Json;

namespace Natsurainko.FluentCore.Model.Mod;

public class CurseForgeModpackCategory
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}