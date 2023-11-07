﻿using Nrk.FluentCore.Classes.Enums;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Classes.Datas.Download;

public record CurseFile
{
    [JsonPropertyName("fileId")]
    public int FileId { get; set; }

    [JsonPropertyName("gameVersion")]
    public string McVersion { get; set; }

    [JsonPropertyName("filename")]
    public string FileName { get; set; }

    [JsonPropertyName("modLoader")]
    public ModLoaderType ModLoaderType { get; set; }

    public int ModId { get; set; }

    public string DisplayDescription => $"{ModLoaderType} {McVersion}";
}
