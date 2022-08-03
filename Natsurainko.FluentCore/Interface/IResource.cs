using Natsurainko.Toolkits.Network.Model;
using Natsurainko.Toolkits.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Natsurainko.FluentCore.Interface
{
    public interface IResource
    {
        DirectoryInfo Root { get; set; }

        string Name { get; }

        int Size { get; }

        string CheckSum { get; }

        FileInfo ToFileInfo();

        HttpDownloadRequest ToDownloadRequest();
    }
}
