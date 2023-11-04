using Nrk.FluentCore.Authentication;
using Nrk.FluentCore.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AuthException = Nrk.FluentCore.Authentication.Microsoft.MicrosoftAuthenticationException;
using AuthExceptionType = Nrk.FluentCore.Authentication.Microsoft.MicrosoftAuthenticateExceptionType;
using AuthStep = Nrk.FluentCore.Authentication.Microsoft.MicrosoftAuthenticateStep;

namespace Nrk.FluentCore.Authentication.Microsoft;

// TODO: Refactor to separate different authentication parameters and authentication steps using child classes
// TODO: separate steps into different methods
public class DefaultMicrosoftAuthenticator : IAuthenticator<MicrosoftAccount>
{
    // common
    private string _clientId;
    private string _redirectUri;
    private string? _parameterName; // indicate authtype

    // login and refresh
    private string? _code;

    // device flow
    private OAuth20TokenResponse? _oAuth20TokenResponse;
    private bool _createdFromDeviceFlow = false;

    DefaultMicrosoftAuthenticator(string? parameterName, string clientId, string redirectUri)
    {
        _parameterName = parameterName;
        _clientId = clientId;
        _redirectUri = redirectUri;
    }

    public event EventHandler<MicrosoftAuthenticateProgressChangedEventArgs>? ProgressChanged;

