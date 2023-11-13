namespace Nrk.FluentCore.Resources;

/// <summary>
/// 表示一个下载元素
/// </summary>
public class DownloadElement : IDownloadElement
{
    /// <summary>
    /// 绝对路径
    /// </summary>
    public required string AbsolutePath { get; set; }

    /// <summary>
    /// 下载地址
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// 校验码
    /// </summary>
    public string? Checksum { get; set; }
}
