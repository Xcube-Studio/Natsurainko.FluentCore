using Newtonsoft.Json;
using System.Collections.Generic;

namespace Natsurainko.FluentCore.Model.Auth;

public class LoginRequestModel
{
    [JsonProperty("agent")]
    public Agent Agent { get; set; } = new Agent();

    [JsonProperty("username")]
    public string UserName { get; set; }

    [JsonProperty("password")]
    public string Password { get; set; }

    [JsonProperty("requestUser")]
    public bool RequestUser { get; set; } = true;

    [JsonProperty("clientToken")]
    public string ClientToken { get; set; }
}

public class YggdrasilRequestModel
{
    [JsonProperty("accessToken")]
    public string AccessToken { get; set; }

    [JsonProperty("clientToken")]
    public string ClientToken { get; set; }
}

public class Agent
{
    [JsonProperty("name")]
    public string Name { get; set; } = "Minecraft";

    [JsonProperty("version")]
    public int Version { get; set; } = 1;
}

public class YggdrasilResponseModel
{
    [JsonProperty("user")]
    public User User { get; set; }

    [JsonProperty("clientToken")]
    public string ClientToken { get; set; }

    [JsonProperty("accessToken")]
    public string AccessToken { get; set; }

    [JsonProperty("availableProfiles")]
    public IEnumerable<ProfileModel> AvailableProfiles { get; set; }

    [JsonProperty("selectedProfile")]
    public ProfileModel SelectedProfile { get; set; }
}

public class ErrorResponseModel
{
    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("cause")]
    public string Cause { get; set; }
}

public class User
{
    [JsonProperty("properties")]
    public IEnumerable<PropertyModel> Properties { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }
}

public class PropertyModel
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("profileId")]
    public string ProfileId { get; set; }

    [JsonProperty("userId")]
    public string UserId { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }
}

public class ProfileModel
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }
}