    public MicrosoftAccount Authenticate()
    {
        #region Get Authorization Token

        if (!_createdFromDeviceFlow) // _parameterName is "code" or "refresh_token"
        {
            ProgressChanged?.Invoke(this, (AuthStep.Get_Authorization_Token, 0.2));

            string authCodePost =
                $"client_id={_clientId}"
                + $"&{_parameterName}={_code}"
                + $"&grant_type={(_parameterName == "code" ? "authorization_code" : "refresh_token")}"
                + $"&redirect_uri={_redirectUri}";

            var authCodePostRes = HttpUtils.HttpPost(
                $"https://login.live.com/oauth20_token.srf",
                authCodePost,
                "application/x-www-form-urlencoded"
            );

            authCodePostRes.EnsureSuccessStatusCode();

            var responseJson = authCodePostRes.Content.ReadAsString();
            _oAuth20TokenResponse = JsonSerializer.Deserialize<OAuth20TokenResponse>(responseJson);
            if (_oAuth20TokenResponse is null)
                throw new AuthException("Error in getting authorization token\nOAuth response:\n" + responseJson);
        }

        #endregion

        #region Authenticate with XBL

        if (_oAuth20TokenResponse is null)
            throw new AuthException("Invalid OAuth response");

        ProgressChanged?.Invoke(this, (AuthStep.Authenticate_with_XboxLive, 0.40));

        var xBLReqModel = new XBLAuthenticateRequest();
        xBLReqModel.Properties.RpsTicket = xBLReqModel.Properties.RpsTicket.Replace(
            "<access token>",
            _oAuth20TokenResponse.AccessToken
        );

        using var xBLReqModelPostRes = HttpUtils.HttpPost(
            $"https://user.auth.xboxlive.com/user/authenticate",
            JsonSerializer.Serialize(xBLReqModel)
        );

        xBLReqModelPostRes.EnsureSuccessStatusCode();

        var xblResponse = xBLReqModelPostRes.Content.ReadAsString();
        var xBLResModel = JsonSerializer.Deserialize<XBLAuthenticateResponse>(xblResponse);
        if (xBLResModel is null || xBLResModel?.Token is null)
            throw new AuthException("Error in XBL authentication" + xblResponse);

        #endregion

        #region Authenticate with XSTS

        ProgressChanged?.Invoke(this, (AuthStep.Obtain_XSTS_token_for_Minecraft, 0.55));

        var xSTSReqModel = new XSTSAuthenticateRequest();
        xSTSReqModel.Properties.UserTokens.Add(xBLResModel.Token);

        using var xSTSReqModelPostRes = HttpUtils.HttpPost(
            $"https://xsts.auth.xboxlive.com/xsts/authorize",
            JsonSerializer.Serialize(xSTSReqModel)
        );

        if (xSTSReqModelPostRes.StatusCode.Equals(HttpStatusCode.Unauthorized))
        {
            var error = xSTSReqModelPostRes.Content.ReadAsString();
            var xSTSAuthenticateErrorModel = JsonSerializer.Deserialize<XSTSAuthenticateErrorModel>(error);

            var message = "An error occurred while verifying with Xbox Live";
            if (!string.IsNullOrEmpty(xSTSAuthenticateErrorModel?.Message))
                message += $" ({xSTSAuthenticateErrorModel.Message})";

            throw new AuthException(message)
            {
                HelpLink = xSTSAuthenticateErrorModel?.XErr switch
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
                Step = AuthStep.Obtain_XSTS_token_for_Minecraft,
                Type = AuthExceptionType.XboxLiveError
            };
        }
        ;

        xSTSReqModelPostRes.EnsureSuccessStatusCode();

        var xstsResJson = xSTSReqModelPostRes.Content.ReadAsString();
        var xSTSResModel = JsonSerializer.Deserialize<XSTSAuthenticateResponse>(xstsResJson);
        if (xSTSResModel is null || xSTSResModel?.Token is null)
            throw new AuthException("Error in XSTS authentication\n" + xSTSReqModelPostRes.Content.ReadAsString());

        #endregion

        #region Authenticate with Minecraft

        ProgressChanged?.Invoke(this, (AuthStep.Authenticate_with_Minecraft, 0.75));

        var x =
            xBLResModel.DisplayClaims?.Xui?[0]?["uhs"]?.GetValue<string>()
            ?? throw new AuthException("Error in authenticating with Minecraft\n" + xblResponse);

        string authenticateMinecraftPost = $"{{\"identityToken\":\"XBL3.0 x={x};{xSTSResModel.Token}\"}}";

        using var authenticateMinecraftPostRes = HttpUtils.HttpPost(
            $"https://api.minecraftservices.com/authentication/login_with_xbox",
            authenticateMinecraftPost
        );

        authenticateMinecraftPostRes.EnsureSuccessStatusCode();

        if (authenticateMinecraftPostRes?.Content is null)
            throw new AuthException("Error in authenticating with Minecraft\n" + authenticateMinecraftPostRes?.Content);

        string? access_token = JsonNode
            .Parse(authenticateMinecraftPostRes.Content.ReadAsString())
            ?["access_token"]?.GetValue<string>();
        if (access_token is null)
            throw new AuthException("Error in authenticating with Minecraft\n" + authenticateMinecraftPostRes.Content);

        #endregion

        var authorization = new Tuple<string, string>("Bearer", access_token);

        #region Checking Game Ownership

        ProgressChanged?.Invoke(this, (AuthStep.Checking_Game_Ownership, 0.80));

        using var checkingGameOwnershipGetRes = HttpUtils.HttpGet(
            $"https://api.minecraftservices.com/entitlements/mcstore",
            authorization
        );

        checkingGameOwnershipGetRes.EnsureSuccessStatusCode();

        var checkingGameOwnershipResJson =
            checkingGameOwnershipGetRes.Content.ReadAsString()
            ?? throw new AuthException("Error in checking game ownership\n" + checkingGameOwnershipGetRes.Content);
        var gameOwnershipItems =
            JsonNode.Parse(checkingGameOwnershipResJson)?["items"]?.AsArray()
            ?? throw new AuthException("Error in checking game ownership\n" + checkingGameOwnershipGetRes.Content);

        if (!gameOwnershipItems.Any())
            throw new AuthException("An error occurred while checking game ownership")
            {
                HelpLink = "The account doesn't own the game",
                Step = AuthStep.Checking_Game_Ownership,
                Type = AuthExceptionType.GameOwnershipError
            };

        #endregion

        #region Get the profile

        ProgressChanged?.Invoke(this, (AuthStep.Get_the_profile, 0.9));

        using var profileRes = HttpUtils.HttpGet("https://api.minecraftservices.com/minecraft/profile", authorization);

        profileRes.EnsureSuccessStatusCode();

        var microsoftAuthenticationResponse = JsonSerializer.Deserialize<MicrosoftAuthenticationResponse>(
            profileRes.Content.ReadAsString()
        );

        ProgressChanged?.Invoke(this, (AuthStep.Finished, 1.0));

        if (
            microsoftAuthenticationResponse is null
            || microsoftAuthenticationResponse.Name is null
            || microsoftAuthenticationResponse.Id is null
            || _oAuth20TokenResponse?.RefreshToken is null
        )
            throw new AuthException("Error in getting the profile\n" + profileRes.Content);

        return new MicrosoftAccount(
            Name: microsoftAuthenticationResponse.Name,
            Uuid: Guid.Parse(microsoftAuthenticationResponse.Id),
            AccessToken: access_token,
            RefreshToken: _oAuth20TokenResponse.RefreshToken,
            LastRefreshTime: DateTime.Now
        );

        #endregion
    }

