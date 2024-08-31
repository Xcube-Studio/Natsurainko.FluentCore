using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;

namespace Nrk.FluentCore.Resources;

using FeaturedResources = (IEnumerable<CurseForgeResource> Mods, IEnumerable<CurseForgeResource> Modpacks);

public class CurseForgeClient
{
    private const string BaseUrl = "https://api.curseforge.com/v1/";
    private const int MinecraftGameId = 432;

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;


    public CurseForgeClient(string apiKey, HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _httpClient = httpClient ?? HttpUtils.HttpClient;
    }

    #region CurseForge APIs

    public async Task<IEnumerable<CurseForgeResource>> SearchResourcesAsync(
        string searchFilter,
        CurseForgeResourceType? resourceType = null,
        string? version = null)
    {
        // Build URL
        var stringBuilder = new StringBuilder(BaseUrl)
            .Append($"mods/search?gameId={MinecraftGameId}")
            .Append($"&sortField=Featured")
            .Append($"&sortOrder=desc");

        if (resourceType is not null)
            stringBuilder.Append($"&categoryId=0&classId={(int)resourceType}");

        if (version is not null)
            stringBuilder.Append($"&gameVersion={version}");

        stringBuilder.Append($"&searchFilter={HttpUtility.UrlEncode(searchFilter)}");

        string url = stringBuilder.ToString();

        // Send request
        using var request = CreateCurseForgeGetRequest(url);
        using var responseMessage = await _httpClient.SendAsync(request);

        // Parse response
        var response = await responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();

        IEnumerable<CurseForgeResource>? resources = null;
        try
        {
            resources = JsonNode.Parse(response)?["data"]?
                .AsArray()
                .WhereNotNull()
                .Select(x => ParseCurseForgeResource(x))
                ?? throw new FormatException();
        }
        catch (Exception e) when (e is FormatException || e is InvalidOperationException)
        {
            throw new InvalidResponseException(url, response, "Error in JSON returned by CurseForge", e);
        }

        return resources;
    }

    public async Task<string> GetFileUrlAsync(CurseForgeFile file)
    {
        var url = $"{BaseUrl}mods/{file.ModId}/files/{file.FileId}";
        using var request = CreateCurseForgeGetRequest(url);
        using var responseMessage = await _httpClient.SendAsync(request);

        // Parse response
        var response = await responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();

        return JsonNode.Parse(response)?
            ["data"]?
            ["downloadUrl"]?
            .GetValue<string>()
            ?? throw new InvalidResponseException(url, response, "Error in JSON returned by CurseForge");
    }

    public async Task<FeaturedResources> GetFeaturedResourcesAsync()
    {
        // Create request
        string url = $"{BaseUrl}mods/featured";
        using var request = CreateCurseForgeGetRequest(url);
        request.Content = new StringContent(JsonSerializer.Serialize(new { gameId = MinecraftGameId }));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Send request
        using var response = await _httpClient.SendAsync(request);
        var responseJson = await response
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();

        // Parse response
        var modsList = new List<CurseForgeResource>();
        var modpacksList = new List<CurseForgeResource>();

        try
        {
            var jsonNode = JsonNode.Parse(responseJson)?["data"]
                ?? throw new FormatException();

            var featured = jsonNode["featured"]?.AsArray().WhereNotNull();
            var popular = jsonNode["popular"]?.AsArray().WhereNotNull();

            var resources = new List<JsonNode>();
            if (featured != null)
                resources.AddRange(featured);
            if (popular != null)
                resources.AddRange(popular);

            // Filter mods and modpacks
            foreach (var node in resources)
            {
                if (node["classId"] is not JsonNode classIdNode)
                    continue;

                int classId = classIdNode.GetValue<int>();
                if (classId.Equals((int)CurseForgeResourceType.ModPack))
                    modpacksList.Add(ParseCurseForgeResource(node));
                else if (classId.Equals((int)CurseForgeResourceType.McMod))
                    modsList.Add(ParseCurseForgeResource(node));
            }
        }
        catch (Exception e) when (e is FormatException || e is InvalidOperationException)
        {
            throw new InvalidResponseException(url, responseJson, "Error in JSON returned by CurseForge", e);
        }

        return (modsList, modpacksList);
    }

