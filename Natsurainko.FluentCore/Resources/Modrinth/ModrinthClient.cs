using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Resources;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Resources;

public class ModrinthClient
{
    private const string BaseUrl = "https://api.modrinth.com/v2/";

    private readonly HttpClient _httpClient;

    public ModrinthClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<ModrinthResource>> GetResourceSearchResultAsync(
        string query,
        ModrinthResourceType? resourceType = null,
        string? version = null)
    {
        // Build URL
        var stringBuilder = new StringBuilder($"{BaseUrl}search?query={query}");

        var facets = new List<string>();
        if (resourceType is not null)
        {
            string type = resourceType switch
            {
                ModrinthResourceType.ModPack => "modpack",
                ModrinthResourceType.Resourcepack => "resourcepack",
                _ => "mod"
            };
            facets.Add($"[\"project_type:{type}\"]");
        }
        if (version is not null)
            facets.Add($"[\"versions:{version}\"]");

        if (facets.Any())
            stringBuilder.Append($"&facets=[{string.Join(',', facets)}]");

        string url = stringBuilder.ToString();

        // Send request
        using var responseMessage = await _httpClient.GetAsync(url);
        string responseJson = await responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();

        // Parse JSON
        IEnumerable<ModrinthResource>? modrinthResources = null;
        try
        {
            modrinthResources = responseJson
                .ToJsonNode()?["hits"]?
                .Deserialize<IEnumerable<ModrinthResource?>>()?
                .WhereNotNull()
                ?? throw new FormatException();
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new InvalidResponseException(url, responseJson, "Error in JSON returned by Modrinth", e);
        }

        return modrinthResources;
    }

    public IEnumerable<ModrinthResource> SearchResources(
        string query,
        ModrinthResourceType? resourceType = null,
        string? version = null)
    {
        var stringBuilder = new StringBuilder(BaseUrl);
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

    public async Task<string> GetResourceDescriptionAsync(string resourceId)
    {
        // Send request
        string url = $"{BaseUrl}project/{resourceId}";
        using var responseMessage = await _httpClient.GetAsync(url);
        string responseJson = await responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();

        // Parse JSON
        string? result = null;
        try
        {
            result = responseJson.ToJsonNode()?["body"]?
                .GetValue<string>()
                ?? throw new FormatException();
        }
        catch (Exception e) when (e is FormatException || e is InvalidOperationException)
        {
            throw new InvalidResponseException(url, responseJson, "Error in JSON returned by Modrinth", e);
        }

        return result;
    }

    public string GetResourceDescription(string id)
    {
        using var responseMessage = HttpUtils.HttpGet(BaseUrl + $"project/{id}");
        string responseJson = responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsString();

        return responseJson.ToJsonNode()?["body"]?
            .GetValue<string>()
            ?? throw new Exception("Failed to deserialize JSON response");
    }

    public async Task<IEnumerable<ModrinthFile>> GetProjectVersionsAsync(string projectId)
    {
        // Send request
        string url = $"{BaseUrl}project/{projectId}/version";
        using var responseMessage = await _httpClient.GetAsync(url);
        string responseJson = await responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();

        // Parse JSON
        JsonArray? jsonArray = null;
        try
        {
            jsonArray = JsonNode.Parse(responseJson)?.AsArray()
                ?? throw new FormatException("Response is not a JSON array");
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new InvalidResponseException(url, responseJson, "Error in JSON returned by Modrinth", e);
        }

        IEnumerable<JsonNode> jsonNodes = jsonArray.WhereNotNull();
        List<ModrinthFile> modrinthFiles = new(jsonNodes.Count());

        foreach (var file in jsonNodes)
        {
            // Parse each file
            try
            {
                string? fileUrl = file["files"]?[0]?["url"]?.GetValue<string>();
                string? fileName = file["files"]?[0]?["filename"]?.GetValue<string>();
                string? mcVersion = file["game_versions"]?[0]?.GetValue<string>();

                IEnumerable<string>? loaders = file["loaders"]?
                    .AsArray()
                    .WhereNotNull()
                    .Select(x => x.GetValue<string>());

                if (fileUrl is null || fileName is null || mcVersion is null || loaders is null)
                    throw new FormatException("Failed to deserialize JSON response");

                modrinthFiles.Add(new ModrinthFile
                {
                    Url = fileUrl,
                    FileName = fileName,
                    McVersion = mcVersion,
                    Loaders = string.Join(' ', loaders)
                });
            }
            catch (Exception e) when (e is JsonException || e is FormatException)
            {
                throw new InvalidResponseException(url, responseJson, "Error in JSON returned by Modrinth", e);
            }
        }

        return modrinthFiles;
    }

    public IEnumerable<ModrinthFile> GetProjectVersions(string id)
    {
        using var responseMessage = HttpUtils.HttpGet(BaseUrl + $"project/{id}/version");
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

    public async Task<string> GetResourceSearchResultJsonAsync(
        string query,
        ModrinthResourceType? resourceType = null,
        string? version = null)
    {
        // Build URL
        var stringBuilder = new StringBuilder($"{BaseUrl}search?query={query}");

        var facets = new List<string>();
        if (resourceType is not null)
        {
            string type = resourceType switch
            {
                ModrinthResourceType.ModPack => "modpack",
                ModrinthResourceType.Resourcepack => "resourcepack",
                _ => "mod"
            };
            facets.Add($"[\"project_type:{type}\"]");
        }
        if (version is not null)
            facets.Add($"[\"versions:{version}\"]");

        if (facets.Any())
            stringBuilder.Append($"&facets=[{string.Join(',', facets)}]");

        string url = stringBuilder.ToString();

        // Send request
        using var responseMessage = await _httpClient.GetAsync(url);
        string responseJson = await responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();

        return responseJson;
    }

    public async Task<string> GetProjectJsonAsync(string projectId)
    {
        using var responseMessage = await _httpClient.GetAsync($"{BaseUrl}project/{projectId}");

        return await responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();
    }
}
