﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Authentication.Microsoft;

public class DisplayClaims
{
    [JsonPropertyName("xui")]
    public JsonArray? Xui { get; set; }
}

public class OAuth20TokenResponse
{
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; set; }

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
    public required string Token { get; set; }

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
    public List<SkinModel>? Skins { get; set; }

    [JsonPropertyName("capes")]
    public JsonArray? Capes { get; set; }
}

public class SkinModel
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

public class DeviceCodeResponse
{
    [JsonPropertyName("user_code")]
    public string? UserCode { get; set; }

    [JsonPropertyName("device_code")]
    public required string DeviceCode { get; set; }

    [JsonPropertyName("verification_uri")]
    public string? VerificationUrl { get; set; }

    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; set; } = -1;

    [JsonPropertyName("interval")]
    public required int Interval { get; set; } = -1;

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class DeviceFlowResponse
{
    public bool Success { get; set; }

    public OAuth20TokenResponse? OAuth20TokenResponse { get; set; }
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
    public OAuth20TokenResponse? OAuth20TokenResponse { get; init; }

    public DeviceFlowPollResult(bool? success, OAuth20TokenResponse? oauth2TokenResponse)
    {
        Success = success;
        OAuth20TokenResponse = oauth2TokenResponse;
    }
}
