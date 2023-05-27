using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Auth;
using Natsurainko.Toolkits.Network;
using Natsurainko.Toolkits.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AuthException = Natsurainko.FluentCore.Exceptions.MicrosoftAuthenticationException;

namespace Natsurainko.FluentCore.Module.Authenticator;

public class MicrosoftAuthenticator : IAuthenticator
{
    public string ClientId { get; set; } = "00000000402b5328";

    public string RedirectUri { get; set; } = "https://login.live.com/oauth20_desktop.srf";

    public string Code { get; set; }

    public OAuth20TokenResponseModel OAuth20TokenResponse { get; private set; }

    public AuthenticatorMethod Method { get; private set; } = AuthenticatorMethod.Login;

    public bool CreatedFromDeviceCodeFlow { get; private set; } = false;


    public event EventHandler<(float, string)> ProgressChanged;

    public MicrosoftAuthenticator() { }

    public MicrosoftAuthenticator(
        string code,
        AuthenticatorMethod method = AuthenticatorMethod.Login)
    {
        Code = code;
        Method = method;
    }

    public MicrosoftAuthenticator(
        string clientId,
        string redirectUri,
        AuthenticatorMethod method = AuthenticatorMethod.Login)
    {
        ClientId = clientId;
        RedirectUri = redirectUri;
        Method = method;
    }

    public MicrosoftAuthenticator(
        string code,
        string clientId,
        string redirectUri,
        AuthenticatorMethod method = AuthenticatorMethod.Login)
    {
        Code = code;
        ClientId = clientId;
        RedirectUri = redirectUri;
        Method = method;
    }

