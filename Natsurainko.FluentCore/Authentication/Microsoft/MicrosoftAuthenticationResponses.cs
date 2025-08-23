using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Authentication;

public class DisplayClaims
{
    [JsonPropertyName("xui")]
    public JsonArray? Xui { get; set; }
}

public class OAuth2TokenResponse
{
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("access_token")]
    [JsonRequired]
    public string AccessToken { get; set; } = null!;

    [JsonPropertyName("refresh_token")]
    [JsonRequired]
    public string RefreshToken { get; set; } = null!;

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("foci")]
    public string? Foci { get; set; }
}

public class XBLAuthenticateResponse
{
    [JsonPropertyName("IssueInstant")]
    public string? IssueInstant { get; set; }

    [JsonPropertyName("NotAfter")]
    public string? NotAfter { get; set; }

    [JsonPropertyName("Token")]
    [JsonRequired]
    public string Token { get; set; } = null!;

    [JsonPropertyName("DisplayClaims")]
    public DisplayClaims? DisplayClaims { get; set; }
}

public class XSTSAuthenticateResponse
{
    [JsonPropertyName("IssueInstant")]
    public string? IssueInstant { get; set; }

    [JsonPropertyName("NotAfter")]
    public string? NotAfter { get; set; }

    [JsonPropertyName("Token")]
    public string? Token { get; set; }

    [JsonPropertyName("DisplayClaims")]
    public DisplayClaims? DisplayClaims { get; set; }
}

public class XSTSAuthenticateErrorModel
{
    [JsonPropertyName("Identity")]
    public string? Identity { get; set; }

    [JsonPropertyName("XErr")]
    public long? XErr { get; set; }

    [JsonPropertyName("Message")]
    public string? Message { get; set; }

    [JsonPropertyName("Redirect")]
    public string? Redirect { get; set; }
}

public class MicrosoftAuthenticationResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("skins")]
    public SkinModel[]? Skins { get; set; }

    [JsonPropertyName("capes")]
    public CapeModel[]? Capes { get; set; }
}

public record SkinModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("variant")]
    public string? Variant { get; set; }

    [JsonPropertyName("alias")]
    public string? Alias { get; set; }
}

public record CapeModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("state")]
    public required string State { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("alias")]
    public required string Alias { get; set; }
}

public class OAuth2DeviceCodeResponse
{
    [JsonPropertyName("user_code")]
    [JsonRequired]
    public string UserCode { get; set; } = null!;

    [JsonPropertyName("device_code")]
    [JsonRequired]
    public string DeviceCode { get; set; } = null!;

    [JsonPropertyName("verification_uri")]
    public string? VerificationUrl { get; set; }

    [JsonPropertyName("verification_uri_complete")]
    public string? VerificationUriComplete { get; set; }

    [JsonPropertyName("expires_in")]
    [JsonRequired]
    public int ExpiresIn { get; set; } = -1;

    [JsonPropertyName("interval")]
    [JsonRequired]
    public int Interval { get; set; } = -1;

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Describes the result of a device flow poll.
/// </summary>
/// <param name="Success">true if successful, false if failed, otherwise null</param>
/// <param name="OAuth20TokenResponse">OAuth resposne if successful</param>
internal class DeviceFlowPollResult
{
    public bool? Success { get; init; }

    // Not null when Success is true
    public OAuth2TokenResponse? OAuth20TokenResponse { get; init; }

    public DeviceFlowPollResult(bool? success, OAuth2TokenResponse? oauth2TokenResponse)
    {
        Success = success;
        OAuth20TokenResponse = oauth2TokenResponse;
    }
}

public record OAuth2Tokens(string AccessToken, string RefreshToken);
