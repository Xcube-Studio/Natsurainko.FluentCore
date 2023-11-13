using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Authentication.Yggdrasil;

public class YggdrasilResponseModel
{
    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("clientToken")]
    public string? ClientToken { get; set; }

    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("availableProfiles")]
    public IEnumerable<ProfileModel>? AvailableProfiles { get; set; }

    [JsonPropertyName("selectedProfile")]
    public ProfileModel? SelectedProfile { get; set; }
}

public class ErrorResponseModel
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("cause")]
    public string? Cause { get; set; }
}

public class User
{
    [JsonPropertyName("properties")]
    public IEnumerable<PropertyModel>? Properties { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class PropertyModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("profileId")]
    public string? ProfileId { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class ProfileModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
