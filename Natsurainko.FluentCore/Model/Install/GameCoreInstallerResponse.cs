using Natsurainko.FluentCore.Interface;
using Natsurainko.Toolkits.Network.Downloader;
using System;
using System.Diagnostics;

namespace Natsurainko.FluentCore.Model.Install;

public class GameCoreInstallerResponse
{
    public bool Success { get; internal set; }

    public IGameCore GameCore { get; internal set; }

    public Exception Exception { get; internal set; }

    public Stopwatch Stopwatch { get; internal set; }

    public ParallelDownloaderResponse DownloaderResponse { get; internal set; }
}
