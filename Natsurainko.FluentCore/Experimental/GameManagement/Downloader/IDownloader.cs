using System.Threading;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public interface IDownloader
{
    IDownloadTask DownloadFileAsync(string url, string localPath, CancellationToken cancellationToken);
}