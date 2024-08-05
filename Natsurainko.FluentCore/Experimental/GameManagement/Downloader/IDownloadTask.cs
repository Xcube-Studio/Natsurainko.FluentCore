using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public interface IDownloadTask
{
    string Url { get; init; }
    string LocalPath { get; init; }

    long DownloadedBytes { get; }
    Exception? Exception { get; }
    DownloadStatus Status { get; }
    Task Task { get; }
    long? TotalBytes { get; }

    public event EventHandler<long?>? FileSizeReceived;
    public event EventHandler<int>? BytesDownloaded;

    TaskAwaiter GetAwaiter();
}
