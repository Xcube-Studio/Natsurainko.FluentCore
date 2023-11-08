using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources;

public record ModrinthResource
{
    [JsonPropertyName("project_id")]
    public required string Id { get; set; }

    [JsonPropertyName("slug")]
    public required string Slug { get; set; }

    [JsonPropertyName("project_type")]
    public required string ProjectType { get; set; }

    [JsonPropertyName("title")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Summary { get; set; }

    [JsonPropertyName("downloads")]
    public required int DownloadCount { get; set; }

    [JsonPropertyName("date_modified")]
    public required DateTime DateModified { get; set; }

    [JsonPropertyName("author")]
    public required string Author { get; set; }

    [JsonPropertyName("display_categories")]
    public required IEnumerable<string> Categories { get; set; }

    [JsonPropertyName("gallery")]
    public required IEnumerable<string> ScreenshotUrls { get; set; }

    [JsonPropertyName("icon_url")]
    public required string IconUrl { get; set; }

    public string WebLink => $"https://modrinth.com/{ProjectType}/{Slug}";
}