    public MicrosoftAuthenticator(
        OAuth20TokenResponseModel oAuth20TokenResponseModel,
        string clientId,
        string redirectUri)
    {
        OAuth20TokenResponse = oAuth20TokenResponseModel;
        ClientId = clientId;
        RedirectUri = redirectUri;

        CreatedFromDeviceCodeFlow = true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Natsurainko.FluentCore.Exceptions.MicrosoftAuthenticationException"></exception>
    public IAccount Authenticate()
        => AuthenticateAsync().GetAwaiter().GetResult();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Natsurainko.FluentCore.Exceptions.MicrosoftAuthenticationException"></exception>
    public async Task<IAccount> AuthenticateAsync()
    {
        static void EnsureSuccessStatusCode(HttpResponseMessage httpResponseMessage, MicrosoftAuthenticationStep step)
        {
            try
            {
                httpResponseMessage.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                var message = $"An error occurred while connecting to the \"{httpResponseMessage.RequestMessage.RequestUri}\"";
                if (!string.IsNullOrEmpty(ex.Message))
                    message += $"\r\n{ex.Message}";

                string data = null;
                if (httpResponseMessage.Content.Headers.ContentLength > 0)
                {
                    var @string = httpResponseMessage.Content.ReadAsStringAsync();
                    @string.Wait();

                    data = @string.Result;
                }

                httpResponseMessage.Dispose();

                throw new AuthException
                {
                    Message = message,
                    InnerException = ex,
                    HelpLink = "Please check your network connection and try again",
                    Step = step,
                    Type = MicrosoftAuthenticationExceptionType.NetworkConnectionError,
                    Data =
                    {
                        { "HttpResponseMessage.Content", data }
                    },
                };
            }
        }

        static HttpResponseMessage HttpTaskContinueWith(Task<HttpResponseMessage> task, MicrosoftAuthenticationStep step, bool ensureSuccessStatusCode = true)
        {
            if (task.IsFaulted)
            {
                var message = $"Network request failed";
                if (!string.IsNullOrEmpty(task.Exception.Message))
                    message += "\r\n" + task.Exception.Message;

                throw new AuthException
                {
                    Message = message,
                    InnerException = task.Exception,
                    HelpLink = "Please check your network connection and try again",
                    Step = step,
                    Type = MicrosoftAuthenticationExceptionType.NetworkConnectionError
                };
            }

            if (!ensureSuccessStatusCode)
                return task.Result;

            var httpResponseMessage = task.Result;
            EnsureSuccessStatusCode(httpResponseMessage, step);

            return httpResponseMessage;
        }

        #region Get Authorization Token

        if (!CreatedFromDeviceCodeFlow)
        {
            ProgressChanged?.Invoke(this, (0.20f, "Getting Authorization Token"));

            string authCodePost =
                $"client_id={ClientId}" +
                $"&{(Method == AuthenticatorMethod.Login ? "code" : "refresh_token")}={Code}" +
                $"&grant_type={(Method == AuthenticatorMethod.Login ? "authorization_code" : "refresh_token")}" +
                $"&redirect_uri={RedirectUri}";

            var authCodePostRes = await HttpWrapper.HttpPostAsync(
                $"https://login.live.com/oauth20_token.srf", 
                authCodePost, 
                "application/x-www-form-urlencoded")
                .ContinueWith(task => HttpTaskContinueWith(task, MicrosoftAuthenticationStep.Get_Authorization_Token));

            OAuth20TokenResponse = JsonConvert.DeserializeObject<OAuth20TokenResponseModel>(await authCodePostRes.Content.ReadAsStringAsync());
        }

        #endregion

        #region Authenticate with XBL

        ProgressChanged?.Invoke(this, (0.40f, "Authenticating with XBL"));

        var xBLReqModel = new XBLAuthenticateRequestModel();
        xBLReqModel.Properties.RpsTicket = xBLReqModel.Properties.RpsTicket.Replace("<access token>", OAuth20TokenResponse.AccessToken);

        using var xBLReqModelPostRes = await HttpWrapper.HttpPostAsync(
            $"https://user.auth.xboxlive.com/user/authenticate", 
            xBLReqModel.ToJson())
            .ContinueWith(task => HttpTaskContinueWith(task, MicrosoftAuthenticationStep.Authenticate_with_XboxLive));

        var xBLResModel = JsonConvert.DeserializeObject<XBLAuthenticateResponseModel>(await xBLReqModelPostRes.Content.ReadAsStringAsync());

        #endregion

        #region Authenticate with XSTS

        ProgressChanged?.Invoke(this, (0.55f, "Authenticating with XSTS"));

        var xSTSReqModel = new XSTSAuthenticateRequestModel();
        xSTSReqModel.Properties.UserTokens.Add(xBLResModel.Token);

        using var xSTSReqModelPostRes = await HttpWrapper.HttpPostAsync(
            $"https://xsts.auth.xboxlive.com/xsts/authorize", 
            xSTSReqModel.ToJson()).
            ContinueWith(task => HttpTaskContinueWith(task, MicrosoftAuthenticationStep.Obtain_XSTS_token_for_Minecraft, false));

        if (xSTSReqModelPostRes.StatusCode.Equals(HttpStatusCode.Unauthorized))
        {
            var xSTSAuthenticateErrorModel = JsonConvert.DeserializeObject<XSTSAuthenticateErrorModel>(await xSTSReqModelPostRes.Content.ReadAsStringAsync());

            var message = "An error occurred while verifying with Xbox Live";
            if (!string.IsNullOrEmpty(xSTSAuthenticateErrorModel.Message))
                message += $" ({xSTSAuthenticateErrorModel.Message})";

            throw new AuthException
            {
                Message = message,
                HelpLink = xSTSAuthenticateErrorModel.XErr switch
                {
                    "2148916233" => "The account doesn't have an Xbox account. Once they sign up for one (or login through minecraft.net to create one) " +
                        "then they can proceed with the login. This shouldn't happen with accounts that have purchased Minecraft with a Microsoft account, " +
                        "as they would've already gone through that Xbox signup process.",
                    "2148916235" => "The account is from a country where Xbox Live is not available/banned",
                    "2148916236" => "The account needs adult verification on Xbox page. (South Korea)",
                    "2148916237" => "The account needs adult verification on Xbox page. (South Korea)",
                    "2148916238" => "The account is a child (under 18) and cannot proceed unless the account is added to a Family by an adult. " +
                        "This only seems to occur when using a custom Microsoft Azure application. When using the Minecraft launchers client id, " +
                        "this doesn't trigger.",
                    _ => string.Empty
                },
                Step = MicrosoftAuthenticationStep.Obtain_XSTS_token_for_Minecraft,
                Type = MicrosoftAuthenticationExceptionType.XboxLiveError
            };
        }
        else EnsureSuccessStatusCode(xSTSReqModelPostRes, MicrosoftAuthenticationStep.Obtain_XSTS_token_for_Minecraft);

        var xSTSResModel = JsonConvert.DeserializeObject<XSTSAuthenticateResponseModel>(await xSTSReqModelPostRes.Content.ReadAsStringAsync());

        #endregion

        #region Authenticate with Minecraft

        ProgressChanged?.Invoke(this, (0.75f, "Authenticating with Minecraft"));

        string authenticateMinecraftPost =
            $"{{\"identityToken\":\"XBL3.0 x={xBLResModel.DisplayClaims.Xui[0]["uhs"]};{xSTSResModel.Token}\"}}";

        using var authenticateMinecraftPostRes = await HttpWrapper.HttpPostAsync(
            $"https://api.minecraftservices.com/authentication/login_with_xbox", 
            authenticateMinecraftPost)
            .ContinueWith(task => HttpTaskContinueWith(task, MicrosoftAuthenticationStep.Authenticate_with_Minecraft));
        
        string access_token = (string)JObject.Parse(await authenticateMinecraftPostRes.Content.ReadAsStringAsync())["access_token"];

        #endregion

        var authorization = new Tuple<string, string>("Bearer", access_token);

        #region Checking Game Ownership

        ProgressChanged?.Invoke(this, (0.80f, "Checking Game Ownership"));

        using var checkingGameOwnershipGetRes = await HttpWrapper.HttpGetAsync(
            $"https://api.minecraftservices.com/entitlements/mcstore",
            authorization)
            .ContinueWith(task => HttpTaskContinueWith(task, MicrosoftAuthenticationStep.Checking_Game_Ownership));

        var gameOwnershipItems = JObject.Parse(await checkingGameOwnershipGetRes.Content.ReadAsStringAsync())["items"].ToObject<JArray>();

        if (!gameOwnershipItems.Any())
            throw new AuthException
            {
                Message = "An error occurred while checking game ownership",
                HelpLink = "The account doesn't own the game",
                Step = MicrosoftAuthenticationStep.Checking_Game_Ownership,
                Type = MicrosoftAuthenticationExceptionType.GameOwnershipError
            };

        #endregion

        #region Get the profile

        ProgressChanged?.Invoke(this, (0.9f, "Getting the profile"));

        using var profileRes = await HttpWrapper.HttpGetAsync(
            "https://api.minecraftservices.com/minecraft/profile", 
            authorization)
            .ContinueWith(task => HttpTaskContinueWith(task, MicrosoftAuthenticationStep.Get_the_profile));
        
        var microsoftAuthenticationResponse = JsonConvert.DeserializeObject<MicrosoftAuthenticationResponse>(await profileRes.Content.ReadAsStringAsync());

        ProgressChanged?.Invoke(this, (1.0f, "Finished"));

        return new MicrosoftAccount
        {
            AccessToken = access_token,
            ClientToken = Guid.NewGuid().ToString("N"),
            Name = microsoftAuthenticationResponse.Name,
            Uuid = Guid.Parse(microsoftAuthenticationResponse.Id),
            RefreshToken = OAuth20TokenResponse.RefreshToken,
            DateTime = DateTime.Now
        };

        #endregion
    }

    public static async Task<DeviceFlowAuthResult> DeviceFlowAuthAsync
        (string clientId, Action<DeviceAuthorizationResponse> ReceiveUserCodeAction)
    {
        try
        {
            var deviceAuthPost =
                $"client_id={clientId}" +
                "&scope=XboxLive.signin%20offline_access";

            var deviceAuthPostRes = await HttpWrapper.HttpPostAsync
                ($"https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode", deviceAuthPost, "application/x-www-form-urlencoded");
            var deviceAuthResponse = JsonConvert.DeserializeObject<DeviceAuthorizationResponse>(await deviceAuthPostRes.Content.ReadAsStringAsync());
            ReceiveUserCodeAction(deviceAuthResponse);

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < TimeSpan.FromSeconds(deviceAuthResponse.ExpiresIn))
            {
                await Task.Delay(deviceAuthResponse.Interval * 1000);

                var pollingPost =
                    "grant_type=urn:ietf:params:oauth:grant-type:device_code" +
                    $"&client_id={clientId}" +
                    $"&device_code={deviceAuthResponse.DeviceCode}";

                var pollingPostRes = await HttpWrapper.HttpPostAsync
                    ($"https://login.microsoftonline.com/consumers/oauth2/v2.0/token", pollingPost, "application/x-www-form-urlencoded");
                var pollingPostJson = JObject.Parse(await pollingPostRes.Content.ReadAsStringAsync());

                if (pollingPostRes.IsSuccessStatusCode)
                    return new()
                    {
                        Success = true,
                        OAuth20TokenResponse = pollingPostJson.ToObject<OAuth20TokenResponseModel>()
                    };
                else
                {
                    var error = (string)pollingPostJson["error"];
                    if (error.Equals("authorization_declined") ||
                        error.Equals("bad_verification_code") ||
                        error.Equals("expired_token"))
                        break;
                }
            }

            stopwatch.Stop();
        }
        catch (Exception ex) { }

        return new()
        {
            Success = false
        };
    }
}
