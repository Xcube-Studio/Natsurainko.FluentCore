using FluentCore.Interface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Model.Game
{
    public class Library
    {
        [JsonProperty("downloads")]
        public Downloads Downloads { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        // 为旧式Forge Library提供
        [JsonProperty("url")] 
        public string Url { get; set; }

        [JsonProperty("natives")] 
        public Dictionary<string, string> Natives { get; set; }

        // 为旧式Forge Library提供
        [JsonProperty("checksums")] 
        public List<string> CheckSums { get; set; }

        // 为旧式Forge Library提供
        [JsonProperty("serverreq")] 
        public bool? ServerReq { get; set; }

        // 为旧式Forge Library提供
        [JsonProperty("clientreq")] 
        public bool? ClientReq { get; set; }

    }
}
