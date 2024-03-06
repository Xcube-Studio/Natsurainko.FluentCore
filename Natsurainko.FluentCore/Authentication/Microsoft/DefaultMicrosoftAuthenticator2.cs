﻿using Nrk.FluentCore.Utils;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using AuthException = Nrk.FluentCore.Authentication.Microsoft.MicrosoftAccountAuthenticationException;
using AuthExceptionType = Nrk.FluentCore.Authentication.Microsoft.MicrosoftAccountAuthenticationExceptionType;
using AuthStep = Nrk.FluentCore.Authentication.Microsoft.MicrosoftAccountAuthenticationProgress;

namespace Nrk.FluentCore.Authentication.Microsoft;

public class DefaultMicrosoftAuthenticator2
{
    private readonly HttpClient _httpClient;

    private string _clientId;
    private string _redirectUri;

    public DefaultMicrosoftAuthenticator2(string clientId, string redirectUri, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? HttpUtils.HttpClient;

        _clientId = clientId;
        _redirectUri = redirectUri;
    }

    public Task<MicrosoftAccount> LoginAsync(string code, IProgress<AuthStep>? progress = null)
        => AuthenticateAsync(code, "code", progress);

    public Task<MicrosoftAccount> RefreshAsync(MicrosoftAccount account, IProgress<AuthStep>? progress = null)
        => AuthenticateAsync(account.RefreshToken, "refresh_token", progress);

    // Common authentication process
    private async Task<MicrosoftAccount> AuthenticateAsync(
        string code,
        string param,
        IProgress<AuthStep>? progress = null)
    {
        progress?.Report(AuthStep.AuthenticatingMicrosoftAccount);
        (string msaToken, string msaRefreshToken) = await AuthMsaAsync(param, code);

        progress?.Report(AuthStep.AuthenticatingWithXboxLive);
        var xblResponse = await AuthXboxLiveAsync(msaToken);

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
    private async Task<(string msaAccessToken, string msaRefreshToken)> AuthMsaAsync(string parameterName, string code)
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
        OAuth20TokenResponse? oauth2TokenResponse = null;
        try
        {
            oauth2TokenResponse = await response
                .EnsureSuccessStatusCode().Content
                .ReadFromJsonAsync<OAuth20TokenResponse>();

            if (oauth2TokenResponse is null)
                throw new FormatException("Response is null");
            if (oauth2TokenResponse.AccessToken is null || oauth2TokenResponse.RefreshToken is null)
                throw new FormatException("Token is null");
        }
        catch (Exception e) when (e is JsonException || e is FormatException)
        {
            throw new AuthException("Error in getting authorization token\nOAuth response:\n" + response.Content.ReadAsString());
        }

        return (oauth2TokenResponse.AccessToken, oauth2TokenResponse.RefreshToken);
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

    #endregion
}
