using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Authentication.Yggdrasil;

public class YggdrasilAuthenticator
{
    private readonly HttpClient _httpClient;

    private readonly string _serverUrl;
    private readonly string _clientToken;

    public YggdrasilAuthenticator(string serverUrl, string clientToken, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? HttpUtils.HttpClient;

        _serverUrl = serverUrl;
        _clientToken = clientToken;
    }

    /// <summary>
    /// Login to Yggdrasil account
    /// </summary>
    /// <param name="email">Yggdrasil account email</param>
    /// <param name="password">Yggdrasil account password</param>
    /// <returns>All Minecraft accounts associated with the Yggdrasil account</returns>
    public async Task<YggdrasilAccount[]> LoginAsync(string email, string password)
    {
        var request = new YggdrasilLoginRequest
        {
            ClientToken = _clientToken,
            UserName = email,
            Password = password
        };

        using var response = await _httpClient.PostAsync(
            $"{_serverUrl}/authserver/authenticate",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        return await ParseResponseAsync(response);
    }

    /// <summary>
    /// Refresh all Minecraft accounts associated with the Yggdrasil account
    /// </summary>
    /// <param name="account">Any Yggdrasil Minecraft account to be refreshed</param>
    /// <returns>All Minecraft accounts associated with the Yggdrasil account</returns>
    public async Task<YggdrasilAccount[]> RefreshAsync(YggdrasilAccount account)
    {
        var request = new YggdrasilRefreshRequest
        {
            ClientToken = _clientToken,
            AccessToken = account.AccessToken,
            RequestUser = true
        };

        using var response = await _httpClient.PostAsync(
            $"{_serverUrl}/authserver/refresh",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        return await ParseResponseAsync(response);
    }

    // Read Yggdrasil accounts from the response for both login and refresh
    private async Task<YggdrasilAccount[]> ParseResponseAsync(HttpResponseMessage responseMessage)
    {
        YggdrasilResponseModel? response = null;
        try
        {
            response = await responseMessage
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync<YggdrasilResponseModel>();

            if (response?.AvailableProfiles is null)
                throw new FormatException("Response does not contain any profile");
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new YggdrasilAuthenticationException(responseMessage.Content.ReadAsString());
        }

        return response.AvailableProfiles
            .Select(profile =>
            {
                if (profile.Name is null || profile.Id is null || response.AccessToken is null)
                    throw new YggdrasilAuthenticationException(responseMessage.Content.ReadAsString());

                if (!Guid.TryParse(profile.Id, out var uuid))
                    throw new YggdrasilAuthenticationException("Invalid UUID");

                return new YggdrasilAccount(
                    profile.Name,
                    uuid,
                    response.AccessToken,
                    _clientToken,
                    _serverUrl
                );
            })
            .ToArray();
    }
}
