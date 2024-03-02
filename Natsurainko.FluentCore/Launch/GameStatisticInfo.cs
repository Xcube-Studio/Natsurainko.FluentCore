using Nrk.FluentCore.Management.ModLoaders;
using System.Collections.Generic;

namespace Nrk.FluentCore.Launch;

/// <summary>
/// 游戏统计数据
/// </summary>
public record GameStatisticInfo
{
    /// <summary>
    /// 依赖库文件数
    /// </summary>
    public int LibrariesCount { get; set; }

    /// <summary>
    /// 依赖材质文件数
    /// </summary>
    public int AssetsCount { get; set; }

    /// <summary>
    /// 共计占用磁盘空间大小
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// 加载器
    /// </summary>
    public IEnumerable<ModLoaderInfo>? ModLoaders { get; set; }
}
