using Natsurainko.FluentCore.Interface;
using Natsurainko.Toolkits.Network.Downloader;
using System;

namespace Natsurainko.FluentCore.Model.Install;

public class InstallerResponse
{
    public bool Success { get; internal set; }

    public IGameCore GameCore { get; internal set; }

    public Exception Exception { get; internal set; }

    public ParallelDownloaderResponse DownloaderResponse { get; internal set; }
}
