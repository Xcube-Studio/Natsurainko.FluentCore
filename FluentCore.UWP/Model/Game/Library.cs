using FluentCore.UWP.Interface;
using FluentCore.UWP.Service.Local;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Model.Game
{
    public class Library : IDependence
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

        [JsonProperty("rules")] 
        public IEnumerable<RuleModel> Rules { get; set; }

        // 为旧式Forge Library提供
        [JsonProperty("checksums")] 
        public List<string> CheckSums { get; set; }

        // 为旧式Forge Library提供
        [JsonProperty("serverreq")] 
        public bool? ServerReq { get; set; }

        // 为旧式Forge Library提供
        [JsonProperty("clientreq")] 
        public bool? ClientReq { get; set; }

        public virtual HttpDownloadRequest GetDownloadRequest(string root)
        {
            return new HttpDownloadRequest
            {
                Sha1 = this.Downloads?.Artifact.Sha1,
                Size = this.Downloads?.Artifact.Size,
                Url = $"{SystemConfiguration.Api.Libraries}/{this.GetRelativePath().Replace("\\", "/")}",
                Directory = new FileInfo($"{PathHelper.GetLibrariesFolder(root)}\\{this.GetRelativePath()}").Directory
            };
        }

        public virtual string GetRelativePath() 
        {
            string[] temp = Name.Split(':');
            return $"{temp[0].Replace(".", "\\")}\\{temp[1]}\\{temp[2]}\\{temp[1]}-{temp[2]}.jar";
        }
    }
}
