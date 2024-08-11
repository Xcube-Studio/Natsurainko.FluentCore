using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public interface IDownloader
{
    Task<DownloadResult> DownloadFileAsync(
        string url, string localPath,
        Action<long?>? fileSizeReceivedCallback = null,
        Action<long>? bytesDownloadedCallback = null,
        CancellationToken cancellationToken = default);

    Task<GroupDownloadResult> DownloadFilesAsync(
        IEnumerable<(string url, string localPath)> files,
        Action<(string url, string localPath), DownloadResult>? downloadTaskFinishedCallback = null,
        CancellationToken cancellationToken = default);
}