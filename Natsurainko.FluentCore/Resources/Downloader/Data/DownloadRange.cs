namespace Nrk.FluentCore.Resources;

/// <summary>
/// 表示一个分片文件的下载范围
/// </summary>
public class DownloadRange
{
    public required long Start { get; set; }

    public required long End { get; set; }

    public required string TempFileAbsolutePath { get; set; }
}
