using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public interface IDownloadTaskGroup
{
    IReadOnlyList<IDownloadTask> DownloadTasks { get; }
    IReadOnlyList<IDownloadTask> FailedDownloadTasks { get; }
    int TotalTasks { get; }
    int CompletedTasks { get; }

    event EventHandler<IDownloadTask>? DownloadTaskCompleted;

    TaskAwaiter GetAwaiter();
}