using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Natsurainko.FluentCore.Model.Auth;

public class DisplayClaimsModel
{
    [JsonProperty("xui")]
    public List<JObject> Xui { get; set; }
}

public class OAuth20TokenResponseModel
{
    [JsonProperty("token_type")]
    public string TokenType { get; set; }

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonProperty("scope")]
    public string Scope { get; set; }

    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonProperty("user_id")]
    public string UserId { get; set; }

    [JsonProperty("foci")]
    public string Foci { get; set; }
}

public class XBLAuthenticateRequestModel
{
    [JsonProperty("Properties")]
    public XBLAuthenticatePropertiesModel Properties { get; set; } = new XBLAuthenticatePropertiesModel();

    [JsonProperty("RelyingParty")]
    public string RelyingParty { get; set; } = "http://auth.xboxlive.com";

    [JsonProperty("TokenType")]
    public string TokenType { get; set; } = "JWT";
}

public class XBLAuthenticateResponseModel
{
    [JsonProperty("IssueInstant")]
    public string IssueInstant { get; set; }

    [JsonProperty("NotAfter")]
    public string NotAfter { get; set; }

    [JsonProperty("Token")]
    public string Token { get; set; }

    [JsonProperty("DisplayClaims")]
    public DisplayClaimsModel DisplayClaims { get; set; }
}

public class XBLAuthenticatePropertiesModel
{
    [JsonProperty("AuthMethod")]
    public string AuthMethod { get; set; } = "RPS";

    [JsonProperty("SiteName")]
    public string SiteName { get; set; } = "user.auth.xboxlive.com";

    [JsonProperty("RpsTicket")]
    public string RpsTicket { get; set; } = "d=<access token>";
}

public class XSTSAuthenticateRequestModel
{
    [JsonProperty("Properties")]
    public XSTSAuthenticatePropertiesModels Properties { get; set; } = new XSTSAuthenticatePropertiesModels();

    [JsonProperty("RelyingParty")]
    public string RelyingParty { get; set; } = "rp://api.minecraftservices.com/";

    [JsonProperty("TokenType")]
    public string TokenType { get; set; } = "JWT";
}

public class XSTSAuthenticateResponseModel
{
    [JsonProperty("IssueInstant")]
    public string IssueInstant { get; set; }

    [JsonProperty("NotAfter")]
    public string NotAfter { get; set; }

    [JsonProperty("Token")]
    public string Token { get; set; }

    [JsonProperty("DisplayClaims")]
    public DisplayClaimsModel DisplayClaims { get; set; }
}

public class XSTSAuthenticateErrorModel
{
    [JsonProperty("Identity")]
    public string Identity { get; set; }

    [JsonProperty("XErr")]
    public string XErr { get; set; }

    [JsonProperty("Message")]
    public string Message { get; set; }

    [JsonProperty("Redirect")]
    public string Redirect { get; set; }
}

public class XSTSAuthenticatePropertiesModels
{
    [JsonProperty("SandboxId")]
    public string SandboxId { get; set; } = "RETAIL";

    [JsonProperty("UserTokens")]
    public List<string> UserTokens { get; set; } = new List<string>();
}

public class MicrosoftAuthenticationResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("skins")]
    public List<SkinModel> Skins { get; set; }

    [JsonProperty("capes")]
    public JArray Capes { get; set; }
}

public class SkinModel
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("state")]
    public string State { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("variant")]
    public string Variant { get; set; }

    [JsonProperty("alias")]
    public string Alias { get; set; }
}

public class DeviceAuthorizationResponse
{
    [JsonProperty("user_code")]
    public string UserCode { get; set; }

    [JsonProperty("device_code")]
    public string DeviceCode { get; set; }

    [JsonProperty("verification_uri")]
    public string VerificationUrl { get; set; }

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonProperty("interval")]
    public int Interval { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}

public class DeviceFlowAuthResult
{
    public bool Success { get; set; }

    public OAuth20TokenResponseModel OAuth20TokenResponse { get; set; }
}

public enum MicrosoftAuthenticationExceptionType
{
    Unknown = 0,
    NetworkConnectionError = 1,
    XboxLiveError = 3,
    GameOwnershipError = 4,
}

public enum MicrosoftAuthenticationStep
{
    Get_Authorization_Token = 1,
    Authenticate_with_XboxLive = 2,
    Obtain_XSTS_token_for_Minecraft = 3,
    Authenticate_with_Minecraft = 4,
    Checking_Game_Ownership = 5,
    Get_the_profile = 6
}