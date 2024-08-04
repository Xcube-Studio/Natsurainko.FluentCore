using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public enum DownloadStatus
{
    Preparing, // -> Downloading
    Downloading, // -> Paused | Completed | Failed | Cancelled
    Paused, // -> Downloading
    Completed,
    Failed,
    Cancelled
}
