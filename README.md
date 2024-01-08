# FluentCore 📜
![](https://img.shields.io/badge/license-MIT-green)
![](https://img.shields.io/github/repo-size/Xcube-Studio/Natsurainko.FluentCore)
![](https://img.shields.io/github/stars/Xcube-Studio/Natsurainko.FluentCore)
![](https://img.shields.io/github/commit-activity/y/Xcube-Studio/Natsurainko.FluentCore)

基于 .NET 8 的跨平台的模块化 Minecraft 启动核心  
提供简单的模块化调用，以及更面向 Mvvm 模式的服务调用  
**现在正用于 [Fluent Launcher](https://github.com/Xcube-Studio/Natsurainko.FluentLauncher) 的开发中**

>
> 需要注意的是: 目前的 v3 版本与先前的旧版本完全无法兼容，如果已经使用了旧的 v2 版本，请不要更新  
> 且新的 v3 版本暂时没用对启动过程的简单封装，必须创建 ServiceProvider 进行调用，这一问题在后续会改进
> 也因此新的 v3 版本还没有更新过 nuget包 源
>

## 未来路线计划 📝

| 功能                                     | 状态               |
| ---------------------------------------- | ------------------ |
| Native AOT 支持 (需要讨论?)              | [ ]                |
| 完整的 Nullable 支持                     | [ ]                |
| 完整的启动过程封装                       | [ ]                |
| Async 异步过程处理                       | [ ]                |

## 功能列表 ✨

+ 基本功能
  + [x] 查找 .minecraft 中的游戏核心
  + [x] 创建、启动、管理 Minecraft 进程 
  + [x] 多线程高速补全游戏资源
  + [x] 查找已安装的 Java 运行时 (仅 Windows 平台支持) 
  + [x] 支持第三方下载镜像源 [Bmclapi、Mcbbs](https://bmclapidoc.bangbang93.com/)
+ 多种验证方案的支持
  + [x] 微软验证
  + [x] Yggdrasil 验证 (外置验证)
  + [x] 离线验证
  + [ ] 统一通行证验证 (`需要讨论?`)
+ 多种加载器安装器的支持
  + [x] Forge 安装器 (NeoForge 暂用)
  + [x] Fabric 安装器
  + [x] OptiFine 安装器
  + [x] Quilt 安装器
  + [ ] LiteLoder (`已过时而未支持`)
+ 第三方资源下载的支持
  + [x] 对 CurseForge Api 的封装
  + [x] 对 Modrinth Api 的封装

## 与我们联系 ☕️

Xcube Studio 开发群(qq): 1138713376  
Natsurainko 的邮箱: a-275@qq.com  

如果有任何项目代码的问题还是建议留 issues，因为目前作者学业压力较大，没法及时处理加群请求之类的

## 引用及鸣谢 🎉

#### 引用
+ 本篇 readme 模板引用自 [readme-template](https://github.com/iuricode/readme-template)  

#### 鸣谢
+ 首先感谢各位贡献者的共同努力  
+ 感谢bangbang93提供镜像站服务 如果支持他们的服务话 可以[赞助Bmclapi](https://afdian.net/@bangbang93)  
+ 也感谢开发过程中大佬[laolarou726](https://github.com/laolarou726)给出的建议和指导 不妨也看看它的启动核心项目[Projbobcat](https://github.com/Corona-Studio/ProjBobcat)

## 使用示例 

后面等完善了再贴出来罢
