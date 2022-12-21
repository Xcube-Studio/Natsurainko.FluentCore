using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Service;
using Natsurainko.Toolkits.Network;
using Natsurainko.Toolkits.Network.Downloader;
using System.IO;

namespace Natsurainko.FluentCore.Model.Download;

public class AssetResource : IResource
{
    public DirectoryInfo Root { get; set; }

    public string Name { get; set; }

    public int Size { get; set; }

    public string CheckSum { get; set; }

    public DownloadRequest ToDownloadRequest() => new()
    {
        Directory = ToFileInfo().Directory,
        FileName = CheckSum,
        FileSize = Size,
        Url = UrlExtension.Combine(
            DownloadApiManager.Current.Assets,
            CheckSum.Substring(0, 2),
            CheckSum)
    };

    public FileInfo ToFileInfo()
        => new(Path.Combine(Root.FullName, "assets", "objects", CheckSum.Substring(0, 2), CheckSum));
}