    public async Task<string> GetResourceDescriptionAsync(int resourceId)
    {
        // Create request
        string url = $"{BaseUrl}mods/{resourceId}/description";
        using var request = CreateCurseForgeGetRequest(url);

        // Send request
        using var responseMessage = await _httpClient.SendAsync(request);
        string responseJson = await responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();

        // Parse response
        string? result = null;
        try
        {
            result = JsonNode.Parse(responseJson)?["data"]?
                .GetValue<string>()
                ?? throw new FormatException();
        }
        catch (Exception e) when (e is FormatException || e is InvalidOperationException)
        {
            throw new InvalidResponseException(url, responseJson, "Error in JSON returned by CurseForge", e);
        }

        return result;
    }

    public async Task<CurseForgeResource> GetResourceAsync(int resourceId)
    {
        // Create request
        string url = $"{BaseUrl}mods/{resourceId}";
        using var request = CreateCurseForgeGetRequest(url);

        // Send request
        using var responseMessage = await _httpClient.SendAsync(request);
        string responseJson = await responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();

        // Parse response
        CurseForgeResource? result = null;
        try
        {
            var node = JsonNode.Parse(responseJson)?["data"]
                ?? throw new FormatException();

            result = ParseCurseForgeResource(node);
        }
        catch (Exception e) when (e is FormatException || e is InvalidOperationException)
        {
            throw new InvalidResponseException(url, responseJson, "Error in JSON returned by CurseForge", e);
        }

        return result;
    }

    #endregion

    private HttpRequestMessage CreateCurseForgeGetRequest(string url)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("x-api-key", _apiKey);
        return requestMessage;
    }

    private CurseForgeResource ParseCurseForgeResource(JsonNode jsonNode)
    {
        var id = jsonNode["id"]?.GetValue<int>();
        var classId = jsonNode["classId"]?.GetValue<int>();
        var name = jsonNode["name"]?.GetValue<string>();
        var summary = jsonNode["summary"]?.GetValue<string>();
        var downloadCount = jsonNode["downloadCount"]?.GetValue<int>();
        var dateModified = jsonNode["dateModified"]?.GetValue<DateTime>();
        var categories = jsonNode["categories"]?
            .AsArray()
            .WhereNotNull()
            .Select(x => x["name"]?.GetValue<string>())
            .WhereNotNull();
        var authors = jsonNode["authors"]?
            .AsArray()
            .WhereNotNull()
            .Select(x => x["name"]?.GetValue<string>())
            .WhereNotNull();
        var screenshotUrls = jsonNode["screenshots"]?
            .AsArray()
            .WhereNotNull()
            .Select(x => x["url"]?.GetValue<string>())
            .WhereNotNull();
        var latestFilesIndexes = jsonNode["latestFilesIndexes"]?
            .AsArray()
            .WhereNotNull()
            .Select(x =>
            {
                var file = x.Deserialize(ResourcesJsonSerializerContext.Default.CurseForgeFile)
                    ?? throw new InvalidOperationException();
                file.ModId = id.GetValueOrDefault();
                return file;
            })
            .WhereNotNull();
        var websiteUrl = jsonNode["links"]?["websiteUrl"]?.GetValue<string>();
        string? iconurl = jsonNode["logo"]?["url"]?.GetValue<string>();

        if (id is null
            || classId is null
            || name is null
            || summary is null
            || downloadCount is null
            || dateModified is null
            || latestFilesIndexes is null
            || categories is null
            || authors is null
            || screenshotUrls is null
            || websiteUrl is null
        )
            throw new FormatException();

        // Create CurseForgeResource object
        var curseResource = new CurseForgeResource
        {
            Id = id.Value,
            ClassId = classId.Value,
            Name = name,
            Summary = summary,
            DownloadCount = downloadCount.Value,
            DateModified = dateModified.Value,
            Files = latestFilesIndexes,
            Categories = categories,
            Authors = authors,
            ScreenshotUrls = screenshotUrls,
            WebsiteUrl = websiteUrl,
            IconUrl = iconurl
        };
        curseResource.Files = curseResource.Files.Select(x =>
        {
            x.ModId = curseResource.Id;
            return x;
        });
        return curseResource;
    }
}
