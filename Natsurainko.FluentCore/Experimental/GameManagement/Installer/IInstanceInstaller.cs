using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Experimental.GameManagement.Instances;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer;

/// <summary>
/// 实例安装器接口
/// </summary>
public interface IInstanceInstaller
{
    /// <summary>
    /// 安装目标 .minecraft 目录  
    /// </summary>
    public string MinecraftFolder { get; init; }

    /// <summary>
    /// 镜像源
    /// </summary>
    public IDownloadMirror? DownloadMirror { get; init; }

    /// <summary>
    /// 强制检查所有依赖必须被下载
    /// </summary>
    public bool CheckAllDependencies { get; init; }

    /// <summary>
    /// 异步安装（支持取消）
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<MinecraftInstance> InstallAsync(CancellationToken cancellationToken = default);
}
