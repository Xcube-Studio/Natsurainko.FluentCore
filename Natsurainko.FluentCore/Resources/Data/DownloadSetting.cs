namespace Nrk.FluentCore.Resources;

/// <summary>
/// 表示一个下载设置
/// </summary>
public record DownloadSetting
{
    /// <summary>
    /// 大文件判断阈值
    /// </summary>
    public long FileSizeThreshold { get; set; } = 0;

    /// <summary>
    /// 是否启用大文件分片下载
    /// </summary>
    public bool EnableLargeFileMultiPartDownload { get; set; } = false;

    /// <summary>
    /// 分片数量
    /// </summary>
    public int MultiPartsCount { get; set; } = 0;

    /// <summary>
    /// 最大并行下载线程数
    /// </summary>
    public int MultiThreadsCount { get; set; } = 0;
}
