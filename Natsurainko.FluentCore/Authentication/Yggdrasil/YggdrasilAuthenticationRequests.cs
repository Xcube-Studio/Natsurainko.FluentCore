using System.Text.Json.Serialization;

namespace Nrk.FluentCore.Authentication;

public class YggdrasilLoginRequest
{
    [JsonPropertyName("agent")]
    public Agent Agent { get; set; } = new Agent();

    [JsonPropertyName("username")]
    [JsonRequired]
    public string UserName { get; set; } = null!;

    [JsonPropertyName("password")]
    [JsonRequired]
    public string Password { get; set; } = null!;

    [JsonPropertyName("requestUser")]
    public bool RequestUser { get; set; } = true;

    [JsonPropertyName("clientToken")]
    [JsonRequired]
    public string ClientToken { get; set; } = null!;
}

public class YggdrasilRefreshRequest
{
    [JsonPropertyName("accessToken")]
    [JsonRequired]
    public string AccessToken { get; set; } = null!;

    [JsonPropertyName("clientToken")]
    [JsonRequired]
    public string ClientToken { get; set; } = null!;

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
