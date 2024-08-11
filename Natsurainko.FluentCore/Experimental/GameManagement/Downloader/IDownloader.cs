using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public interface IDownloader
{
    Task<DownloadResult> DownloadFileAsync(DownloadRequest request, CancellationToken cancellationToken = default);

    Task<GroupDownloadResult> DownloadFilesAsync(GroupDownloadRequest request, CancellationToken cancellationToken = default);
}
