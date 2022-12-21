using Natsurainko.FluentCore.Event;
using Natsurainko.FluentCore.Extension.Windows.Model.Launch;
using Natsurainko.FluentCore.Model.Auth;
using Natsurainko.FluentCore.Model.Launch;
using Natsurainko.FluentCore.Module.Authenticator;
using Natsurainko.FluentCore.Module.Downloader;
using Natsurainko.FluentCore.Module.Launcher;
using Natsurainko.FluentCore.Service;
using Natsurainko.FluentCore.Wrapper;
using Natsurainko.Toolkits.Network.Downloader;
using System;
using System.Linq;

namespace Natsurainko.FluentCore.Demo;

public class Program
{
    public static void Main(string[] args)
    {
        #region OptiFine Installer
        // var builds = MinecraftOptiFineInstaller.GetOptiFineBuildsFromMcVersionAsync("1.7.2").GetAwaiter().GetResult();
        //
        // var installer = new MinecraftOptiFineInstaller
        // (
        //     new GameCoreLocator(@"D:\Debug\.minecraft"),
        //     builds.First(),
        //     @"D:\Java\openJDK18\bin\java.exe"
        // );
        //
        // installer.ProgressChanged += (object sender, (string, float) e) =>
        // {
        //     Console.Clear();
        //     Console.SetCursorPosition(0, 0);
        //
        //     Console.WriteLine($"[{e.Item2 * 100:0.00}%]{e.Item1}");
        //     Console.SetCursorPosition(0, 0);
        // };
        //
        // var res = installer.Install();
        // Console.ReadKey();
        // return;
        #endregion

        #region Fabric Installer
        // var builds = MinecraftFabricInstaller.GetFabricBuildsFromMcVersionAsync("1.18.2").GetAwaiter().GetResult();
        //
        // var installer = new MinecraftFabricInstaller
        // (
        //     new GameCoreLocator(@"D:\Debug\.minecraft"),
        //     builds.First(),
        //     customId: "1.18.2-Fabric"
        // );
        //
        // installer.ProgressChanged += (object sender, (string, float) e) =>
        // {
        //     Console.Clear();
        //     Console.SetCursorPosition(0, 0);
        //
        //     Console.WriteLine($"[{e.Item2 * 100:0.00}%]{e.Item1}");
        //     Console.SetCursorPosition(0, 0);
        // };
        //
        // var res = installer.Install();
        // Console.ReadKey();
        // return;
        #endregion

        #region Forge Installer
        // var builds = MinecraftForgeInstaller.GetForgeBuildsFromMcVersionAsync("1.19.1").GetAwaiter().GetResult().ToList();
        // builds.Sort((a, b) => a.Build.CompareTo(b.Build));
        //
        // var installer = new MinecraftForgeInstaller
        // (
        //     new GameCoreLocator(@"D:\Debug\.minecraft"),
        //     builds.Last(),
        //     @"D:\Java\openJDK18\bin\java.exe",
        //     customId: "1.19.1-Forge"
        // );
        //
        // installer.ProgressChanged += (object sender, (string, float) e) =>
        // {
        //     Console.Clear();
        //     Console.SetCursorPosition(0, 0);
        //
        //     Console.WriteLine($"[{e.Item2 * 100:0.00}%]{e.Item1}");
        //     Console.SetCursorPosition(0, 0);
        // };
        //
        // var res = installer.Install();
        // Console.ReadKey();
        //
        // return;
        #endregion

        #region Vanllia Installer
        //DownloadApiManager.Current = DownloadApiManager.Mcbbs;

        //var installer = new MinecraftVanlliaInstaller(new GameCoreLocator(@"D:\Debug\.minecraft"), "1.18.2");
        //installer.ProgressChanged += (object sender, (string, float) e) =>
        //{
        //    Console.WriteLine($"[{e.Item2 * 100:0.00}%]{e.Item1}");
        //};        
        //var res = installer.Install();

        //Console.ReadKey();
        //return;
        #endregion

        #region CurseForge Modpack Finder
        // CurseForgeModpackFinder curseForgeModpackFinder = new CurseForgeModpackFinder("Token");
        // var modpacks = curseForgeModpackFinder.GetFeaturedModpacksAsync().GetAwaiter().GetResult();
        //
        // modpacks.ForEach(x =>
        // {
        //     Console.WriteLine(x.Name);
        //     Console.WriteLine($"[{x.SupportedVersions.First()}{(x.SupportedVersions.First() == x.SupportedVersions.Last() ? string.Empty : $"-{x.SupportedVersions.Last()}")}]|{x.Description}|{x.DownloadCount}|{x.LastUpdateTime}");
        //     Console.WriteLine($"[{string.Join('|', x.SupportedVersions)}]");
        //
        //     foreach (var (key, value) in x.Links)
        //         Console.WriteLine($"{key}:{value}");
        //
        //     foreach (var (key, value) in x.Files)
        //         value.ForEach(y => Console.WriteLine($"[{y.ModLoaderType}][{y.SupportedVersion}][{y.FileName}][{y.DownloadUrl}]"));
        // });
        //
        // curseForgeModpackFinder.GetCategories().GetAwaiter().GetResult().ForEach(x => Console.WriteLine($"{x.Name}|{x.Id}"));
        //
        // Console.ReadKey();
        //
        //return;
        #endregion

        #region UwpMinecraftLauncher

        //UwpMinecraftLauncher.LaunchMinecraft();
        //return;

        #endregion

        DownloadApiManager.Current = DownloadApiManager.Mojang;

        Console.Write("请输入 Java 运行时：");
        string javaPath = Console.ReadLine();

        Console.Write("请输入 minecraft 路径：");
        string gameFolder = Console.ReadLine();

        var gameLocator = new GameCoreLocator(gameFolder);
        Console.WriteLine("已找到以下核心：");
        Console.WriteLine(string.Join(',', gameLocator.GetGameCores().Select(x => x.Id)));

        Console.Write("请输入要启动的核心 Id ：");
        string core = Console.ReadLine();

        //完整的启动配置
        var launchSetting = new LaunchSetting()
        {
            Account = Account.Default,
            GameWindowSetting = new GameWindowSetting
            {
                Width = 854,
                Height = 480,
                IsFullscreen = false
            },
            IsDemoUser = false,
            JvmSetting = new JvmSetting(javaPath)
            {
                MaxMemory = 2048,
                MinMemory = 1024,
                AdvancedArguments = DefaultSettings.DefaultAdvancedArguments,
                GCArguments = DefaultSettings.DefaultGCArguments
            },
            NativesFolder = null,
            ServerSetting = new ServerSetting
            {
                IPAddress = "mc.hypixel.net",
                Port = 25565
            }
        };

        var authenticator = new MicrosoftAuthenticator();
        MinecraftLauncher minecraftLauncher = new MinecraftLauncher(launchSetting, authenticator, gameLocator);

        var launcher = new MinecraftLauncher(launchSetting, gameLocator);

        var resourceDownloader = new ResourceDownloader();
        resourceDownloader.DownloadProgressChanged += (object sender, ParallelDownloaderProgressChangedEventArgs e)
            => Console.WriteLine($"当前文件资源补全进度：{e.Progress * 100:0.00} %");

        launcher.ResourceDownloader = resourceDownloader; //设置资源补全模块
        using var launchResponse = launcher.LaunchMinecraft(core);
        launchResponse.GameProcessOutput += (object sender, GameProcessOutputArgs e) => e.Print();

        if (launchResponse.State == LaunchState.Succeess)
        {
            Console.Clear();
            Console.WriteLine($"启动成功：{core}");

            launchResponse.SetMainWindowTitle("Natsurainko.FluentCore.Demo");
            launchResponse.GameExited += (object sender, GameExitedArgs e) =>
            {
                Console.WriteLine("核心启动参数：");
                Console.WriteLine(string.Join("\r\n", launchResponse.Arguemnts));
            };

            launchResponse.WaitForExit();
        }
        else
        {
            Console.WriteLine($"启动失败：{core}");

            if (launchResponse.Exception != null)
            {
                Console.WriteLine("启动时捕获的异常：");
                Console.WriteLine(launchResponse.Exception);
            }
        }

        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}