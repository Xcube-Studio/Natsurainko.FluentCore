using Nrk.FluentCore.Authentication;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Utils;

public static class PlayerTextureHelper
{
    public record PlayerTextureProfile
    {
        public required AccountType Type { get; set; }

        public Guid Uuid { get; set; }

        public string? YggdrasilServerUrl { get; set; }

        public SkinModel? ActiveSkin { get; set; }

        public CapeModel? ActiveCape { get; set; }
    }

    public static async Task<PlayerTextureProfile> GetTextureProfileAsync(this Account account)
    {
        string api = account switch
        {
            YggdrasilAccount yggdrasil => $"{yggdrasil.YggdrasilServerUrl}/sessionserver/session/minecraft/profile/",
            MicrosoftAccount => "https://sessionserver.mojang.com/session/minecraft/profile/",
            _ => throw new InvalidOperationException("Unsupported account type for texture profile retrieval.")
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, api +
            account.Uuid.ToString("N").ToLower());

        requestMessage.Headers.Authorization = new("Bearer", account.AccessToken);

        using var responseMessage = await HttpUtils.HttpClient.SendAsync(requestMessage);
        responseMessage.EnsureSuccessStatusCode();

        return ParseFromJson(account, await responseMessage.Content.ReadAsStringAsync());
    }

    public static async Task<CapeModel[]> GetAllCapeOfProfileAsync(this MicrosoftAccount microsoftAccount)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
        requestMessage.Headers.Authorization = new("Bearer", microsoftAccount.AccessToken);

        using var responseMessage = await HttpUtils.HttpClient.SendAsync(requestMessage);
        responseMessage.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize(
            await responseMessage.Content.ReadAsStreamAsync(),
            AuthenticationJsonSerializerContext.Default.MicrosoftAuthenticationResponse)?
            .Capes ?? throw new InvalidDataException();
    }

    public static async Task UploadSkinTextureAsync(MicrosoftAccount account, string filePath, string variant = "classic")
    {
        using var multipartFormData = new MultipartFormDataContent();
        using var stringContent = new StringContent(variant);
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

    private static PlayerTextureProfile ParseFromJson(Account account, string response)
    {
        PlayerTextureProfile profile = new()
        {
            Type = account.Type,
            Uuid = account.Uuid
        };

        if (account is YggdrasilAccount yggdrasilAccount)
            profile.YggdrasilServerUrl = yggdrasilAccount.YggdrasilServerUrl;

        if (JsonNode.Parse(response)!["properties"] is not JsonArray array || array.Count == 0)
            return profile;

        var node = array.FirstOrDefault();
        if (node?["value"]?.GetValue<string>() is not string base64)
            return profile;

        var texturesNode = JsonNode.Parse(base64.ConvertFromBase64())!["textures"]!;

        if (texturesNode["SKIN"] is JsonNode skinNode)
        {
            profile.ActiveSkin = new()
            {
                Url = skinNode["url"]!.GetValue<string>(),
                Variant = skinNode["metadata"]?["model"]?.GetValue<string>() ?? "classic",
                State = "ACTIVE"
            };
        }

        if (texturesNode["CAPE"] is JsonNode capeNode)
        {
            profile.ActiveCape = new()
            {
                Url = capeNode["url"]!.GetValue<string>(),
                Alias = capeNode["url"]?.GetValue<string>().Split('/').Last()!,
                State = "ACTIVE"
            };
        }

        return profile;
    }
}
