using Nrk.FluentCore.Experimental.GameManagement.Instances;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer;

/// <summary>
/// 实例安装器接口
/// </summary>
/// <typeparam name="TInstance"></typeparam>
public interface IInstanceInstaller<TInstance> where TInstance : MinecraftInstance
{
    /// <summary>
    /// 安装目标 .minecraft 目录  
    /// </summary>
    public string MinecraftFolder { get; set; }

    /// <summary>
    /// 异步安装
    /// </summary>
    /// <returns></returns>
    public Task<TInstance> InstallAsync();

    /// <summary>
    /// 异步安装（支持取消）
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<TInstance> InstallAsync(CancellationToken cancellationToken);
}
