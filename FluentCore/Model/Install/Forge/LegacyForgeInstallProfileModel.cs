using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Model.Install.Forge
{
    public class LegacyForgeInstallProfileModel
    {
        [JsonProperty("install")]
        public LegacyForgeInstallModel Install { get; set; }

        [JsonProperty("versionInfo")]
        public JObject VersionInfo { get; set; }

        [JsonProperty("optionals")]
        public IEnumerable<string> Optionals { get; set; }
    }

    public class LegacyForgeInstallModel
    {
        [JsonProperty("profileName")]
        public string ProfileName { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("filePath")]
        public string FilePath { get; set; }

        [JsonProperty("welcome")]
        public string Welcome { get; set; }

        [JsonProperty("minecraft")]
        public string Minecraft { get; set; }

        [JsonProperty("mirrorList")]
        public string MirrorList { get; set; }

        [JsonProperty("logo")]
        public string Logo { get; set; }

        [JsonProperty("modList")]
        public string ModList { get; set; }
    }
}
