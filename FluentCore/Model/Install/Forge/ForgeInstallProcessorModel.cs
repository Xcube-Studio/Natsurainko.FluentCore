using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Model.Install.Forge
{
    public class ForgeInstallProcessorModel
    {
        [JsonProperty("sides")]
        public List<string> Sides { get; set; }

        [JsonProperty("jar")]
        public string Jar { get; set; }

        [JsonProperty("classpath")]
        public List<string> Classpath { get; set; }

        [JsonProperty("args")]
        public List<string> Args { get; set; }

        [JsonProperty("outputs")]
        public Dictionary<string, string> Outputs { get; set; }
    }
}
