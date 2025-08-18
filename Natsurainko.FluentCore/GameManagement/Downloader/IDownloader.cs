using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.Downloader;

public interface IDownloader
{
    HttpClient HttpClient { get; }

    IDownloadMirror? DownloadMirror { get; }

    Task<DownloadResult> DownloadFileAsync(DownloadRequest request, CancellationToken cancellationToken = default);

    Task<GroupDownloadResult> DownloadFilesAsync(GroupDownloadRequest request, CancellationToken cancellationToken = default);
}
