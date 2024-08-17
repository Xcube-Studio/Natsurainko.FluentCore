using Nrk.FluentCore.Management.Downloader.Data;
using System;
using System.Collections.Generic;

namespace Nrk.FluentCore.Launch.Exceptions;

public class IncompleteGameResourcesException : Exception
{
    public IEnumerable<DownloadResult> ErrorDownloadResults { get; set; }

    public IncompleteGameResourcesException(IEnumerable<DownloadResult> errorDownloadResults)
    {
        ErrorDownloadResults = errorDownloadResults;
    }
}
