using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using System;
using System.Collections.Generic;

namespace Nrk.FluentCore.Experimental.Exceptions;

/// <summary>
/// 依赖补全不完整错误
/// </summary>
public class IncompleteDependenciesException : Exception
{
    public IReadOnlyList<(DownloadRequest, DownloadResult)> Failed { get; init; }

    public IncompleteDependenciesException(IReadOnlyList<(DownloadRequest, DownloadResult)> failed, string message) : base(message)
    {
        Failed = failed;
    }
}
