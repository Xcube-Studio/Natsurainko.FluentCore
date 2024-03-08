using Nrk.FluentCore.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using AuthException = Nrk.FluentCore.Authentication.MicrosoftAuthenticationException;
using AuthExceptionType = Nrk.FluentCore.Authentication.MicrosoftAuthenticationExceptionType;
using AuthStep = Nrk.FluentCore.Authentication.MicrosoftAuthenticationProgress;

namespace Nrk.FluentCore.Authentication;

public class MicrosoftAuthenticator
{
    private readonly HttpClient _httpClient;

    private readonly string _clientId;
    private readonly string _redirectUri;

    public MicrosoftAuthenticator(string clientId, string redirectUri, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? HttpUtils.HttpClient;

        _clientId = clientId;
        _redirectUri = redirectUri;
    }

    public async Task<MicrosoftAccount> LoginAsync(string code, IProgress<AuthStep>? progress = null)
    {
        progress?.Report(AuthStep.AuthenticatingMicrosoftAccount);
        var msaOAuthTokens = await AuthMsaAsync("code", code);

        return await AuthenticateCommonAsync(msaOAuthTokens.AccessToken, msaOAuthTokens.RefreshToken, progress);
    }

    public async Task<MicrosoftAccount> LoginAsync(OAuth2Tokens msaTokens, IProgress<AuthStep>? progress = null)
        => await AuthenticateCommonAsync(msaTokens.AccessToken, msaTokens.RefreshToken, progress);

    public async Task<MicrosoftAccount> RefreshAsync(MicrosoftAccount account, IProgress<AuthStep>? progress = null)
    {
        progress?.Report(AuthStep.AuthenticatingMicrosoftAccount);
        var msaTokens = await AuthMsaAsync("refresh_token", account.RefreshToken);

        return await AuthenticateCommonAsync(msaTokens.AccessToken, msaTokens.RefreshToken, progress);
    }