    MicrosoftAccount[] IAuthenticator<MicrosoftAccount>.Authenticate() => new[] { Authenticate() };

    #region Factory Methods

    public static DefaultMicrosoftAuthenticator CreateForLogin(string clientId, string redirectUri, string code) =>
        new("code", clientId, redirectUri) { _code = code };

    public static DefaultMicrosoftAuthenticator CreateForRefresh(
        string clientId,
        string redirectUri,
        MicrosoftAccount account
    ) => new("refresh_token", clientId, redirectUri) { _code = account.RefreshToken, };

    public static DefaultMicrosoftAuthenticator CreateFromDeviceFlow(
        string clientId,
        string redirectUri,
        OAuth20TokenResponse oAuth20TokenResponseModel
    ) =>
        new(null, clientId, redirectUri)
        {
            _oAuth20TokenResponse = oAuth20TokenResponseModel,
            _createdFromDeviceFlow = true
        };

    #endregion

    public static Task<DeviceFlowResponse> DeviceFlowAuthAsync(
        string clientId,
        Action<DeviceCodeResponse> ReceiveUserCodeAction,
        out CancellationTokenSource cancellationTokenSource
    )
    {
        cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        return Task.Run(
            async () =>
            {
                var deviceAuthPost = $"client_id={clientId}" + "&scope=XboxLive.signin%20offline_access";

                using var deviceAuthPostRes = HttpUtils.HttpPost(
                    $"https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode",
                    deviceAuthPost,
                    "application/x-www-form-urlencoded"
                );

                var deviceAuthResponse = JsonSerializer.Deserialize<DeviceCodeResponse>(
                    await deviceAuthPostRes.Content.ReadAsStringAsync()
                );

                if (
                    deviceAuthResponse is null
                    || deviceAuthResponse.ExpiresIn is null
                    || deviceAuthResponse.Interval is null
                )
                    throw new AuthException("Error in device flow authentication\n" + deviceAuthPostRes.Content);

                ReceiveUserCodeAction(deviceAuthResponse);

                var stopwatch = Stopwatch.StartNew();

                while (stopwatch.Elapsed < TimeSpan.FromSeconds((double)deviceAuthResponse.ExpiresIn))
                {
                    if (token.IsCancellationRequested)
                        break;

                    await Task.Delay((int)deviceAuthResponse.Interval * 1000);

                    var pollingPost =
                        "grant_type=urn:ietf:params:oauth:grant-type:device_code"
                        + $"&client_id={clientId}"
                        + $"&device_code={deviceAuthResponse.DeviceCode}";

                    using var pollingPostRes = HttpUtils.HttpPost(
                        $"https://login.microsoftonline.com/consumers/oauth2/v2.0/token",
                        pollingPost,
                        "application/x-www-form-urlencoded"
                    );
                    var pollingPostJson = JsonNode.Parse(await pollingPostRes.Content.ReadAsStringAsync());
                    if (pollingPostJson is null)
                        throw new AuthException("Error in device flow authentication\n" + pollingPostRes.Content);

                    if (pollingPostRes.IsSuccessStatusCode)
                        return new DeviceFlowResponse()
                        {
                            Success = true,
                            OAuth20TokenResponse =
                                pollingPostJson.Deserialize<OAuth20TokenResponse>()
                                ?? throw new AuthException(
                                    "Error in device flow authentication\n" + pollingPostRes.Content
                                )
                        };
                    else
                    {
                        var error = (string?)pollingPostJson["error"];
                        if (
                            error == "authorization_declined"
                            || error == "bad_verification_code"
                            || error == "expired_token"
                        )
                            break;
                    }
                }

                stopwatch.Stop();

                return new DeviceFlowResponse() { Success = false };
            },
            token
        );
    }
}
