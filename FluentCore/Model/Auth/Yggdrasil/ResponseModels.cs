using FluentCore.Model.Game;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Model.Auth.Yggdrasil
{
    public class ResponseModel { }

    public class StandardResponseModel : ResponseModel
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

    public class ErrorResponseModel : ResponseModel
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

    public class ProfileModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
