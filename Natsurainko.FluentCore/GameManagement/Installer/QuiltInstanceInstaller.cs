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
using static Nrk.FluentCore.GameManagement.Installer.VanillaInstanceInstaller;

namespace Nrk.FluentCore.GameManagement.Installer;

/// <summary>
/// Quilt 实例安装器
/// </summary>
public partial class QuiltInstanceInstaller : IInstanceInstaller
{
    public required string MinecraftFolder { get; init; }

    public IDownloader Downloader { get; init; } = HttpUtils.Downloader;

    public bool CheckAllDependencies { get; init; }

    /// <summary>
    /// Quilt 安装所需数据
    /// </summary>
    public required QuiltInstallData InstallData { get; init; }

    /// <summary>
    /// 原版 Minecraft 版本清单项
    /// </summary>
    public required VersionManifestItem McVersionManifestItem { get; init; }

    /// <summary>
    /// 继承的原版实例（可选）
    /// </summary>
    public VanillaMinecraftInstance? InheritedInstance { get; init; }

    /// <summary>
    /// 自定义安装实例的 Id
    /// </summary>
    public string? CustomizedInstanceId { get; init; }

    public IProgress<InstallerProgress<QuiltInstallationStage>>? Progress { get; init; }

    public IProgress<InstallerProgress<VanillaInstallationStage>>? VanillaInstallationProgress { get; init; }

    Task<MinecraftInstance> IInstanceInstaller.InstallAsync(CancellationToken cancellationToken)
        => InstallAsync(cancellationToken).ContinueWith(MinecraftInstance (t) => t.Result);

    public async Task<ModifiedMinecraftInstance> InstallAsync(CancellationToken cancellationToken = default)
    {
        VanillaMinecraftInstance? vanillaInstance;
        FileInfo? quiltClientJson = null;
        ModifiedMinecraftInstance? instance = null;

        var stage = QuiltInstallationStage.ParseOrInstallVanillaInstance;
        try
        {
            vanillaInstance = await ParseOrInstallVanillaInstance(cancellationToken);

            stage = QuiltInstallationStage.DownloadQuiltClientJson;
            quiltClientJson = await DownloadQuiltClientJson(vanillaInstance, cancellationToken);

            stage = QuiltInstallationStage.DownloadQuiltLibraries;
            instance = ParseModifiedMinecraftInstance(quiltClientJson, cancellationToken);
            await DownloadQuiltLibraries(instance, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 取消后清理产生的部分文件

            if (instance != null)
            {
                quiltClientJson!.Directory?.DeleteAllFiles();
                quiltClientJson!.Directory?.Delete();
            }

            Progress?.Report(new(stage, InstallerStageProgress.Failed()));
            throw;
        }
        catch
        {
            Progress?.Report(new(stage, InstallerStageProgress.Failed()));
            throw;
        }

        return instance ?? throw new ArgumentNullException(nameof(instance), "Unexpected null reference to variable");
    }

    /// <summary>
    /// 解析继承的原版实例或直接安装新的原版实例
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    async Task<VanillaMinecraftInstance> ParseOrInstallVanillaInstance(CancellationToken cancellationToken)
    {
        Progress?.Report(new(
            QuiltInstallationStage.ParseOrInstallVanillaInstance,
            InstallerStageProgress.Starting()
        ));

        if (InheritedInstance != null)
            return InheritedInstance;

        var vanillaInstanceInstaller = new VanillaInstanceInstaller()
        {
            Downloader = Downloader,
            McVersionManifestItem = McVersionManifestItem,
            MinecraftFolder = MinecraftFolder,
            CheckAllDependencies = true,
            Progress = VanillaInstallationProgress,
            CleanAfterCancelled = false
        };

        var instance = await vanillaInstanceInstaller.InstallAsync(cancellationToken);

        Progress?.Report(new(
            QuiltInstallationStage.ParseOrInstallVanillaInstance,
            InstallerStageProgress.Finished()
        ));

        return instance;
    }

    /// <summary>
    /// 下载 version.json 并写入文件
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    async Task<FileInfo> DownloadQuiltClientJson(VanillaMinecraftInstance instance, CancellationToken cancellationToken)
    {
        Progress?.Report(new(
            QuiltInstallationStage.DownloadQuiltClientJson,
            InstallerStageProgress.Starting()
        ));

        string requestUrl = $"https://meta.quiltmc.org/v3/versions/loader/{instance.InstanceId}/{InstallData.Loader.Version}/profile/json";
        string instanceId = CustomizedInstanceId ?? $"quilt-loader-{InstallData.Loader.Version}-{instance.InstanceId}";
        FileInfo jsonFile = new(Path.Combine(MinecraftFolder, "versions", instanceId, $"{instanceId}.json"));

        var downloadResult = await Downloader.DownloadFileAsync(new(requestUrl, jsonFile.FullName), cancellationToken);
        if (downloadResult.Type != DownloadResultType.Successful)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw downloadResult.Exception!;
        }

        Progress?.Report(new(
            QuiltInstallationStage.DownloadQuiltClientJson,
            InstallerStageProgress.Finished()
        ));

        return jsonFile;
    }

    /// <summary>
    /// 解析 Modified Minecraft 实例
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    static ModifiedMinecraftInstance ParseModifiedMinecraftInstance(FileInfo fileInfo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var instance = MinecraftInstance.Parse(fileInfo.Directory!) as ModifiedMinecraftInstance;

        return instance ?? throw new InvalidOperationException("An incorrect modified instance was encountered");
    }

    /// <summary>
    /// 使用 MultipartDownloader 下载 Quilt Libraries
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    async Task DownloadQuiltLibraries(MinecraftInstance instance, CancellationToken cancellationToken)
    {
        Progress?.Report(new(
            QuiltInstallationStage.DownloadQuiltLibraries,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        var dependencyResolver = new DependencyResolver(instance)
        {
            AllowInheritedDependencies = false,
            AllowVerifyAssets = false,
        };

        dependencyResolver.InvalidDependenciesDetermined += (_, e)
            => Progress?.Report(new(
                QuiltInstallationStage.DownloadQuiltLibraries,
                InstallerStageProgress.UpdateTotalTasks(e.Count())
            ));
        dependencyResolver.DependencyDownloaded += (_, _)
            => Progress?.Report(new(
                QuiltInstallationStage.DownloadQuiltLibraries,
                InstallerStageProgress.IncrementFinishedTasks()
            ));

        var groupDownloadResult = await dependencyResolver.VerifyAndDownloadDependenciesAsync(
            downloader: Downloader,
            cancellationToken: cancellationToken);

        if (CheckAllDependencies && groupDownloadResult.Failed.Count > 0)
            throw new IncompleteDependenciesException(groupDownloadResult.Failed, "Some dependent files encountered errors during download");

        Progress?.Report(new(
            QuiltInstallationStage.DownloadQuiltLibraries,
            InstallerStageProgress.Finished()
        ));
    }

    public enum QuiltInstallationStage
    {
        ParseOrInstallVanillaInstance,
        DownloadQuiltClientJson,
        DownloadQuiltLibraries
    }
}
