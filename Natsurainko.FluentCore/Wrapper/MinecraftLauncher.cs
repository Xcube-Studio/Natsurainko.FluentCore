using Natsurainko.FluentCore.Event;
using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Launch;
using Natsurainko.FluentCore.Module.Launcher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Wrapper;

public class MinecraftLauncher : ILauncher
{
    public LaunchSetting LaunchSetting { get; private set; }

    public ArgumentsBuilder ArgumentsBuilder { get; private set; }

    public IAuthenticator Authenticator { get; set; }

    public IGameCoreLocator<IGameCore> GameCoreLocator { get; set; }

    public IResourceDownloader ResourceDownloader { get; set; }

    public MinecraftLauncher(LaunchSetting launchSetting, IGameCoreLocator<IGameCore> gameCoreLocator)
    {
        LaunchSetting = launchSetting;
        GameCoreLocator = gameCoreLocator;

        if (LaunchSetting.Account == null)
            throw new ArgumentNullException("LaunchSetting.Account");
    }

    public MinecraftLauncher(LaunchSetting launchSetting, IAuthenticator authenticator, IGameCoreLocator<IGameCore> gameCoreLocator)
    {
        LaunchSetting = launchSetting;
        Authenticator = authenticator;
        GameCoreLocator = gameCoreLocator;
    }

    public LaunchResponse LaunchMinecraft(string id)
        => LaunchMinecraftAsync(id).GetAwaiter().GetResult();

    public LaunchResponse LaunchMinecraft(IGameCore core)
        => LaunchMinecraftAsync(core).GetAwaiter().GetResult();

    public LaunchResponse LaunchMinecraft(IGameCore core, Action<LaunchProgressChangedEventArgs> action)
        => LaunchMinecraftAsync(core, action).GetAwaiter().GetResult();

    public LaunchResponse LaunchMinecraft(string id, Action<LaunchProgressChangedEventArgs> action)
        => LaunchMinecraftAsync(id, action).GetAwaiter().GetResult();

    public async Task<LaunchResponse> LaunchMinecraftAsync(string id)
        => await LaunchMinecraftAsync(GameCoreLocator.GetGameCore(id));

    public async Task<LaunchResponse> LaunchMinecraftAsync(IGameCore core)
    {
        IEnumerable<string> args = Array.Empty<string>();
        Process process = null;

        try
        {
            if (core == null)
                throw new Exception("GameCore Not Found!");

            if (ResourceDownloader != null)
            {
                ResourceDownloader.GameCore = core;
                var res = await ResourceDownloader.DownloadAsync();
            }

            if (Authenticator != null)
                LaunchSetting.Account = await Authenticator.AuthenticateAsync();

            ArgumentsBuilder = new ArgumentsBuilder(core, LaunchSetting);
            args = ArgumentsBuilder.Build();

            var natives = new DirectoryInfo(LaunchSetting.NativesFolder != null && LaunchSetting.NativesFolder.Exists
                ? LaunchSetting.NativesFolder.FullName.ToString()
                : Path.Combine(core.Root.FullName, "versions", core.Id, "natives"));

            NativesDecompressor.Decompress(natives, core.LibraryResources);

            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = LaunchSetting.JvmSetting.Javaw.FullName,
                    Arguments = string.Join(' '.ToString(), args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = LaunchSetting.EnableIndependencyCore && (bool)LaunchSetting.WorkingFolder?.Exists
                        ? LaunchSetting.WorkingFolder.FullName
                        : core.Root.FullName
                },
                EnableRaisingEvents = true
            };

            return new LaunchResponse(process, LaunchState.Succeess, args, Stopwatch.StartNew());
        }
        catch (Exception ex)
        {
            return new LaunchResponse(
                process,
                ex.GetType() == typeof(OperationCanceledException)
                    ? LaunchState.Cancelled
                    : LaunchState.Failed,
                args,
                ex);
        }
    }

    public async Task<LaunchResponse> LaunchMinecraftAsync(IGameCore core, Action<LaunchProgressChangedEventArgs> action)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        IProgress<LaunchProgressChangedEventArgs> progress = new Progress<LaunchProgressChangedEventArgs>();
        ((Progress<LaunchProgressChangedEventArgs>)progress).ProgressChanged += MinecraftLauncher_ProgressChanged;

        void MinecraftLauncher_ProgressChanged(object _, LaunchProgressChangedEventArgs e)
        {
            action(e);

            if (e.CancellationToken.IsCancellationRequested)
                e.CancellationToken.ThrowIfCancellationRequested();
        }

        IEnumerable<string> args = Array.Empty<string>();
        Process process = null;

        try
        {
            progress.Report(LaunchProgressChangedEventArgs.Create(0.2f, "正在查找游戏核心", cancellationTokenSource.Token));

            if (core == null)
                throw new Exception("GameCore Not Found!");

            if (ResourceDownloader != null)
            {
                ResourceDownloader.GameCore = core;
                progress.Report(LaunchProgressChangedEventArgs.Create(0.4f, "正在补全游戏文件", cancellationTokenSource.Token));
                var res = await ResourceDownloader.DownloadAsync();
            }

            progress.Report(LaunchProgressChangedEventArgs.Create(0.6f, "正在验证账户信息", cancellationTokenSource.Token));
            if (Authenticator != null)
                LaunchSetting.Account = await Authenticator.AuthenticateAsync();

            progress.Report(LaunchProgressChangedEventArgs.Create(0.8f, "正在构建启动参数", cancellationTokenSource.Token));
            ArgumentsBuilder = new ArgumentsBuilder(core, LaunchSetting);
            args = ArgumentsBuilder.Build();

            var natives = new DirectoryInfo(LaunchSetting.NativesFolder != null && LaunchSetting.NativesFolder.Exists
                ? LaunchSetting.NativesFolder.FullName.ToString()
                : Path.Combine(core.Root.FullName, "versions", core.Id, "natives"));

            NativesDecompressor.Decompress(natives, core.LibraryResources);

            progress.Report(LaunchProgressChangedEventArgs.Create(1.0f, "正在启动游戏", cancellationTokenSource.Token));

            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = LaunchSetting.JvmSetting.Javaw.FullName,
                    Arguments = string.Join(' '.ToString(), args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = LaunchSetting.EnableIndependencyCore && (bool)LaunchSetting.WorkingFolder?.Exists
                        ? LaunchSetting.WorkingFolder.FullName
                        : core.Root.FullName
                },
                EnableRaisingEvents = true
            };

            ((Progress<LaunchProgressChangedEventArgs>)progress).ProgressChanged -= MinecraftLauncher_ProgressChanged;
            return new LaunchResponse(process, LaunchState.Succeess, args, Stopwatch.StartNew());
        }
        catch (Exception ex)
        {
            ((Progress<LaunchProgressChangedEventArgs>)progress).ProgressChanged -= MinecraftLauncher_ProgressChanged;

            return new LaunchResponse(
                process,
                ex.GetType() == typeof(OperationCanceledException)
                    ? LaunchState.Cancelled
                    : LaunchState.Failed,
                args,
                ex);
        }
    }

    public async Task<LaunchResponse> LaunchMinecraftAsync(string id, Action<LaunchProgressChangedEventArgs> action)
        => await LaunchMinecraftAsync(GameCoreLocator.GetGameCore(id), action);
}
