using Natsurainko.FluentCore.Model.Launch;
using Natsurainko.Toolkits.Network.Downloader;
using System;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Interface;

public interface IResourceDownloader
{
    event EventHandler<ParallelDownloaderProgressChangedEventArgs> DownloadProgressChanged;

    GameCore GameCore { get; set; }

    ParallelDownloaderResponse Download();

    Task<ParallelDownloaderResponse> DownloadAsync();
}
