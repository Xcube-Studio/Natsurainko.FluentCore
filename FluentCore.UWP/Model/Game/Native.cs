using FluentCore.UWP.Service.Local;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Model.Game
{
    public class Native : Library
    {
        public Native(Library library)
        {
            this.CheckSums = library.CheckSums;
            this.ClientReq = library.ClientReq;
            this.Downloads = library.Downloads;
            this.Name = library.Name;
            this.Natives = library.Natives;
            this.Rules = library.Rules;
            this.ServerReq = library.ServerReq;
            this.Url = library.Url;
        }

        public override HttpDownloadRequest GetDownloadRequest(string root)
        {
            var file = this.Downloads.Classifiers[Natives["windows"].Replace("${arch}", SystemConfiguration.Arch)];

            return new HttpDownloadRequest
            {
                Sha1 = file.Sha1,
                Size = file.Size,
                Url = $"{SystemConfiguration.Api.Libraries}/{this.GetRelativePath().Replace("\\", "/")}",
                Directory = new FileInfo($"{PathHelper.GetLibrariesFolder(root)}\\{this.GetRelativePath()}").Directory
            };
        }

        public override string GetRelativePath()
        {
            string[] temp = Name.Split(':');
            return $"{temp[0].Replace(".", "\\")}\\{temp[1]}\\{temp[2]}\\" +
                $"{temp[1]}-{temp[2]}-{Natives["windows"].Replace("${arch}", SystemConfiguration.Arch)}.jar";
        }
    }
}
