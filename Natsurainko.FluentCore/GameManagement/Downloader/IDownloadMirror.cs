using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.Downloader;

public interface IDownloadMirror
{
    public string GetMirrorUrl(string sourceUrl);
}
