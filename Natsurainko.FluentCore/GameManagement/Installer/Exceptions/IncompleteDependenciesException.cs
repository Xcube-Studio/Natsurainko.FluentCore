using Nrk.FluentCore.GameManagement.Downloader;
using System;
using System.Collections.Generic;

namespace Nrk.FluentCore.GameManagement.Installer;

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
