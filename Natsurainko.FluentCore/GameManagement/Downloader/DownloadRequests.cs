using System;
using System.Collections.Generic;

namespace Nrk.FluentCore.GameManagement.Downloader;

public class DownloadRequest(string url, string localPath)
{
    public string Url { get; set; } = url;

    public string LocalPath { get; set; } = localPath;

    public int AttemptCount { get; internal set; } = 0;

    public Action<long?>? FileSizeReceived { get; set; }

    public Action<long>? BytesDownloaded { get; set; }
}

public class GroupDownloadRequest(IEnumerable<DownloadRequest> files)
{
    public IEnumerable<DownloadRequest> Files { get; set; } = files;

    public Action<DownloadRequest, DownloadResult>? SingleRequestCompleted { get; set; }
}