using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.Downloader;

public interface IDownloader
{
    Task<DownloadResult> DownloadFileAsync(DownloadRequest request, CancellationToken cancellationToken = default);

    Task<GroupDownloadResult> DownloadFilesAsync(GroupDownloadRequest request, CancellationToken cancellationToken = default);
}
