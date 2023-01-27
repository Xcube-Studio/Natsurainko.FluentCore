using Natsurainko.Toolkits.Network.Downloader;
using System;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Interface;

public interface IResourceDownloader
{
    event EventHandler<ParallelDownloaderProgressChangedEventArgs> DownloadProgressChanged;

    IGameCore GameCore { get; set; }

    ParallelDownloaderResponse Download();

    Task<ParallelDownloaderResponse> DownloadAsync();
}
