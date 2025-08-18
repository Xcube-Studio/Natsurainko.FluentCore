using Nrk.FluentCore.Exceptions;
using Nrk.FluentCore.GameManagement.Dependencies;
using Nrk.FluentCore.GameManagement.Downloader;
using Nrk.FluentCore.GameManagement.Instances;
using Nrk.FluentCore.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.Installer;

/// <summary>
/// 原版 Minecraft 实例安装器
/// </summary>
public class VanillaInstanceInstaller : IInstanceInstaller
{
    public required string MinecraftFolder { get; init; }

    public IDownloader Downloader { get; init; } = HttpUtils.Downloader;

    public bool CheckAllDependencies { get; init; }

    /// <summary>
    /// 原版 Minecraft 版本清单项
    /// </summary>
    public required VersionManifestItem McVersionManifestItem { get; init; }

    public IProgress<InstallerProgress>? Progress { get; init; }

    public bool CleanAfterCancelled { get; init; } = true;

    Task<MinecraftInstance> IInstanceInstaller.InstallAsync(CancellationToken cancellationToken)
        => InstallAsync(cancellationToken).ContinueWith(MinecraftInstance (t) => t.Result);

    public async Task<VanillaMinecraftInstance> InstallAsync(CancellationToken cancellationToken = default)
    {
        FileInfo? versionJsonFile = null;
        FileInfo? assetIndex = null;
        VanillaMinecraftInstance? instance = null;

        var stage = VanillaInstallationStage.DownloadVersionJson;
        try
        {
            versionJsonFile = await DownloadVersionJson(cancellationToken);
            instance = ParseVanillaMinecraftInstance(versionJsonFile, cancellationToken);

            stage = VanillaInstallationStage.DownloadAssetIndexJson;
            assetIndex = await DownloadAssetIndexJson(instance, cancellationToken);

            stage = VanillaInstallationStage.DownloadMinecraftDependencies;
            await DownloadMinecraftDependencies(instance, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 取消后清理产生的部分文件
            if (CleanAfterCancelled)
            {
                assetIndex?.Delete();

                if (instance != null)
                {
                    versionJsonFile!.Directory?.DeleteAllFiles();
                    versionJsonFile!.Directory?.Delete();
                }
            }

            Progress?.Report(new InstallerProgress<VanillaInstallationStage>(stage, InstallerStageProgress.Failed()));
            throw;
        }
        catch
        {
            Progress?.Report(new InstallerProgress<VanillaInstallationStage>(stage, InstallerStageProgress.Failed()));
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
        Progress?.Report(new InstallerProgress<VanillaInstallationStage>(
            VanillaInstallationStage.DownloadVersionJson,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        FileInfo jsonFile = new(Path.Combine(MinecraftFolder, "versions", McVersionManifestItem.Id, $"{McVersionManifestItem.Id}.json"));

        var downloadResult = await Downloader.DownloadFileAsync(new(McVersionManifestItem.Url, jsonFile.FullName), cancellationToken);
        if (downloadResult.Type != DownloadResultType.Successful)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw downloadResult.Exception!;
        }

        Progress?.Report(new InstallerProgress<VanillaInstallationStage>(
            VanillaInstallationStage.DownloadVersionJson,
            InstallerStageProgress.Finished()
        ));

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
        Progress?.Report(new InstallerProgress<VanillaInstallationStage>(
            VanillaInstallationStage.DownloadAssetIndexJson,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        var assetIndex = instance.GetAssetIndex();
        FileInfo jsonFile = new(assetIndex.FullPath);

        var downloadResult = await Downloader.DownloadFileAsync(new(assetIndex.Url, jsonFile.FullName), cancellationToken);
        if (downloadResult.Type != DownloadResultType.Successful)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw downloadResult.Exception!;
        }

        Progress?.Report(new InstallerProgress<VanillaInstallationStage>(
             VanillaInstallationStage.DownloadAssetIndexJson,
             InstallerStageProgress.Finished()
        ));

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
        Progress?.Report(new InstallerProgress<VanillaInstallationStage>(
            VanillaInstallationStage.DownloadMinecraftDependencies,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        var dependencyResolver = new DependencyResolver(instance);

        dependencyResolver.InvalidDependenciesDetermined += (_, e)
            => Progress?.Report(new InstallerProgress<VanillaInstallationStage>(
                VanillaInstallationStage.DownloadMinecraftDependencies,
                InstallerStageProgress.UpdateTotalTasks(e.Count())
            ));
        dependencyResolver.DependencyDownloaded += (_, _)
            => Progress?.Report(new InstallerProgress<VanillaInstallationStage>(
                VanillaInstallationStage.DownloadMinecraftDependencies,
                InstallerStageProgress.IncrementFinishedTasks()
            ));

        var groupDownloadResult = await dependencyResolver.VerifyAndDownloadDependenciesAsync(
            downloader: Downloader,
            cancellationToken: cancellationToken);

        if (CheckAllDependencies && groupDownloadResult.Failed.Count > 0)
            throw new IncompleteDependenciesException(groupDownloadResult.Failed, "Some dependent files encountered errors during download");

        Progress?.Report(new InstallerProgress<VanillaInstallationStage>(
            VanillaInstallationStage.DownloadMinecraftDependencies,
            InstallerStageProgress.Finished()
        ));
    }

    public enum VanillaInstallationStage
    {
        DownloadVersionJson,
        DownloadAssetIndexJson,
        DownloadMinecraftDependencies,
    }
}

