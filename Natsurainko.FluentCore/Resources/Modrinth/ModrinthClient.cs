using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Resources;

public class ModrinthClient
{
    private const string BaseUrl = "https://api.modrinth.com/v2/";

    private readonly HttpClient _httpClient;

    public ModrinthClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? HttpUtils.HttpClient;
    }

    public async Task<IEnumerable<ModrinthResource>> SearchResourcesAsync(
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
                    Loaders = loaders.ToArray()
                });
            }
            catch (Exception e) when (e is JsonException || e is FormatException)
            {
                throw new InvalidResponseException(url, responseJson, "Error in JSON returned by Modrinth", e);
            }
        }

        return modrinthFiles;
    }
}
