using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Authentication.Yggdrasil;

public class YggdrasilLoginRequest
{
    [JsonPropertyName("agent")]
    public Agent Agent { get; set; } = new Agent();

    [JsonPropertyName("username")]
    public required string UserName { get; set; }

    [JsonPropertyName("password")]
    public required string Password { get; set; }

    [JsonPropertyName("requestUser")]
    public bool RequestUser { get; set; } = true;

    [JsonPropertyName("clientToken")]
    public required string ClientToken { get; set; }
}

public class YggdrasilRefreshRequest
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("clientToken")]
    public required string ClientToken { get; set; }

    [JsonPropertyName("requestUser")]
    public bool RequestUser { get; set; } = true;
}

public class Agent
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Minecraft";

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;
}
