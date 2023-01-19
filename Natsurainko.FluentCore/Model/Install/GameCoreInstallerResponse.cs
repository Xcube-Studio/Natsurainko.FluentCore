using Natsurainko.FluentCore.Interface;
using Natsurainko.Toolkits.Network.Downloader;
using System;
using System.Diagnostics;

namespace Natsurainko.FluentCore.Model.Install;

public class GameCoreInstallerResponse
{
    public bool Success { get; set; }

    public IGameCore GameCore { get; set; }

    public Exception Exception { get; set; }

    public Stopwatch Stopwatch { get; set; }

    public ParallelDownloaderResponse DownloaderResponse { get; set; }
}
