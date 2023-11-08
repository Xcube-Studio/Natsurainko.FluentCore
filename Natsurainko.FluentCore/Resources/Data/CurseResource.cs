using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Resources;

public record CurseResource
{
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    [JsonPropertyName("classId")]
    public required int ClassId { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("summary")]
    public required string Summary { get; set; }

    [JsonPropertyName("downloadCount")]
    public required int DownloadCount { get; set; }

    [JsonPropertyName("dateModified")]
    public required DateTime DateModified { get; set; }

    [JsonPropertyName("latestFilesIndexes")]
    public required IEnumerable<CurseFile> Files { get; set; }

    public required IEnumerable<string> Categories { get; set; }

    public required IEnumerable<string> Authors { get; set; }

    public required IEnumerable<string> ScreenshotUrls { get; set; }

    public required string WebLink { get; set; }

    public required string IconUrl { get; set; }
}
