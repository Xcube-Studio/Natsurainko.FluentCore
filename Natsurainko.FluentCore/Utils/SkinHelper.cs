using Nrk.FluentCore.Authentication;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Utils;

public static class SkinHelper
{
    public static async Task<string> GetSkinUrlAsync(MicrosoftAccount account)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
        requestMessage.Headers.Authorization = new("Bearer", account.AccessToken);

        using var responseMessage = await HttpUtils.HttpClient.SendAsync(requestMessage);
        responseMessage.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(responseMessage.Content.ReadAsString())!["skins"]!
                .AsArray().Where(item => (item!["state"]?.GetValue<string>().Equals("ACTIVE")).GetValueOrDefault()).FirstOrDefault();

        return json!["url"]!.GetValue<string>();
    }

    public static async Task<string> GetSkinUrlAsync(YggdrasilAccount account)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get,
            account.YggdrasilServerUrl +
            "/sessionserver/session/minecraft/profile/" +
            account.Uuid.ToString("N").ToLower());

        requestMessage.Headers.Authorization = new("Bearer", account.AccessToken);

        using var responseMessage = await HttpUtils.HttpClient.SendAsync(requestMessage);
        responseMessage.EnsureSuccessStatusCode();

        var jsonBase64 = JsonNode.Parse(responseMessage.Content.ReadAsString())!["properties"]![0]!["value"]!;
        var json = JsonNode.Parse(jsonBase64.GetValue<string>().ConvertFromBase64());

        return json!["textures"]?["SKIN"]?["url"]?.GetValue<string>() ?? "http://assets.mojang.com/SkinTemplates/steve.png";
    }

    public static async Task UploadSkinAsync(MicrosoftAccount account, bool isSlim, string filePath)
    {
        using var multipartFormData = new MultipartFormDataContent();

        using var stringContent = new StringContent(isSlim ? "slim" : "classic");
        stringContent.Headers.ContentDisposition = new("form-data") { Name = "variant" };
        multipartFormData.Add(stringContent);

        using var arrayContent = new ByteArrayContent(File.ReadAllBytes(filePath));
        arrayContent.Headers.ContentDisposition = new("form-data")
        {
            Name = "file",
            FileName = Path.GetFileName(filePath)
        };
        multipartFormData.Add(arrayContent);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.minecraftservices.com/minecraft/profile/skins");
        requestMessage.Content = multipartFormData;
        requestMessage.Headers.Authorization = new("Bearer", account.AccessToken);

        using var responseMessage = await HttpUtils.HttpClient.SendAsync(requestMessage);
        responseMessage.EnsureSuccessStatusCode();
    }

    public static async Task UploadSkinAsync(YggdrasilAccount account, bool isSlim, string filePath)
    {
        using var multipartFormData = new MultipartFormDataContent();

        using var stringContent = new StringContent(isSlim ? "slim" : string.Empty);
        stringContent.Headers.ContentDisposition = new("form-data") { Name = "variant" };
        multipartFormData.Add(stringContent);

        using var arrayContent = new ByteArrayContent(File.ReadAllBytes(filePath));
        arrayContent.Headers.ContentDisposition = new("form-data")
        {
            Name = "file",
            FileName = Path.GetFileName(filePath)
        };
        multipartFormData.Add(arrayContent);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{account.YggdrasilServerUrl}/api/user/profile/{account.Uuid:N}/skin");
        requestMessage.Content = multipartFormData;
        requestMessage.Headers.Authorization = new("Bearer", account.AccessToken);

        using var responseMessage = await HttpUtils.HttpClient.SendAsync(requestMessage);
        responseMessage.EnsureSuccessStatusCode();
    }
}
