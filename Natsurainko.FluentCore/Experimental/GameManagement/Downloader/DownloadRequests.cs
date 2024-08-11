using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public interface IDownloadRequest
{
    string Url { get; }
    string LocalPath { get; }

    void OnFileSizeReceived(long? fileSize);
    void OnBytesDownloaded(long bytes);
}

public interface IGroupDownloadRequest
{
    IEnumerable<DownloadRequest> Files { get; }

    void OnSingleRequestCompleted(DownloadRequest request, DownloadResult result);
}

public class DownloadRequest : IDownloadRequest
{
    public string Url { get; init; }
    public string LocalPath { get; init; }

    public event EventHandler<long?>? FileSizeReceived;
    public event EventHandler<long>? BytesReceived;

    void IDownloadRequest.OnFileSizeReceived(long? fileSize)
        => FileSizeReceived?.Invoke(this, fileSize);

    void IDownloadRequest.OnBytesDownloaded(long bytes)
        => BytesReceived?.Invoke(this, bytes);

    public DownloadRequest(string url, string localPath)
    {
        Url = url;
        LocalPath = localPath;
    }
}


public delegate void DownloadRequestCompletedEventHandler(DownloadRequest request, DownloadResult result);

public class GroupDownloadRequest : IGroupDownloadRequest
{
    public IEnumerable<DownloadRequest> Files { get; init; }

    public event DownloadRequestCompletedEventHandler? SingleRequestCompleted;

    void IGroupDownloadRequest.OnSingleRequestCompleted(DownloadRequest request, DownloadResult result)
        => SingleRequestCompleted?.Invoke(request, result);

    public GroupDownloadRequest(IEnumerable<DownloadRequest> files)
    {
        Files = files;
    }
}