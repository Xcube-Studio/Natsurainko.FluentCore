using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.Downloader;

public class DownloadRequest
{
    public string Url { get; set; }
    public string LocalPath { get; set; }

    public Action<long?>? FileSizeReceived { get; set; }
    public Action<long>? BytesDownloaded { get; set; }

    public DownloadRequest(string url, string localPath)
    {
        Url = url;
        LocalPath = localPath;
    }
}

public class GroupDownloadRequest
{
    public IEnumerable<DownloadRequest> Files { get; set; }
    public Action<DownloadRequest, DownloadResult>? SingleRequestCompleted { get; set; }

    public GroupDownloadRequest(IEnumerable<DownloadRequest> files)
    {
        Files = files;
    }
}