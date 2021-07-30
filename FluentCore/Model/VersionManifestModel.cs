using Newtonsoft.Json;
using System.Collections.Generic;

namespace FluentCore.Model
{
    public class VersionManifestModel
    {
        [JsonProperty("latest")]
        public Dictionary<string, string> Latest { get; set; }

        [JsonProperty("versions")]
        public IEnumerable<VersionManifestItem> Versions { get; set; }
    }
}
