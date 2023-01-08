using Natsurainko.FluentCore.Event;
using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Launch;
using Natsurainko.FluentCore.Module.Launcher;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Wrapper;

public class UwpMinecraftLauncher : ILauncher
{
    #region UnSupported
    public LaunchSetting LaunchSetting => throw new NotSupportedException();

    public ArgumentsBuilder ArgumentsBuilder => throw new NotSupportedException();

    public IAuthenticator Authenticator { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public IGameCoreLocator<IGameCore> GameCoreLocator { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public IResourceDownloader ResourceDownloader { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    /// <summary>
    /// 应该使用对应的 <see cref="LaunchMinecraft()"/> 来启动 Minecraft: Bedrock Edition
    /// <para>
    /// 使用这项将引发 <see cref="NotSupportedException"/> 错误
    /// </para>
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
    [Obsolete]
    public LaunchResponse LaunchMinecraft(IGameCore gameCore) => throw new NotSupportedException();

    /// <summary>
    /// 应该使用对应的 <see cref="LaunchMinecraftAsync()"/> 来启动 Minecraft: Bedrock Edition
    /// <para>
    /// 使用这项将引发 <see cref="NotSupportedException"/> 错误
    /// </para>
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
    [Obsolete]
    public Task<LaunchResponse> LaunchMinecraftAsync(IGameCore gameCore) => throw new NotSupportedException();
    #endregion

    public static bool LaunchMinecraft() => LaunchMinecraftAsync().GetAwaiter().GetResult();

    public static Task<bool> LaunchMinecraftAsync() => Task.Run(() =>
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "minecraft:"
        });
    }).ContinueWith(task => !task.IsFaulted);
}
