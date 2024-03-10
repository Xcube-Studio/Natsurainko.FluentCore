using System;
using System.Diagnostics.CodeAnalysis;
using Nrk.FluentCore.Management.Downloader;

namespace Nrk.FluentCore.Management.Downloader.Data;

/// <summary>
/// 表示下载任务结果
/// </summary>
public class DownloadResult
{
    /// <summary>
    /// 是否失败
    /// </summary>
    [MemberNotNullWhen(true, nameof(DownloadElement), nameof(Exception))]
    public required bool IsFaulted { get; set; }

    /// <summary>
    /// 失败的下载元素，若未失败该值为null
    /// </summary>
    public IDownloadElement? DownloadElement { get; set; }

    /// <summary>
    /// 导致失败的异常，若未失败该值为null
    /// </summary>
    public Exception? Exception { get; set; }
}
