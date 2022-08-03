using Natsurainko.FluentCore.Class.Model.Auth;
using Natsurainko.FluentCore.Class.Model.Launch;
using Natsurainko.FluentCore.Event;
using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Module.Downloader;
using Natsurainko.FluentCore.Module.Launcher;
using Natsurainko.FluentCore.Service;
using Natsurainko.FluentCore.Wrapper;
using Natsurainko.Toolkits.Values;
using System;
using System.Linq;


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
    Account = new Account
    {
        Name = "Steve",
        AccessToken = Guid.NewGuid().ToString(),
        ClientToken = Guid.NewGuid().ToString(),
        AccountType = AccountType.Offline,
        Uuid = GuidHelper.FromString("Steve")
    },
    GameWindowSetting = new GameWindowSetting
    {
        Width = 854,
        Height = 480,
        IsFullscreen = true,
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
    },
    XmlOutputSetting = new XmlOutputSetting
    {
        Enable = false
    }
};

var launcher = new MinecraftLauncher(launchSetting, gameLocator);

var resourceDownloader = new ResourceDownloader() { 
    DownloadProgressChangedAction = x => { Console.WriteLine($"当前文件资源补全进度：{x * 100:0.00} %"); } 
};

launcher.ResourceDownloader = resourceDownloader; //设置资源补全模块
using var launchResponse = launcher.LaunchMinecraft(core);

if (launchResponse.State == LaunchState.Succeess)
{
    Console.Clear();
    Console.WriteLine($"启动成功：{core}");

    launchResponse.MinecraftProcessOutput += (object sender, IProcessOutput e) => e.Print();
    launchResponse.MinecraftExited += (object sender, MinecraftExitedArgs e) =>
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