using Nrk.FluentCore.Exceptions;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Authentication.Yggdrasil.OAuth;

public class YggdrasilOAuthAuthenticator
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;

    public required string ClientId { get; init; }

    public string? ClientSecret { get; init; }

    public string? RedirectUri { get; init; }

    public string? ServerName { get; init; }

    public required string TokenEndpoint { get; init; }

    public string? DeviceAuthorizationEndpoint { get; init; }

    public required string UserInfoEndpoint { get; init; }

    public YggdrasilOAuthAuthenticator(string serverUrl, HttpClient? httpClient = null)
    {
        _serverUrl = serverUrl;
        _httpClient = httpClient ?? HttpUtils.HttpClient;
    }

    public async Task<YggdrasilAccount> LoginAsync(string code, CancellationToken cancellationToken = default)
    {
        var oAuth2TokenResponse = await AuthAsync("code", code, cancellationToken);
        var (name, guid) = await GetMinecraftProfileAsync(oAuth2TokenResponse.AccessToken, cancellationToken);

        return new YggdrasilAccount(name, guid, oAuth2TokenResponse.AccessToken, _serverUrl)
        {
            MetaData = GetMetaData(oAuth2TokenResponse.RefreshToken, oAuth2TokenResponse.ExpiresIn.GetValueOrDefault(259200))
        };
    }

    public async Task<YggdrasilAccount> LoginAsync(OAuth2TokenResponse oAuth2TokenResponse, CancellationToken cancellationToken = default)
    {
        var (name, guid) = await GetMinecraftProfileAsync(oAuth2TokenResponse.AccessToken, cancellationToken);

        return new YggdrasilAccount(name, guid, oAuth2TokenResponse.AccessToken, _serverUrl) 
        {
            MetaData = GetMetaData(oAuth2TokenResponse.RefreshToken, oAuth2TokenResponse.ExpiresIn.GetValueOrDefault(259200))
        };
    }

    public async Task<YggdrasilAccount> RefreshAsync(YggdrasilAccount account, CancellationToken cancellationToken = default)
    {
        if (!account.MetaData.TryGetValue("authType", out var authType) || authType != "OAuth")
            throw new InvalidOperationException("The account could not be refreshed by oauth authenticator");

        if (account.MetaData.TryGetValue("useDeviceFlow", out var useDeviceFlow) && useDeviceFlow == "false" && string.IsNullOrEmpty(this.ClientSecret))
            throw new InvalidOperationException("The account could not be refreshed by an oauth authenticator without the client secret");

        if (!account.MetaData.TryGetValue("refresh_token", out var refreshToken))
            throw new ArgumentException("The account does not have a refresh token");

        var oAuth2TokenResponse = await AuthAsync("refresh_token", refreshToken, cancellationToken);
        var (name, guid) = await GetMinecraftProfileAsync(oAuth2TokenResponse.AccessToken, cancellationToken);

        account.MetaData["refresh_token"] = oAuth2TokenResponse.RefreshToken;
        account.MetaData["expires_in"] = oAuth2TokenResponse.ExpiresIn.ToString()!;

        return new YggdrasilAccount(name, guid, oAuth2TokenResponse.AccessToken, _serverUrl)
        {
            MetaData = account.MetaData
        };
    }

    /// <summary>
    /// Authenticate with Yggdrasil Account using OAuth2 device flow and poll for user login
    /// </summary>
    /// <param name="receiveUserCodeAction">Function called when user code is received</param>
    /// <param name="cancellationToken">Token for cancelling device flow authentication process</param>
    /// <returns>Yggdrasil Account OAuth tokens</returns>
    /// <exception cref="AuthException">Error in authenticating Yggdrasil Account using OAuth2 device flow</exception>
    public async Task<OAuth2TokenResponse> AuthFromDeviceFlowAsync(
        Action<OAuth2DeviceCodeResponse> receiveUserCodeAction,
        CancellationToken cancellationToken = default)
    {
        var deviceCodeResponse = await GetDeviceCodeAsync();

        // Allow the caller to provide the device code to the user, e.g. display on UI or print to console
        receiveUserCodeAction(deviceCodeResponse);

        // Polling for device flow login
        OAuth2TokenResponse? oauth2TokenResponse = null;
        int timeout = deviceCodeResponse.ExpiresIn;
        var stopwatch = Stopwatch.StartNew();
        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(TimeSpan.FromSeconds(deviceCodeResponse.Interval), cancellationToken);

            // Check if the user has logged in
            var pollResult = await PollDeviceFlowLoginAsync(deviceCodeResponse.DeviceCode);

            // Authentication successful
            if (pollResult.Success == true)
            {
                oauth2TokenResponse = pollResult.OAuth20TokenResponse!;
                break; // Stop polling
            }
            // Authentication failed
            if (pollResult.Success == false)
            {
                throw new YggdrasilAuthenticationException("Device flow authentication failed")
                {
                    HelpLink = deviceCodeResponse.Message
                };
            }
            // Continue polling
        }
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(timeout));

        // Time out
        if (oauth2TokenResponse is null)
        {
            throw new YggdrasilAuthenticationException("Device flow authentication timed out")
            {
                HelpLink = "The user didn't login within the time limit"
            };
        }

        // Device flow login successful
        return oauth2TokenResponse;
    }

    #region Factory Methods

    /// <summary>
    /// Create a YggdrasilOAuthAuthenticator from a public client of targeted server
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <returns></returns>
    public static async Task<YggdrasilOAuthAuthenticator> CreateFromServerPublicClientAsync(string serverUrl)
    {
        string metaRawJson = await HttpUtils.HttpClient.GetStringAsync(serverUrl);
        JsonNode baseMetaJson = JsonNode.Parse(metaRawJson) ?? throw new FormatException("Invalid meta json reponse");

        var (support, oauthMetaUrl) = IsSupportOAuthAsync(baseMetaJson, CancellationToken.None);
        if (!support) throw new NotSupportedException("The server does not support OAuth");

        string oAuthMetaRawJson = await HttpUtils.HttpClient.GetStringAsync(oauthMetaUrl);
        JsonNode jsonNode = JsonNode.Parse(oAuthMetaRawJson)
            ?? throw new FormatException("Invalid meta json response");

        if (jsonNode["shared_client_id"]?.GetValue<string>() is not string publicClientId)
            throw new NotSupportedException("The server does not support public client");

        return new YggdrasilOAuthAuthenticator(serverUrl) 
        {
            ClientId = publicClientId,
            TokenEndpoint = jsonNode["token_endpoint"]?.GetValue<string>()
                ?? throw new NotSupportedException("Invalid server token_endpoint"),
            UserInfoEndpoint = jsonNode["userinfo_endpoint"]?.GetValue<string>()
                ?? throw new NotSupportedException("Invalid server userinfo_endpoint"),
            DeviceAuthorizationEndpoint = jsonNode["device_authorization_endpoint"]?.GetValue<string>(),
            ServerName = baseMetaJson["meta"]?["serverName"]?.GetValue<string>()
        };
    }

    /// <summary>
    /// Create a YggdrasilOAuthAuthenticator from a private client of targeted server
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    /// <param name="redirectUri"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="FormatException"></exception>
    public static async Task<YggdrasilOAuthAuthenticator> CreateFromPrivateClientAsync(string serverUrl, string clientId, string? clientSecret = default, string? redirectUri = default)
    {
        string metaRawJson = await HttpUtils.HttpClient.GetStringAsync(serverUrl);
        JsonNode baseMetaJson = JsonNode.Parse(metaRawJson)
                ?? throw new FormatException("Invalid meta json reponse");

        var (support, oauthMetaUrl) = IsSupportOAuthAsync(baseMetaJson, CancellationToken.None);
        if (!support) throw new NotSupportedException("The server does not support OAuth");

        string oAuthMetaRawJson = await HttpUtils.HttpClient.GetStringAsync(oauthMetaUrl);
        JsonNode jsonNode = JsonNode.Parse(oAuthMetaRawJson)
            ?? throw new FormatException("Invalid meta json response");

        return new YggdrasilOAuthAuthenticator(serverUrl)
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            RedirectUri = redirectUri,
            TokenEndpoint = jsonNode["token_endpoint"]?.GetValue<string>()
                ?? throw new NotSupportedException("Invalid server token_endpoint"),
            UserInfoEndpoint = jsonNode["userinfo_endpoint"]?.GetValue<string>()
                ?? throw new NotSupportedException("Invalid server userinfo_endpoint"),
            DeviceAuthorizationEndpoint = jsonNode["device_authorization_endpoint"]?.GetValue<string>(),
            ServerName = baseMetaJson["meta"]?["serverName"]?.GetValue<string>()
        };
    }

    #endregion

    #region HTTP APIs

    /// <summary>
    /// Check if a yggdrasil server supports OAuth
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static (bool, string?) IsSupportOAuthAsync(JsonNode jsonNode, CancellationToken cancellationToken = default)
    {
        try
        {
            if ((jsonNode["meta"]?["feature.no_email_login"]?.GetValue<bool>()).GetValueOrDefault())
            {
                string oauthMetaUrl = jsonNode["meta"]?["feature.openid_configuration_url"]?.GetValue<string>()
                    ?? throw new FormatException("Invalid meta json reponse");

                return (true, oauthMetaUrl);
            }
        }
        catch { }

        return (false, null);
    }

    // Get OAuth2 token
    private async Task<OAuth2TokenResponse> AuthAsync(string? parameterName, string parameter, CancellationToken cancellationToken = default)
    {
        // Send OAuth2 request
        string authCodePost = $"&client_id={this.ClientId}";

        if (parameterName == "code")
        {
            authCodePost +=
                $"&grant_type=authorization_code" +
                $"&client_secret={this.ClientSecret}" +
                $"&redirect_uri={this.RedirectUri}" +
                $"&code={parameter}";
        }
        else if (parameterName == "refresh_token")
        {
            authCodePost =
                $"&grant_type=refresh_token" +
                $"&refresh_token={parameter}";

            if (!string.IsNullOrEmpty(this.ClientSecret))
                authCodePost += $"&client_secret={this.ClientSecret}";
        }
        else throw new ArgumentException("Invalid parameter name");

        using var response = await _httpClient.PostAsync(this.TokenEndpoint,
            new StringContent(authCodePost, Encoding.UTF8, "application/x-www-form-urlencoded"),
            cancellationToken);

        // Parse response
        OAuth2TokenResponse? oauth2TokenResponse = null;
        try
        {
            oauth2TokenResponse = await response
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync(AuthenticationJsonSerializerContext.Default.OAuth2TokenResponse, cancellationToken);

            if (oauth2TokenResponse is null)
                throw new FormatException("Response is null");
            if (oauth2TokenResponse.AccessToken is null || oauth2TokenResponse.RefreshToken is null)
                throw new FormatException("Token is null");
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new YggdrasilAuthenticationException("Error in getting authorization token\r\nOAuth response:\r\n" + response.Content.ReadAsString());
        }

        return oauth2TokenResponse;
    }

    // Get Minecraft Account
    private async Task<(string Name, Guid Guid)> GetMinecraftProfileAsync(string minecraftToken, CancellationToken cancellationToken = default)
    {
        // Send request
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, this.UserInfoEndpoint);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", minecraftToken);

        using var responseMessage = await _httpClient.SendAsync(requestMessage, cancellationToken);

        // Parse response
        YggdrasilResponseModel? response = null;
        Guid guid = Guid.Empty;
        try
        {
            response = await responseMessage
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync(AuthenticationJsonSerializerContext.Default.YggdrasilResponseModel, cancellationToken);

            // Check errors
            if (response is null ||
                response.SelectedProfile?.Name is null ||
                response.SelectedProfile?.Id is null ||
                !Guid.TryParse(response.SelectedProfile?.Id, out guid))
            {
                throw new FormatException("Invalid response");
            }
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new YggdrasilAuthenticationException("Error in getting the profile\n" + responseMessage.Content.ReadAsString());
        }

        return (response.SelectedProfile.Name, guid);
    }

    // Get device code for device flow authentication
    private async Task<OAuth2DeviceCodeResponse> GetDeviceCodeAsync()
    {
        // Send request
        var requestParams = $"client_id={this.ClientId}" + "&scope=openid%20offline_access%20Yggdrasil.PlayerProfiles.Select%20Yggdrasil.Server.Join";

        using var responseMessage = await _httpClient.PostAsync(this.DeviceAuthorizationEndpoint,
            new StringContent(requestParams, Encoding.UTF8, "application/x-www-form-urlencoded"));

        // Parse response
        OAuth2DeviceCodeResponse? response = null;
        try
        {
            response = await responseMessage
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync(AuthenticationJsonSerializerContext.Default.OAuth2DeviceCodeResponse);

            if (response is null)
                throw new FormatException("Response is null");
            if (response.ExpiresIn == -1 || response.Interval <= 0 || response.DeviceCode is null || response.UserCode is null)
                throw new FormatException("Invalid response");
        }
        catch (Exception e) // when (e is JsonException || e is FormatException)
        {
            throw new YggdrasilAuthenticationException("Error in device flow authentication\n" + await responseMessage.Content.ReadAsStringAsync());
        }

        return response;
    }

    // Poll for device flow authentication
    private async Task<DeviceFlowPollResult> PollDeviceFlowLoginAsync(string deviceCode)
    {
        var requestParams =
            "grant_type=urn:ietf:params:oauth:grant-type:device_code" +
            $"&client_id={this.ClientId}" +
            $"&device_code={deviceCode}";

        using var responseMessage = await _httpClient.PostAsync(this.TokenEndpoint,
            new StringContent(requestParams, Encoding.UTF8, "application/x-www-form-urlencoded"));

        // Handle errors
        if (!responseMessage.IsSuccessStatusCode)
        {
            string responseJson = await responseMessage.Content.ReadAsStringAsync();
            string error = "";
            try
            {
                error = JsonNode.Parse(responseJson)?["error"]?.GetValue<string>() ?? error;
            }
            catch { }

            bool authFailed =
                error != "authorization_pending" &&
                error != "slow_down";

            // Other errors means polling should continue, i.e. waiting for user to login
            return new DeviceFlowPollResult(authFailed ? false : null, null);
        }

        // Device flow login successful
        OAuth2TokenResponse? oauthResponse = null;
        try
        {
            oauthResponse = await responseMessage
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync(AuthenticationJsonSerializerContext.Default.OAuth2TokenResponse);

            if (oauthResponse is null)
                throw new FormatException("Response is null");
            if (oauthResponse.AccessToken is null || oauthResponse.RefreshToken is null)
                throw new FormatException("Token is null");
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new YggdrasilAuthenticationException("Error in device flow authentication\n" + await responseMessage.Content.ReadAsStringAsync());
        }

        return new DeviceFlowPollResult(true, oauthResponse);
    }

    #endregion

    Dictionary<string, string> GetMetaData(string refreshToken, int expiresIn)
    {
        Dictionary<string, string> dictionary = new()
        {
            { "authType", "OAuth" },
            { "useDeviceFlow", "true" },
            { "refresh_token", refreshToken },
            { "expires_in" , expiresIn.ToString() }
        };

        if (!string.IsNullOrEmpty(ServerName))
            dictionary.Add("server_name", ServerName);

        return dictionary;
    }
}
