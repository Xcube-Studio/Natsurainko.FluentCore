using FluentCore.UWP.Model.Game;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Model
{
    public class LauncherProfilesModel
    {
        [JsonProperty("AuthenticationDatabase")]
        public Dictionary<string,AuthenticationDataModel> AuthenticationDataBase { get; set; }

        [JsonProperty("ClientToken")]
        public Guid clientToken { get; set; }

        [JsonProperty("launcherVersion")]
        public LauncherVersionModel LauncherVersion { get; set; }

        [JsonProperty("profiles")]
        public Dictionary<string,JToken> Profiles { get; set; }

        [JsonProperty("selectedUser")]
        public SelectedUserModel SelectedUser { get; set; }

        [JsonProperty("settings")]
        public Dictionary<string, JToken> Settings { get; set; }

        [JsonProperty("selectedProfile")]
        public string SelectedProfile { get; set; }
    }

}
