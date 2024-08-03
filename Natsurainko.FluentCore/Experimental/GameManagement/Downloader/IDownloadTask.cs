using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public interface IDownloadTask
{
    long DownloadedBytes { get; }
    Exception? Exception { get; }
    string LocalPath { get; init; }
    DownloadStatus Status { get; }
    Task Task { get; init; }
    long? TotalBytes { get; }
    string Url { get; init; }

    TaskAwaiter GetAwaiter();
}