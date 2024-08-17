using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public class DownloadTask
{
    public DownloadRequest Request { get; init; }
    public long? TotalBytes { get; private set; } = null;
    public long DownloadedBytes { get => _downloadedBytes; }

    private readonly IDownloader _downloader;

    private long _downloadedBytes = 0;

    public event Action<long?>? FileSizeReceived;
    public event Action<long>? BytesDownloaded;

    public DownloadTask(IDownloader downloader, DownloadRequest request)
    {
        Request = request;
        _downloader = downloader;

        request.FileSizeReceived += (size) =>
        {
            TotalBytes = size;
            FileSizeReceived?.Invoke(size);
        };
        request.BytesDownloaded += (bytes) =>
        {
            Interlocked.Add(ref _downloadedBytes, bytes);
            BytesDownloaded?.Invoke(bytes);
        };
    }

    public Task<DownloadResult> StartAsync(CancellationToken cancellationToken = default)
        => _downloader.DownloadFileAsync(Request, cancellationToken);
}

public class GroupDownloadTask
{
    public GroupDownloadRequest Request { get; init; }
    public int TotalFiles { get; init; }
    public int CompletedFiles { get; private set; }

    private readonly IDownloader _downloader;

    private int _completedFiles = 0;

    public event Action<DownloadRequest, DownloadResult>? SingleRequestCompleted;

    public GroupDownloadTask(IDownloader downloader, GroupDownloadRequest request)
    {
        Request = request;
        _downloader = downloader;
        TotalFiles = Request.Files.Count();

        request.SingleRequestCompleted += (request, result) =>
        {
            Interlocked.Add(ref _completedFiles, 1);
            SingleRequestCompleted?.Invoke(request, result);
        };
    }

    public Task<GroupDownloadResult> StartAsync(CancellationToken cancellationToken = default)
        => _downloader.DownloadFilesAsync(Request, cancellationToken);
}

public static class IDownloaderExtensions
{
    public static DownloadTask CreateDownloadTask(this IDownloader downloader, string url, string localPath)
    {
        return new(downloader, new(url, localPath));
    }

    public static GroupDownloadTask CreateGroupDownloadTask(this IDownloader downloader, IEnumerable<DownloadRequest> requests)
    {
        return new(downloader, new(requests));
    }
}