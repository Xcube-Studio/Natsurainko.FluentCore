using Natsurainko.Toolkits.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Natsurainko.FluentCore.Class.Model.Parser
{
    public class AssetManifestJsonEntity : IJsonEntity
    {
        [JsonProperty("objects")]
        public Dictionary<string, AssetJsonEntity> Objects { get; set; }
    }

    public class AssetJsonEntity
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
    }
}
