using Natsurainko.FluentCore.Model.Install;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Natsurainko.FluentCore.Model.Mod;

public class CurseForgeResource
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("gameId")]
    public int GameId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("slug")]
    public string Slug { get; set; }

    [JsonProperty("summary")]
    public string Summary { get; set; }

    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("downloadCount")]
    public int DownloadCount { get; set; }

    [JsonProperty("isFeatured")]
    public bool IsFeatured { get; set; }

    [JsonProperty("primaryCategoryId")]
    public int PrimaryCategoryId { get; set; }

    [JsonProperty("classId")]
    public int ClassId { get; set; }

    [JsonProperty("mainFileId")]
    public int MainFileId { get; set; }

    [JsonProperty("dateCreated")]
    public DateTime DateCreated { get; set; }

    [JsonProperty("dateModified")]
    public DateTime DateModified { get; set; }

    [JsonProperty("dateReleased")]
    public DateTime DateReleased { get; set; }

    [JsonProperty("allowModDistribution")]
    public bool? AllowModDistribution { get; set; }

    [JsonProperty("gamePopularityRank")]
    public int GamePopularityRank { get; set; }

    [JsonProperty("isAvailable")]
    public bool IsAvailable { get; set; }

    [JsonProperty("thumbsUpCount")]
    public int ThumbsUpCount { get; set; }

    [JsonProperty("categories")]
    public IEnumerable<CurseForgeCategory> Categories { get; set; }

    [JsonProperty("authors")]
    public IEnumerable<CurseForgeAuthor> Author { get; set; }

    [JsonProperty("links")]
    public Dictionary<string,string> Links { get; set; }

    [JsonProperty("logo")]
    public CurseForgeImage Logo { get; set; }

    [JsonProperty("screenshots")]
    public IEnumerable<CurseForgeImage> Screenshots { get; set; }

    [JsonProperty("latestFilesIndexes")]
    public List<CurseForgeModpackFileInfo> LatestFilesIndexes { get; set; }
}

public class CurseForgeAuthor
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}

public class CurseForgeImage
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("modId")]
    public int ModId { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("thumbnailUrl")]
    public string ThumbnailUrl { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

}

public class CurseForgeResourceFileInfo
{
    public string DownloadUrl { get; set; }

    [JsonProperty("fileId")]
    public int FileId { get; set; }

    [JsonProperty("filename")]
    public string FileName { get; set; }

    [JsonProperty("modLoader")]
    public ModLoaderType? ModLoaderType { get; set; }

    [JsonProperty("gameVersion")]
    public string SupportedVersion { get; set; }
}