using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources;

public record ModrinthResource
{
    [JsonPropertyName("project_id")]
    [JsonRequired]
    public string Id { get; set; } = null!;

    [JsonPropertyName("slug")]
    [JsonRequired]
    public string Slug { get; set; } = null!;

    [JsonPropertyName("project_type")]
    [JsonRequired]
    public string ProjectType { get; set; } = null!;

    [JsonPropertyName("title")]
    [JsonRequired]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    [JsonRequired]
    public string Summary { get; set; } = null!;

    [JsonPropertyName("downloads")]
    [JsonRequired]
    public int DownloadCount { get; set; }

    [JsonPropertyName("date_modified")]
    [JsonRequired]
    public DateTime DateModified { get; set; }

    [JsonPropertyName("author")]
    [JsonRequired]
    public string Author { get; set; } = null!;

    [JsonPropertyName("versions")]
    [JsonRequired]
    public IEnumerable<string> Versions { get; set; } = null!;

    [JsonPropertyName("display_categories")]
    [JsonRequired]
    public IEnumerable<string> Categories { get; set; } = null!;

    [JsonPropertyName("gallery")]
    [JsonRequired]
    public IEnumerable<string> ScreenshotUrls { get; set; } = null!;

    [JsonPropertyName("icon_url")]
    [JsonRequired]
    public string IconUrl { get; set; } = null!;

    public string WebLink => $"https://modrinth.com/{ProjectType}/{Slug}";
}

public record ModrinthProject
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public string Id { get; set; } = null!;

    [JsonPropertyName("slug")]
    [JsonRequired]
    public string Slug { get; set; } = null!;

    [JsonPropertyName("project_type")]
    [JsonRequired]
    public string ProjectType { get; set; } = null!;

    [JsonPropertyName("title")]
    [JsonRequired]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    [JsonRequired]
    public string Summary { get; set; } = null!;

    [JsonPropertyName("downloads")]
    [JsonRequired]
    public int DownloadCount { get; set; }

    [JsonPropertyName("updated")]
    [JsonRequired]
    public DateTime DateModified { get; set; }

    [JsonPropertyName("loaders")]
    [JsonRequired]
    public IEnumerable<string> Loaders { get; set; }

    //[JsonPropertyName("author")]
    //[JsonRequired]
    //public string Author { get; set; } = null!;

    [JsonPropertyName("categories")]
    [JsonRequired]
    public IEnumerable<string> Categories { get; set; } = null!;

    //[JsonPropertyName("gallery")]
    //[JsonRequired]
    //public IEnumerable<string> ScreenshotUrls { get; set; } = null!;

    [JsonPropertyName("icon_url")]
    [JsonRequired]
    public string IconUrl { get; set; } = null!;

    public string WebLink => $"https://modrinth.com/{ProjectType}/{Slug}";
}