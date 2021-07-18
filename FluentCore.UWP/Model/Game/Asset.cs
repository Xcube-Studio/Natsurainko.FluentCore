using FluentCore.UWP.Interface;
using FluentCore.UWP.Model.Launch;
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
    public class Asset : IDependence
    {
        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        public HttpDownloadRequest GetDownloadRequest(string root)
        {
            return new HttpDownloadRequest
            {
                Sha1 = this.Hash,
                Size = this.Size,
                Url = $"{SystemConfiguration.Api.Assets}/{this.Hash.Substring(0,2)}/{this.Hash}",
                Directory = new FileInfo($"{PathHelper.GetAssetsFolder(root)}\\{this.GetRelativePath()}").Directory
            };
        }

        public virtual string GetRelativePath() => $"objects\\{this.Hash.Substring(0, 2)}\\{this.Hash}";
    }
}
