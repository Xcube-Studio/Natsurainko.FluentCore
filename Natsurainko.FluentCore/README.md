# Natsurainko.FluentCore
![](https://img.shields.io/badge/license-MIT-green)
![](https://img.shields.io/github/repo-size/Xcube-Studio/Natsurainko.FluentCore)
![](https://img.shields.io/github/stars/Xcube-Studio/Natsurainko.FluentCore)
![](https://img.shields.io/github/commit-activity/y/Xcube-Studio/Natsurainko.FluentCore)

一个高效的模块化的 Minecraft 启动核心
---------------------------------------------------------

### 简介
一个由C#编写的跨平台模块化 Minecraft 启动核心

+ 支持桌面平台的跨平台调用 (Windows/Linux/Mac上的调试均已通过)
+ Minecraft游戏核心的查找
+ Minecraft的参数生成、启动封装
+ 对离线、微软、外置登录验证的支持
+ 支持多线程高速补全Assets、Libraries等游戏资源
+ 支持自动安装Forge、Fabric、OptiFine加载器
+ 支持对CurseForge的api的封装
+ 支持从[Bmclapi、Mcbbs](https://bmclapidoc.bangbang93.com/)下载源进行文件补全
  + 在此感谢bangbang93提供镜像站服务 如果您支持我们 可以 [赞助Bmclapi](https://afdian.net/@bangbang93)

本项目依赖框架: .NET Standard 2.0 / .NET 6

声明
+ BMCLAPI是@bangbang93开发的BMCL的一部分，用于解决国内线路对Forge和Minecraft官方使用的Amazon S3 速度缓慢的问题。BMCLAPI是对外开放的，所有需要Minecraft资源的启动器均可调用。
+ 感谢开发过程中大佬[laolarou726](https://github.com/laolarou726)给出的建议和指导 不妨也看看它的启动核心项目[Projbobcat](https://github.com/Corona-Studio/ProjBobcat)

> 您发现了我们项目中的bug? 对我们的项目中有不满意的地方? <br/>
> 或是您愿意加入我们，与我们一同开发？ <br/>
> 联系: a-275@qq.com (作者本人邮箱)

### 安装

+ 在Visual Studio的Nuget包管理器中搜索 Natsurainko.FluentCore 并安装
+ 直接下载本仓库Release中的.nupkg文件进行安装
+ 直接下载本仓库Release中的.dll文件导入项目

### 用法
#### 初始化启动核心并启动游戏
引用
``` c#
using Natsurainko.FluentCore.Model.Launch;
using Natsurainko.FluentCore.Module.Authenticator;
using Natsurainko.FluentCore.Module.Launcher;
using Natsurainko.FluentCore.Wrapper;
using System;
```
``` c#
string javaPath = Console.ReadLine();
string gameFolder = Console.ReadLine();
string core = Console.ReadLine();
string userName = Console.ReadLine();

var settings = new LaunchSetting(new JvmSetting(javaPath)); // 初始化启动配置
var authenticator = new OfflineAuthenticator(userName); // 初始化离线账户验证器
var locator = new GameCoreLocator(gameFolder); // 初始化核心定位器

var launcher = new MinecraftLauncher(settings, authenticator, locator); // 初始化启动
using var response = launcher.LaunchMinecraft(core); // 启动游戏

if (response.State == LaunchState.Succeess) // 判断启动状态是否成功
    response.WaitForExit(); // 若成功就等待游戏进程退出

if (response.Exception != null) // 判断启动过程中是否发生异常
    Console.WriteLine(response.Exception); // 输出异常
```

> 详细的启动过程请翻阅 Demo
#### 初始化微软账户验证器 并调用系统默认浏览器登录
引用
``` c#
using Natsurainko.FluentCore.Extension.Windows.Module.Authenticator;
using Natsurainko.FluentCore.Module.Authenticator;
```
``` c#
var microsoftAuthenticator = new MicrosoftAuthenticator(); // 初始化一个微软账户验证器
// 如果你拥有 Azure 创建的应用，你可以使用 new MicrosoftAuthenticator(string clientId, string redirectUri) 来替代官方的api

await microsoftAuthenticator.GetAccessCode(); // 调用系统默认浏览器取回验证令牌 需要 Natsurainko.FluentCore 的 Windows 扩展
var account = await microsoftAuthenticator.AuthenticateAsync(); // 验证账户

// 将验证得到的账户 添加到启动 方法 1
// var settings = new LaunchSetting(new JvmSetting(javaPath));
// settings.Account = account;
// var launcher = new MinecraftLauncher(settings, locator);

// 将验证得到的账户 添加到启动 方法 2
// 采用方法二则不需要在 await microsoftAuthenticator.GetAccessCode() 之后再添加 var account = await microsoftAuthenticator.AuthenticateAsync()
// var launcher = new MinecraftLauncher(settings, microsoftAuthenticator, locator);
```

#### 初始化 Yggdrasil 账户验证器 并采用外置登录
引用
``` c#
using Natsurainko.FluentCore.Model.Auth;
using Natsurainko.FluentCore.Model.Auth.Yggdrasil;
using Natsurainko.FluentCore.Service;
using Natsurainko.Toolkits.Text;
using System.Collections.Generic;
using System.Linq;
using System;
```
``` c#
string email = Console.ReadLine(); // Yggdrasil 账户邮箱
string password = Console.ReadLine(); // Yggdrasil 账户密码
string yggdrasilServerUrl = Console.ReadLine(); // 外置登录 api 服务地址
string authlibPath = Console.ReadLine(); // authlib-injector-1.1.xx.jar 文件路径

var authenticator = new YggdrasilAuthenticator(
    YggdrasilAuthenticatorMethod.Login,
    email: email,
    password: password,
    yggdrasilServerUrl: $"{yggdrasilServerUrl}/authserver");

// 获取外置登录 api 服务密匙
string base64 = (await (await HttpWrapper.HttpGetAsync(yggdrasilServerUrl)).Content.ReadAsStringAsync()).ConvertToBase64(); // 需要 Natsurainko.Toolkits

var args = DefaultSettings.DefaultAdvancedArguments.ToList();
args.Add($"-javaagent:{authlibPath.ToPath()}={yggdrasilServerUrl}");
args.Add($"-Dauthlibinjector.yggdrasil.prefetched={base64}");

// launchSetting.JvmSetting.AdvancedArguments = args; 设置高级启动参数
```
