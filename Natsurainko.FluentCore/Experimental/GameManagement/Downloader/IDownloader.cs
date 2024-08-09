using System.Collections.Generic;
using System.Threading;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public interface IDownloader
{
    IDownloadTask DownloadFileAsync(string url, string localPath, CancellationToken cancellationToken);

    IDownloadTaskGroup DownloadFilesAsync(IEnumerable<(string url, string localPath)> files, CancellationToken cancellationToken);
}