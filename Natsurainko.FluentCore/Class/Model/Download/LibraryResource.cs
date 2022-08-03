using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Service;
using Natsurainko.Toolkits.Network;
using Natsurainko.Toolkits.Network.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Natsurainko.FluentCore.Class.Model.Download
{
    public class LibraryResource : IResource
    {
        [JsonIgnore]
        public DirectoryInfo Root { get; set; }

        public string Name { get; set; }

        public bool IsEnable { get; set; }

        public bool IsNatives { get; set; }

        public int Size { get; set; }

        public string CheckSum { get; set; }

        public string Url { get; set; }

        public HttpDownloadRequest ToDownloadRequest()
        {
            var root = DownloadApiManager.Current.Libraries;

            foreach (var item in FormatName())
                root = UrlExtension.Combine(root, item);

            if (!string.IsNullOrEmpty(this.Url))
                root = DownloadApiManager.Current == DownloadApiManager.Mojang ? this.Url
                    : this.Url.Replace(DownloadApiManager.ForgeLibrary, DownloadApiManager.Current.Libraries);

            return new HttpDownloadRequest
            {
                Directory = this.ToFileInfo().Directory,
                FileName = this.ToFileInfo().Name,
                Sha1 = this.CheckSum,
                Size = this.Size,
                Url = root
            };
        }

        public FileInfo ToFileInfo()
        {
            var root = Path.Combine(this.Root.FullName, "libraries");

            foreach (var item in FormatName())
                root = Path.Combine(root, item);

            return new FileInfo(root);
        }

        private IEnumerable<string> FormatName()
        {
            var subString = this.Name.Split(':');

            foreach (string item in subString[0].Split('.'))
                yield return item;

            yield return subString[1];
            yield return subString[2];

            if (subString.Length > 3)
                yield return $"{subString[1]}-{subString[2]}-{subString[3]}.jar";
            else yield return $"{subString[1]}-{subString[2]}.jar";
        }
    }
}
