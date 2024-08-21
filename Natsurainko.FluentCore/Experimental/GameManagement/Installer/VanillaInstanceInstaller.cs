using Nrk.FluentCore.Experimental.Exceptions;
using Nrk.FluentCore.Experimental.GameManagement.Dependencies;
using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Experimental.GameManagement.Installer.Data;
using Nrk.FluentCore.Experimental.GameManagement.Instances;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Nrk.FluentCore.Utils.IProgressReporter;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer;

/// <summary>
/// 原版 Minecraft 实例安装器
/// </summary>
public partial class VanillaInstanceInstaller : IInstanceInstaller<VanillaMinecraftInstance>, IProgressReporter
{
    private readonly HttpClient httpClient = HttpUtils.HttpClient;

    public required string MinecraftFolder { get; set; }

    /// <summary>
    /// 原版 Minecraft 版本清单项
    /// </summary>
    public required VersionManifestItem McVersionManifestItem { get; set; }

    /// <summary>
    /// 镜像源
    /// </summary>
    public IDownloadMirror? DownloadMirror { get; set; }

    /// <summary>
    /// 强制检查所有依赖必须被下载
    /// </summary>
    public bool CheckAllDependencies { get; set; }

    public Task<VanillaMinecraftInstance> InstallAsync() => InstallAsync(CancellationToken.None);

    public async Task<VanillaMinecraftInstance> InstallAsync(CancellationToken cancellationToken)
    {
        FileInfo? versionJsonFile = null;
        FileInfo? assetIndex = null;
        VanillaMinecraftInstance? instance = null;

        try
        {
            versionJsonFile = await DownloadVersionJson(cancellationToken);
            instance = ParseVanillaMinecraftInstance(versionJsonFile, cancellationToken);
            assetIndex = await DownloadAssetIndexJson(instance, cancellationToken);
            await DownloadMinecraftDependencies(instance, cancellationToken);
        }
        catch (OperationCanceledException) 
        {
            // 取消后清理产生的部分文件

            assetIndex?.Delete();

            if (instance != null)
            {
                versionJsonFile!.Directory?.DeleteAllFiles();
                versionJsonFile!.Directory?.Delete();
            }

            ProgressReporterHelper.ReportWhenExceptionThrow(this);
            throw;
        }
        catch
        {
            ProgressReporterHelper.ReportWhenExceptionThrow(this);
            throw;
        }

        return instance ?? throw new ArgumentNullException(nameof(instance), "Unexpected null reference to variable");
    }

    /// <summary>
    /// 下载 version.json 并写入文件
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    async Task<FileInfo> DownloadVersionJson(CancellationToken cancellationToken)
    {
        Progress.Report(ProgressUpdater.FromRunning("DownloadVersionJson"));

        string requestUrl = McVersionManifestItem.Url;

        if (DownloadMirror != null)
            requestUrl = DownloadMirror.GetMirrorUrl(requestUrl);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        using var responseMessage = await httpClient.SendAsync(requestMessage, cancellationToken);

        responseMessage.EnsureSuccessStatusCode();

        string jsonContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        var jsonFile = new FileInfo(Path.Combine(MinecraftFolder, "versions", McVersionManifestItem.Id, $"{McVersionManifestItem.Id}.json"));

        if (!jsonFile.Directory!.Exists)
            jsonFile.Directory.Create();

        await File.WriteAllTextAsync(jsonFile.FullName, jsonContent, cancellationToken);

        Progress.Report(ProgressUpdater.FromFinished("DownloadVersionJson"));

        return jsonFile;
    } 

    /// <summary>
    /// 解析原版 Minecraft 实例
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    static VanillaMinecraftInstance ParseVanillaMinecraftInstance(FileInfo fileInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var instance = MinecraftInstance.Parse(fileInfo.Directory!) as VanillaMinecraftInstance;

        return instance ?? throw new InvalidOperationException("An incorrect vanilla instance was encountered");
    }

    /// <summary>
    /// 下载 assets.json 并写写入文件
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    async Task<FileInfo> DownloadAssetIndexJson(MinecraftInstance instance, CancellationToken cancellationToken)
    {
        Progress.Report(ProgressUpdater.FromRunning("DownloadAssetIndexJson"));

        cancellationToken.ThrowIfCancellationRequested();

        var assetIndex = instance.GetAssetIndex();
        var jsonFile = new FileInfo(assetIndex.FullPath);

        string requestUrl = assetIndex.Url;

        if (DownloadMirror != null)
            requestUrl = DownloadMirror.GetMirrorUrl(requestUrl);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        using var responseMessage = await httpClient.SendAsync(requestMessage, cancellationToken);

        responseMessage.EnsureSuccessStatusCode();

        string jsonContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        if (!jsonFile.Directory!.Exists)
            jsonFile.Directory.Create();

        await File.WriteAllTextAsync(jsonFile.FullName, jsonContent, cancellationToken);

        Progress.Report(ProgressUpdater.FromFinished("DownloadAssetIndexJson"));

        return jsonFile;
    }

    /// <summary>
    /// 使用 MultipartDownloader 下载 MinecraftDependencies
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    async Task DownloadMinecraftDependencies(MinecraftInstance instance, CancellationToken cancellationToken)
    {
        void DependencyDownloaded(object? sender, (DownloadRequest, DownloadResult) e)
            => Progress.Report(ProgressUpdater.FromIncrementFinishedTasks("DownloadMinecraftDependencies"));

        Progress.Report(ProgressUpdater.FromRunning("DownloadMinecraftDependencies"));

        cancellationToken.ThrowIfCancellationRequested();

        var minecraftClient = instance.GetJarElement() ?? throw new InvalidOperationException("Unable to obtain download data for client.jar");
        (var libraries, var natives) = instance.GetRequiredLibraries();
        var minecraftAssets = instance.GetRequiredAssets();

        var dependencyResolver = new DependencyResolverBuilder()
            .AddDependencies([minecraftClient])
            .AddDependencies(libraries)
            .AddDependencies(natives)
            .AddDependencies(minecraftAssets)
            .Build();

        Progress.Report(ProgressUpdater.FromUpdateTotalTasks("DownloadMinecraftDependencies", dependencyResolver.Dependencies.Count));
        dependencyResolver.DependencyDownloaded += DependencyDownloaded;

        var groupDownloadResult = await dependencyResolver.VerifyAndDownloadDependenciesAsync(cancellationToken: cancellationToken);

        if (CheckAllDependencies && groupDownloadResult.Failed.Count > 0)
            throw new IncompleteDependenciesException(groupDownloadResult.Failed, "Dependency files are incomplete");

        Progress.Report(ProgressUpdater.FromFinished("DownloadMinecraftDependencies"));
    }
}

partial class VanillaInstanceInstaller
{
    private readonly Progress<ProgressUpdater> _progress = new();

    IProgress<ProgressUpdater> Progress => _progress;
    IProgress<ProgressUpdater> IProgressReporter.Progress => _progress;

    public event EventHandler<ProgressUpdater>? ProgressChanged
    {
        add => _progress.ProgressChanged += value;
        remove => _progress.ProgressChanged -= value;
    }

    public Dictionary<string, ProgressData> Progresses { get; init; } = 
        ProgressReporterHelper.CreateProgressesFromStringArray(
        [
            "DownloadVersionJson",
            "DownloadAssetIndexJson",
            "DownloadMinecraftDependencies"
        ]);
}
