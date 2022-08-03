using Natsurainko.FluentCore.Interface;
using Natsurainko.Toolkits.Network.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Natsurainko.FluentCore.Class.Model.Download
{
    public class FileResource : IResource
    {
        [JsonIgnore]
        public DirectoryInfo Root { get; set; }

        public string Name { get; set; }

        public int Size { get; set; }

        public string CheckSum { get; set; }

        public string Url { get; set; }

        [JsonIgnore]
        public FileInfo FileInfo { get; set; }

        public HttpDownloadRequest ToDownloadRequest()
            => new HttpDownloadRequest
            {
                Directory = this.FileInfo.Directory,
                FileName = this.Name,
                Sha1 = this.CheckSum,
                Size = this.Size,
                Url = this.Url
            };

        public FileInfo ToFileInfo() => this.FileInfo;
    }
}