    /// <summary>
    /// Authenticate with Microsoft Account using OAuth2 device flow and poll for user login
    /// </summary>
    /// <param name="receiveUserCodeAction">Function called when user code is received</param>
    /// <param name="cancellationToken">Token for cancelling device flow authentication process</param>
    /// <returns>Microsoft Account OAuth tokens</returns>
    /// <exception cref="AuthException">Error in authenticating Microsoft Account using OAuth2 device flow</exception>
    public async Task<OAuth2Tokens> AuthMsaFromDeviceFlowAsync(
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
                throw new AuthException("Device flow authentication failed")
                {
                    HelpLink = deviceCodeResponse.Message,
                    Step = AuthStep.AuthenticatingMicrosoftAccount,
                    Type = AuthExceptionType.DeviceFlowError
                };
            }
            // Continue polling
        }
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(timeout));

        // Time out
        if (oauth2TokenResponse is null)
        {
            throw new AuthException("Device flow authentication timed out")
            {
                HelpLink = "The user didn't login within the time limit",
                Step = AuthStep.AuthenticatingMicrosoftAccount,
                Type = AuthExceptionType.DeviceFlowError
            };
        }

        // Device flow login successful
        return new OAuth2Tokens(oauth2TokenResponse.AccessToken, oauth2TokenResponse.RefreshToken);
    }

    // Common authentication process
    private async Task<MicrosoftAccount> AuthenticateCommonAsync(
        string msaAccessToken,
        string msaRefreshToken,
        IProgress<AuthStep>? progress = null)
    {
        progress?.Report(AuthStep.AuthenticatingWithXboxLive);
        var xblResponse = await AuthXboxLiveAsync(msaAccessToken);

        progress?.Report(AuthStep.AuthenticatingWithXsts);
        string xstsToken = await AuthXstsAsync(xblResponse.Token);

        progress?.Report(AuthStep.AuthenticatingMinecraftAccount);
        string minecraftToken = await AuthMinecraftAsync(xblResponse, xstsToken);

        progress?.Report(AuthStep.CheckingGameOwnership);
        await EnsureGameOwnershipAsync(minecraftToken);

        progress?.Report(AuthStep.GettingMinecraftProfile);
        var (name, guid) = await GetMinecraftProfileAsync(minecraftToken);

        progress?.Report(AuthStep.Finish);
        return new MicrosoftAccount(
            name,
            guid,
            minecraftToken,
            msaRefreshToken,
            DateTime.Now
        );
    }


    #region HTTP APIs

    // Get Microsoft Account OAuth2 token
    private async Task<OAuth2TokenResponse> AuthMsaAsync(string? parameterName, string code)
    {
        // Send OAuth2 request
        string authCodePost =
            $"client_id={_clientId}" +
            $"&{parameterName}={code}" +
            $"&grant_type={(parameterName == "code" ? "authorization_code" : "refresh_token")}" +
            $"&redirect_uri={_redirectUri}";

        using var response = await _httpClient.PostAsync(
            "https://login.live.com/oauth20_token.srf",
            new StringContent(authCodePost, Encoding.UTF8, "application/x-www-form-urlencoded")
            );

        // Parse response
        OAuth2TokenResponse? oauth2TokenResponse = null;
        try
        {
            oauth2TokenResponse = await response
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync<OAuth2TokenResponse>();

            if (oauth2TokenResponse is null)
                throw new FormatException("Response is null");
            if (oauth2TokenResponse.AccessToken is null || oauth2TokenResponse.RefreshToken is null)
                throw new FormatException("Token is null");
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new AuthException("Error in getting authorization token\nOAuth response:\n" + response.Content.ReadAsString());
        }

        return oauth2TokenResponse;
    }

    // Get Xbox Live token
    private async Task<XBLAuthenticateResponse> AuthXboxLiveAsync(string msaToken)
    {
        // Send XBL auth request
        var xblRequest = new XBLAuthenticateRequest();
        xblRequest.Properties.RpsTicket = xblRequest.Properties.RpsTicket.Replace(
            "<access token>",
            msaToken);

        using var xblResponseMessage = await _httpClient.PostAsJsonAsync(
            "https://user.auth.xboxlive.com/user/authenticate",
            xblRequest);

        // Parse response
        XBLAuthenticateResponse? xblResponse = null;
        try {
            xblResponse = await xblResponseMessage
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync<XBLAuthenticateResponse>();

            if (xblResponse is null)
                throw new FormatException("Response is null");
            if (xblResponse.Token is null)
                throw new FormatException("Token is null");
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new AuthException("Error in XBL authentication" + xblResponse);
        }

        return xblResponse;
    }

    // Get XSTS token
    private async Task<string> AuthXstsAsync(string xblToken)
    {
        // Send XSTS auth request
        var xstsAuthRequest = new XSTSAuthenticateRequest();
        xstsAuthRequest.Properties.UserTokens.Add(xblToken);

        using var xstsResponseMessage = await _httpClient.PostAsJsonAsync(
            "https://xsts.auth.xboxlive.com/xsts/authorize",
            xstsAuthRequest);

        // Handle errors
        if (xstsResponseMessage.StatusCode.Equals(HttpStatusCode.Unauthorized))
        {
            var xstsErrorResponse = await xstsResponseMessage.Content
                .ReadFromJsonAsync<XSTSAuthenticateErrorModel>();

            string message = "An error occurred while verifying with Xbox Live";
            if (!string.IsNullOrEmpty(xstsErrorResponse?.Message))
                message += $" ({xstsErrorResponse.Message})";

            throw new AuthException(message)
            {
                HelpLink = xstsErrorResponse?.XErr switch
                {
                    2148916233
                        => "The account doesn't have an Xbox account. Once they sign up for one (or login through minecraft.net to create one) "
                            + "then they can proceed with the login. This shouldn't happen with accounts that have purchased Minecraft with a Microsoft account, "
                            + "as they would've already gone through that Xbox signup process.",
                    2148916235 => "The account is from a country where Xbox Live is not available/banned",
                    2148916236 => "The account needs adult verification on Xbox page. (South Korea)",
                    2148916237 => "The account needs adult verification on Xbox page. (South Korea)",
                    2148916238
                        => "The account is a child (under 18) and cannot proceed unless the account is added to a Family by an adult. "
                            + "This only seems to occur when using a custom Microsoft Azure application. When using the Minecraft launchers client id, "
                            + "this doesn't trigger.",
                    _ => "Unknown error"
                },
                Step = AuthStep.AuthenticatingWithXsts,
                Type = AuthExceptionType.XboxLiveError
            };
        }

        // Parse response
        XSTSAuthenticateResponse? xstsResponse = null;
        try
        {
            xstsResponse = await xstsResponseMessage
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync<XSTSAuthenticateResponse>();

            if (xstsResponse is null)
                throw new FormatException("Response is null");
            if (xstsResponse.Token is null)
                throw new FormatException("Token is null");
}
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new AuthException("Error in XSTS authentication\n" + xstsResponseMessage.Content.ReadAsString());
        }

        return xstsResponse.Token;
    }

    // Get Minecraft Account token
    private async Task<string> AuthMinecraftAsync(XBLAuthenticateResponse xblResponse, string xstsToken)
    {
        // Send Minecraft auth request
        string? x = null;
        try
        {
            x = xblResponse.DisplayClaims?.Xui?[0]?["uhs"]?.GetValue<string>()
                ?? throw new FormatException();
        }
        catch (Exception e) when (e is FormatException || e is InvalidOperationException)
        {
            throw new AuthException("Error in authenticating with XBL\n" + JsonSerializer.Serialize(xblResponse));
        }

        string requestContent = $"{{\"identityToken\":\"XBL3.0 x={x};{xstsToken}\"}}";

        using var responseMessage = await _httpClient.PostAsync(
            "https://api.minecraftservices.com/authentication/login_with_xbox",
            new StringContent(requestContent, Encoding.UTF8, "application/json"));

        // Parse response
        string responseString = await responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();

        string? accessToken = null;
        try
        {
            accessToken = JsonNode.Parse(responseString)?["access_token"]?.GetValue<string>()
                ?? throw new FormatException("Unable to parse access token");
        }
        catch (Exception e) when (e is FormatException || e is InvalidOperationException)
        {
            throw new AuthException("Error in authenticating with Minecraft\n" + responseString);
        }
        return accessToken;
    }

    // Check if the Minecraft Account owns the game
    private async Task EnsureGameOwnershipAsync(string minecraftToken)
    {
        // Send request
        var requestMessage = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.minecraftservices.com/entitlements/mcstore");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", minecraftToken);

        using var responseMessage = await _httpClient.SendAsync(requestMessage);

        // Check response
        string responseString = await responseMessage
            .EnsureSuccessStatusCode().Content
            .ReadAsStringAsync();

        try
        {
            var gameOwnershipItems = JsonNode.Parse(responseString)?["items"]?.AsArray();
            if (gameOwnershipItems is null || !gameOwnershipItems.Any())
                throw new InvalidOperationException();
        }
        catch (Exception e) when (e is JsonException || e is InvalidOperationException)
        {
            throw new AuthException("An error occurred while checking game ownership\n" + responseString)
            {
                HelpLink = "The account doesn't own the game",
                Step = AuthStep.CheckingGameOwnership,
                Type = AuthExceptionType.GameOwnershipError
            };
        }

        // The account owns Minecraft
    }

    // Get Minecraft Account
    private async Task<(string Name, Guid Guid)> GetMinecraftProfileAsync(string minecraftToken)
    {
        // Send request
        var requestMessage = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.minecraftservices.com/minecraft/profile");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", minecraftToken);

        using var responseMessage = await _httpClient.SendAsync(requestMessage);

        // Parse response
        MicrosoftAuthenticationResponse? response = null;
        Guid guid = Guid.Empty;
        try
        {
            response = await responseMessage
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync<MicrosoftAuthenticationResponse>();

            // Check errors
            if (response is null ||
                response.Name is null ||
                response.Id is null ||
                !Guid.TryParse(response.Id, out guid))
            {
                   throw new FormatException("Invalid response");
            }
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new AuthException("Error in getting the profile\n" + responseMessage.Content.ReadAsString());
        }

        return (response.Name, guid);
    }

    // Get device code for device flow authentication
    private async Task<OAuth2DeviceCodeResponse> GetDeviceCodeAsync()
    {
        // Send request
        var requestParams = $"client_id={_clientId}" + "&scope=XboxLive.signin%20offline_access";

        using var responseMessage = await _httpClient.PostAsync(
            "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode",
            new StringContent(requestParams, Encoding.UTF8, "application/x-www-form-urlencoded"));

        // Parse response
        OAuth2DeviceCodeResponse? response = null;
        try
        {
            response = await responseMessage
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync<OAuth2DeviceCodeResponse>();

            if (response is null)
                throw new FormatException("Response is null");
            if (response.ExpiresIn == -1 || response.Interval <= 0 || response.DeviceCode is null)
                throw new FormatException("Invalid response");
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new AuthException("Error in device flow authentication\n" + responseMessage.Content);
        }

        return response;
    }

    // Poll for device flow authentication
    private async Task<DeviceFlowPollResult> PollDeviceFlowLoginAsync(string deviceCode)
    {
        var requestParams =
            "grant_type=urn:ietf:params:oauth:grant-type:device_code" +
            $"&client_id={_clientId}" +
            $"&device_code={deviceCode}";

        using var responseMessage = await _httpClient.PostAsync(
            "https://login.microsoftonline.com/consumers/oauth2/v2.0/token",
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
                error == "authorization_declined" ||
                error == "bad_verification_code" ||
                error == "expired_token";

            // Other errors means polling should continue, i.e. waiting for user to login
            return new DeviceFlowPollResult(authFailed ? false : null, null);
        }

        // Device flow login successful
        OAuth2TokenResponse? oauthResponse = null;
        try
        {
            oauthResponse = await responseMessage
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync<OAuth2TokenResponse>();

            if (oauthResponse is null)
                throw new FormatException("Response is null");
            if (oauthResponse.AccessToken is null || oauthResponse.RefreshToken is null)
                throw new FormatException("Token is null");
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new AuthException("Error in device flow authentication\n" + responseMessage.Content);
        }

        return new DeviceFlowPollResult(true, oauthResponse);
    }

    #endregion
}
