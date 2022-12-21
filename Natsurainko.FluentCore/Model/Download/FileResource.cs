using Natsurainko.FluentCore.Interface;
using Natsurainko.Toolkits.Network.Downloader;
using System.IO;

namespace Natsurainko.FluentCore.Model.Download;

public class FileResource : IResource
{
    public DirectoryInfo Root { get; set; }

    public string Name { get; set; }

    public int Size { get; set; }

    public string CheckSum { get; set; }

    public string Url { get; set; }

    public FileInfo FileInfo { get; set; }

    public DownloadRequest ToDownloadRequest() => new()
    {
        Directory = FileInfo.Directory,
        FileName = Name,
        FileSize = Size,
        Url = Url
    };

    public FileInfo ToFileInfo() => FileInfo;
}
