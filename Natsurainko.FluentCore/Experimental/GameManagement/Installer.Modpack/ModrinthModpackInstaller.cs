using Nrk.FluentCore.Exceptions;
using Nrk.FluentCore.Experimental.GameManagement.Modpacks;
using Nrk.FluentCore.GameManagement;
using Nrk.FluentCore.GameManagement.Downloader;
using Nrk.FluentCore.GameManagement.Installer;
using Nrk.FluentCore.GameManagement.Instances;
using Nrk.FluentCore.Resources.Modrinth;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Nrk.FluentCore.Experimental.GameManagement.Installer.Modpack;

public class ModrinthModpackInstaller : IInstanceInstaller
{
    public required string MinecraftFolder { get; init; }

    /// <summary>
    /// 整合包文件路径
    /// </summary>
    public required string ModpackFilePath { get; init; }

    /// <summary>
    /// 编译过程调用的 java.exe 路径
    /// </summary>
    public required string JavaPath { get; init; }

    public IDownloader Downloader { get; init; } = HttpUtils.Downloader;

    public bool DeletePackageAfterInstallation { get; init; } = false;

    public bool CheckAllDependencies { get; init; }

    /// <summary>
    /// 自定义安装实例的 Id
    /// </summary>
    public string? CustomizedInstanceId { get; init; }

    public IProgress<IInstallerProgress>? Progress { get; init; }

    public CreateModLoderInstallerProgressReporterDelegate? CreateModLoderInstallerProgressReporter { get; init; }

    public async Task<MinecraftInstance> InstallAsync(CancellationToken cancellationToken = default)
    {
        MinecraftInstance? instance = null;
        ZipArchive? packageArchive = null;

        var stage = ModrinthModpackInstallationStage.ParseModrinthModpack;

        try
        {
            ParseModrinthModpack(ModpackFilePath, cancellationToken, out packageArchive, out var modpackManifest, out var modLoaderInfo);

            stage = ModrinthModpackInstallationStage.SearchInstallData;
            (VersionManifestItem, object?) installData = await SearchInstallData(modpackManifest, modLoaderInfo, cancellationToken);

            stage = modLoaderInfo is null
                ? ModrinthModpackInstallationStage.InstallVanillaMinecraftInstance
                : ModrinthModpackInstallationStage.InstallModifiedMinecraftInstance;
            instance = modLoaderInfo is null
                ? await InstallVanillaMinecraftInstance(installData, modpackManifest, cancellationToken)
                : await InstallModifiedMinecraftInstance(((VersionManifestItem, object))installData!, modpackManifest, (ModLoaderInfo)modLoaderInfo, cancellationToken);

            stage = ModrinthModpackInstallationStage.DownloadModrinthFiles;
            await DownloadModrinthFiles(instance, modpackManifest, cancellationToken);

            stage = ModrinthModpackInstallationStage.CopyOverriddenFiles;
            CopyOverriddenFiles(instance, packageArchive, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 取消后清理产生的部分文件
            instance?.Delete();

            Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(stage, InstallerStageProgress.Failed()));
            throw;
        }
        catch
        {
            Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(stage, InstallerStageProgress.Failed()));
            throw;
        }
        finally
        {
            packageArchive?.Dispose();

            if (DeletePackageAfterInstallation)
            {
                try { File.Delete(ModpackFilePath); }
                catch (Exception) { }
            }
        }

