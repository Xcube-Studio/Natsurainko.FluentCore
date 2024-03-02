using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nrk.FluentCore.Resources;

public class ModrinthClient
{
    public const string Host = "https://api.modrinth.com/v2/";

    public IEnumerable<ModrinthResource> SearchResources(
        string query,
        ModrinthResourceType? resourceType = null,
        string? version = null)
    {
        var stringBuilder = new StringBuilder(Host);
        stringBuilder.Append($"search?query={query}");

        var facets = new List<string>();

        if (resourceType != null)
            facets.Add($"[\"project_type:{resourceType switch
            {
                ModrinthResourceType.ModPack => "modpack",
                ModrinthResourceType.Resourcepack => "resourcepack",
                _ => "mod"
            }}\"]");

        if (version != null)
            facets.Add($"\"[versions:{version}\"]");

        if (facets.Any())
            stringBuilder.Append($"&facets=[{string.Join(',', facets)}]");

        using var responseMessage = HttpUtils.HttpGet(stringBuilder.ToString());
        string responseJson = responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsString();

        // Parse JSON
        return responseJson
            .ToJsonNode()?["hits"]?
            .Deserialize<IEnumerable<ModrinthResource>>()
            ?? throw new Exception("Failed to deserialize JSON response");
    }

    public string GetResourceDescription(string id)
    {
        using var responseMessage = HttpUtils.HttpGet(Host + $"project/{id}");
        string responseJson = responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsString();

        return responseJson.ToJsonNode()?["body"]?
            .GetValue<string>()
            ?? throw new Exception("Failed to deserialize JSON response");
    }

    public IEnumerable<ModrinthFile> GetProjectVersions(string id)
    {
        using var responseMessage = HttpUtils.HttpGet(Host + $"project/{id}/version");
        string responseJson = responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsString();

        var jsonArray = JsonNode.Parse(responseJson)?.AsArray()
            ?? throw new Exception("Failed to deserialize JSON response");

        foreach (var file in jsonArray)
        {
            if (file is null)
                continue;

            string? url = file["files"]?[0]?["url"]?.GetValue<string>();
            string? fileName = file["files"]?[0]?["filename"]?.GetValue<string>();
            string? mcVersion = file["game_versions"]?[0]?.GetValue<string>();

            IEnumerable<string>? loaders = file["loaders"]?
                .AsArray()
                .WhereNotNull()
                .Select(x => x.GetValue<string>());

            if (url is null || fileName is null || mcVersion is null || loaders is null)
                throw new Exception("Failed to deseralize JSON response"); // QUESTION: Maybe just skip this file?

            yield return new ModrinthFile
            {
                Url = url,
                FileName = fileName,
                McVersion = mcVersion,
                Loaders = string.Join(' ', loaders)
            };
        }
    }

    public string GetRawJsonSearchResources(
        string query,
        ModrinthResourceType? resourceType = default,
        string? version = null)
    {
        var stringBuilder = new StringBuilder(Host);
        stringBuilder.Append($"search?query={query}");

        var facets = new List<string>();

        if (resourceType != null)
            facets.Add($"[\"project_type:{resourceType switch
            {
                ModrinthResourceType.ModPack => "modpack",
                ModrinthResourceType.Resourcepack => "resourcepack",
                _ => "mod"
            }}\"]");

        if (version != null) facets.Add($"\"[versions:{version}\"]");
        if (facets.Any()) stringBuilder.Append($"&facets=[{string.Join(',', facets)}]");

        using var responseMessage = HttpUtils.HttpGet(stringBuilder.ToString());
        responseMessage.EnsureSuccessStatusCode();

        return responseMessage.Content.ReadAsString();
    }

    public string GetRawJsonGetProject(string id)
    {
        using var responseMessage = HttpUtils.HttpGet(Host + $"project/{id}");
        responseMessage.EnsureSuccessStatusCode();

        return responseMessage.Content.ReadAsString();
    }
}
