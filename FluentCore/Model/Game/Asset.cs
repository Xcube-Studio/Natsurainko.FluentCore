using FluentCore.Interface;
using FluentCore.Service.Local;
using Newtonsoft.Json;
using System.IO;

namespace FluentCore.Model.Game
{
    /// <summary>
    /// 游戏Asset依赖
    /// </summary>
    public class Asset : IDependence
    {
        /// <summary>
        /// 哈希值
        /// </summary>
        [JsonProperty("hash")]
        public string Hash { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        [JsonProperty("size")]
        public int Size { get; set; }

        public HttpDownloadRequest GetDownloadRequest(string root)
        {
            return new HttpDownloadRequest
            {
                Sha1 = this.Hash,
                Size = this.Size,
                Url = $"{SystemConfiguration.Api.Assets}/{this.Hash.Substring(0, 2)}/{this.Hash}",
                Directory = new FileInfo($"{PathHelper.GetAssetsFolder(root)}{PathHelper.X}{this.GetRelativePath()}").Directory
            };
        }

        public virtual string GetRelativePath() => $"objects{PathHelper.X}{this.Hash.Substring(0, 2)}{PathHelper.X}{this.Hash}";
    }
}
