namespace Nrk.FluentCore.Management.ModLoaders;

/// <summary>
/// 表示一个模组加载信息
/// </summary>
public record ModLoaderInfo
{
    /// <summary>
    /// 加载器类型
    /// </summary>
    public ModLoaderType LoaderType { get; set; }

    /// <summary>
    /// 加载器版本
    /// </summary>
    public required string Version { get; set; }
}