        return instance ?? throw new ArgumentNullException(nameof(instance), "Unexpected null reference to variable");
    }

    void ParseModrinthModpack(
        string packageFilePath,
        CancellationToken cancellationToken,
        out ZipArchive packageArchive,
        out ModrinthModpackManifest modpackManifest,
        out ModLoaderInfo? modLoaderInfo)
    {
        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.ParseModrinthModpack,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        packageArchive = ZipFile.OpenRead(packageFilePath);
        var modpackInfo = ModpackInfoParser.ParseModrinthModpack(packageArchive, out modpackManifest);
        modLoaderInfo = modpackInfo.ModLoader;

        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.ParseModrinthModpack,
            InstallerStageProgress.Finished()
        ));
    }

    async Task<(VersionManifestItem, object?)> SearchInstallData(
        ModrinthModpackManifest modpackManifest,
        ModLoaderInfo? modLoaderInfo,
        CancellationToken cancellationToken)
    {
        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.SearchInstallData,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        var value = await VersionManifestApi.SearchInstallDataAsync
        (
            modpackManifest.Dependencies["minecraft"],
            modLoaderInfo,
            Downloader.HttpClient,
            Downloader.DownloadMirror,
            cancellationToken
        );

        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.SearchInstallData,
            InstallerStageProgress.Finished()
        ));

        return value;
    }

    async Task<MinecraftInstance> InstallVanillaMinecraftInstance(
        (VersionManifestItem, object?) modification,
        ModrinthModpackManifest modpackManifest,
        CancellationToken cancellationToken)
    {
        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.InstallModifiedMinecraftInstance,
            InstallerStageProgress.Skiped()
        ));
        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.InstallVanillaMinecraftInstance,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        IProgress<IInstallerProgress>? progress = (CreateModLoderInstallerProgressReporter ?? EmptyDelegate)
            (ModLoaderType.Fabric, out IProgress<IInstallerProgress>? vanillaInstallationProgress);

        foreach (var item in Enum.GetValues<FabricInstanceInstaller.FabricInstallationStage>())
            progress?.Report(new InstallerProgress<FabricInstanceInstaller.FabricInstallationStage>(item, InstallerStageProgress.Skiped()));

        IInstanceInstaller instanceInstaller = new VanillaInstanceInstaller
        {
            CheckAllDependencies = CheckAllDependencies,
            CustomizedInstanceId = CustomizedInstanceId ?? modpackManifest.Name,
            Downloader = Downloader,
            McVersionManifestItem = modification.Item1,
            MinecraftFolder = MinecraftFolder,
            Progress = vanillaInstallationProgress,
        };

        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.InstallVanillaMinecraftInstance,
            InstallerStageProgress.Finished()
        ));
        return await instanceInstaller.InstallAsync(cancellationToken);
    }

    async Task<MinecraftInstance> InstallModifiedMinecraftInstance(
        (VersionManifestItem, object) modification,
        ModrinthModpackManifest modpackManifest,
        ModLoaderInfo modLoaderInfo,
        CancellationToken cancellationToken)
    {
        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.InstallVanillaMinecraftInstance,
            InstallerStageProgress.Skiped()
        ));
        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.InstallModifiedMinecraftInstance,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        var minecraftInstanceParser = new MinecraftInstanceParser(MinecraftFolder);
        var vanillaMinecraftInstance = minecraftInstanceParser.ParseAllInstances()
            .OfType<VanillaMinecraftInstance>()
            .FirstOrDefault(i => i.Version.VersionId.Equals(modpackManifest.Dependencies["minecraft"]));

        IInstanceInstaller instanceInstaller = modLoaderInfo.Type switch
        {
            ModLoaderType.Forge => new ForgeInstanceInstaller
            {
                CheckAllDependencies = CheckAllDependencies,
                CustomizedInstanceId = modpackManifest.Name,
                Downloader = Downloader,
                InheritedInstance = vanillaMinecraftInstance,
                InstallData = (ForgeInstallData)modification.Item2,
                IsNeoForgeInstaller = false,
                JavaPath = JavaPath,
                McVersionManifestItem = modification.Item1,
                MinecraftFolder = MinecraftFolder,
                Progress = (CreateModLoderInstallerProgressReporter ?? EmptyDelegate)(modLoaderInfo.Type, out IProgress<IInstallerProgress>? VanillaInstallationProgress),
                VanillaInstallationProgress = VanillaInstallationProgress
            },
            ModLoaderType.NeoForge => new ForgeInstanceInstaller
            {
                CheckAllDependencies = CheckAllDependencies,
                CustomizedInstanceId = modpackManifest.Name,
                Downloader = Downloader,
                InheritedInstance = vanillaMinecraftInstance,
                InstallData = (ForgeInstallData)modification.Item2,
                IsNeoForgeInstaller = true,
                JavaPath = JavaPath,
                McVersionManifestItem = modification.Item1,
                MinecraftFolder = MinecraftFolder,
                Progress = (CreateModLoderInstallerProgressReporter ?? EmptyDelegate)(modLoaderInfo.Type, out IProgress<IInstallerProgress>? VanillaInstallationProgress),
                VanillaInstallationProgress = VanillaInstallationProgress
            },
            ModLoaderType.Fabric => new FabricInstanceInstaller
            {
                CheckAllDependencies = CheckAllDependencies,
                CustomizedInstanceId = modpackManifest.Name,
                Downloader = Downloader,
                InheritedInstance = vanillaMinecraftInstance,
                InstallData = (FabricInstallData)modification.Item2,
                McVersionManifestItem = modification.Item1,
                MinecraftFolder = MinecraftFolder,
                Progress = (CreateModLoderInstallerProgressReporter ?? EmptyDelegate)(modLoaderInfo.Type, out IProgress<IInstallerProgress>? VanillaInstallationProgress),
                VanillaInstallationProgress = VanillaInstallationProgress
            },
            ModLoaderType.Quilt => new QuiltInstanceInstaller
            {
                CheckAllDependencies = CheckAllDependencies,
                CustomizedInstanceId = modpackManifest.Name,
                Downloader = Downloader,
                InheritedInstance = vanillaMinecraftInstance,
                InstallData = (QuiltInstallData)modification.Item2,
                McVersionManifestItem = modification.Item1,
                MinecraftFolder = MinecraftFolder,
                Progress = (CreateModLoderInstallerProgressReporter ?? EmptyDelegate)(modLoaderInfo.Type, out IProgress<IInstallerProgress>? VanillaInstallationProgress),
                VanillaInstallationProgress = VanillaInstallationProgress
            },
            _ => throw new NotImplementedException()
        };

        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.InstallModifiedMinecraftInstance,
            InstallerStageProgress.Finished()
        ));

        return await instanceInstaller.InstallAsync(cancellationToken);
    }

    async Task DownloadModrinthFiles(
        MinecraftInstance minecraftInstance,
        ModrinthModpackManifest modpackManifest,
        CancellationToken cancellationToken)
    {
        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.DownloadModrinthFiles,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        List<DownloadRequest> downloadRequests = [];

        foreach (var file in modpackManifest.Files)
        {
            if (file.Environment != null)
            {
                if (!file.Environment.TryGetValue("client", out var environment)) continue;
                if (environment != "required") continue;
            }

            downloadRequests.Add(new
            (
                file.Downloads.First(),
                Path.Combine(
                    Path.GetDirectoryName(minecraftInstance.ClientJsonPath)!,
                    file.Path)
            ));
        }

        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.DownloadModrinthFiles,
            InstallerStageProgress.UpdateTotalTasks(downloadRequests.Count)
        ));

        var groupDownloadRequest = new GroupDownloadRequest(downloadRequests);
        groupDownloadRequest.SingleRequestCompleted += (_, _)
            => Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
                ModrinthModpackInstallationStage.DownloadModrinthFiles,
                InstallerStageProgress.IncrementFinishedTasks()
            ));

        var groupDownloadResult = await Downloader.DownloadFilesAsync(groupDownloadRequest, cancellationToken);

        if (CheckAllDependencies && groupDownloadResult.Failed.Count > 0)
            throw new IncompleteDependenciesException(groupDownloadResult.Failed, "Some dependent files encountered errors during download");

        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.DownloadModrinthFiles,
            InstallerStageProgress.Finished()
        ));
    }

    void CopyOverriddenFiles(
        MinecraftInstance minecraftInstance,
        ZipArchive packageArchive,
        CancellationToken cancellationToken)
    {
        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.CopyOverriddenFiles,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var entry in packageArchive.Entries)
        {
            if (!entry.FullName.StartsWith("overrides") || string.IsNullOrEmpty(entry.Name))
                continue;

            string targetFilePath = Path.Combine(
                Path.GetDirectoryName(minecraftInstance.ClientJsonPath)!,
                entry.FullName[10..]);
            string targetDirectory = Path.GetDirectoryName(targetFilePath)!;

            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            entry.ExtractToFile(targetFilePath, true);
        }

        Progress?.Report(new InstallerProgress<ModrinthModpackInstallationStage>(
            ModrinthModpackInstallationStage.CopyOverriddenFiles,
            InstallerStageProgress.Finished()
        ));
    }

    public enum ModrinthModpackInstallationStage
    {
        ParseModrinthModpack,
        SearchInstallData,
        InstallVanillaMinecraftInstance,
        InstallModifiedMinecraftInstance,
        DownloadModrinthFiles,
        CopyOverriddenFiles
    }

    private static readonly CreateModLoderInstallerProgressReporterDelegate EmptyDelegate = (ModLoaderType _, out IProgress<IInstallerProgress>? progress) =>
    {
        progress = null;
        return null;
    };
}
