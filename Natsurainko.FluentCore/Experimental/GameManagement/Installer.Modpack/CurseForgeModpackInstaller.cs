using Nrk.FluentCore.Exceptions;
using Nrk.FluentCore.Experimental.GameManagement.Modpacks;
using Nrk.FluentCore.GameManagement;
using Nrk.FluentCore.GameManagement.Downloader;
using Nrk.FluentCore.GameManagement.Installer;
using Nrk.FluentCore.GameManagement.Instances;
using Nrk.FluentCore.Resources;
using Nrk.FluentCore.Resources.CurseForge;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Nrk.FluentCore.Resources.CurseForge.CurseForgeModpackManifest;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer.Modpack;

public delegate IProgress<IInstallerProgress>? CreateModLoderInstallerProgressReporterDelegate(
    ModLoaderType modLoaderType, out IProgress<IInstallerProgress>? VanillaInstallationProgress);

public class CurseForgeModpackInstaller : IInstanceInstaller
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

    /// <summary>
    /// CurseForge Api 客户端
    /// </summary>
    public required CurseForgeClient CurseForgeClient { get; init; }

    public IDownloader Downloader { get; init; } = HttpUtils.Downloader;

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

        var stage = CurseForgeModpackInstallationStage.ParseCurseForgeModpack;

        try
        {
            ParseCurseForgeModpack(ModpackFilePath, cancellationToken, out packageArchive, out var modpackManifest, out var modLoaderInfo);

            stage = CurseForgeModpackInstallationStage.SearchInstallData;
            (VersionManifestItem, object?) installData = await SearchInstallData(modpackManifest, modLoaderInfo, cancellationToken);

            stage = modLoaderInfo is null
                ? CurseForgeModpackInstallationStage.InstallVanillaMinecraftInstance
                : CurseForgeModpackInstallationStage.InstallModifiedMinecraftInstance;
            instance = modLoaderInfo is null
                ? await InstallVanillaMinecraftInstance(installData, modpackManifest, cancellationToken)
                : await InstallModifiedMinecraftInstance(((VersionManifestItem, object))installData!, modpackManifest, (ModLoaderInfo)modLoaderInfo, cancellationToken);

            stage = CurseForgeModpackInstallationStage.ParseCurseForgeFiles;
            var requests = await ParseCurseForgeFiles(instance, modpackManifest, cancellationToken);

            stage = CurseForgeModpackInstallationStage.DownloadCurseForgeFiles;
            await DownloadCurseForgeFiles(requests, cancellationToken);

            stage = CurseForgeModpackInstallationStage.CopyOverriddenFiles;
            CopyOverriddenFiles(instance, modpackManifest, packageArchive, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 取消后清理产生的部分文件
            instance?.Delete();

            Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(stage, InstallerStageProgress.Failed()));
            throw;
        }
        catch
        {
            Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(stage, InstallerStageProgress.Failed()));
            throw;
        }
        finally
        {
            packageArchive?.Dispose();
        }

        return instance ?? throw new ArgumentNullException(nameof(instance), "Unexpected null reference to variable");
    }

    void ParseCurseForgeModpack(
        string packageFilePath,
        CancellationToken cancellationToken,
        out ZipArchive packageArchive,
        out CurseForgeModpackManifest modpackManifest,
        out ModLoaderInfo? modLoaderInfo)
    {
        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.ParseCurseForgeModpack,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        packageArchive = ZipFile.OpenRead(packageFilePath);
        var modpackInfo = ModpackInfoParser.ParseCurseForgeModpack(packageArchive, out modpackManifest);
        modLoaderInfo = modpackInfo.ModLoader;

        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.ParseCurseForgeModpack,
            InstallerStageProgress.Finished()
        ));
    }

    async Task<(VersionManifestItem, object?)> SearchInstallData(
        CurseForgeModpackManifest modpackManifest, 
        ModLoaderInfo? modLoaderInfo, 
        CancellationToken cancellationToken)
    {
        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.SearchInstallData,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        var value = await VersionManifestApi.SearchInstallDataAsync
        (
            modpackManifest.Minecraft.McVersion,
            modLoaderInfo,
            Downloader.HttpClient,
            Downloader.DownloadMirror,
            cancellationToken
        );

        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.SearchInstallData,
            InstallerStageProgress.Finished()
        ));

        return value;
    }

    async Task<MinecraftInstance> InstallVanillaMinecraftInstance(
        (VersionManifestItem, object?) modification,
        CurseForgeModpackManifest modpackManifest,
        CancellationToken cancellationToken)
    {
        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.InstallModifiedMinecraftInstance,
            InstallerStageProgress.Skiped()
        ));
        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.InstallVanillaMinecraftInstance,
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

        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.InstallVanillaMinecraftInstance,
            InstallerStageProgress.Finished()
        ));

        return await instanceInstaller.InstallAsync(cancellationToken);
    }

    async Task<MinecraftInstance> InstallModifiedMinecraftInstance(
        (VersionManifestItem, object) modification,
        CurseForgeModpackManifest modpackManifest,
        ModLoaderInfo modLoaderInfo, 
        CancellationToken cancellationToken)
    {
        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.InstallVanillaMinecraftInstance,
            InstallerStageProgress.Skiped()
        ));
        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.InstallModifiedMinecraftInstance,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        var minecraftInstanceParser = new MinecraftInstanceParser(MinecraftFolder);
        var vanillaMinecraftInstance = minecraftInstanceParser.ParseAllInstances()
            .OfType<VanillaMinecraftInstance>()
            .FirstOrDefault(i => i.Version.VersionId.Equals(modpackManifest.Minecraft.McVersion));

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

        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.InstallModifiedMinecraftInstance,
            InstallerStageProgress.Finished()
        ));

        return await instanceInstaller.InstallAsync(cancellationToken);
    }

    async Task<DownloadRequest[]> ParseCurseForgeFiles(
        MinecraftInstance instance,
        CurseForgeModpackManifest modpackManifest,
        CancellationToken cancellationToken)
    {
        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.ParseCurseForgeFiles,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        ConcurrentBag<DownloadRequest> downloadRequests = [];
        ConcurrentDictionary<CurseForgeModpackFileJsonObject, Exception> failedFiles = [];

        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.ParseCurseForgeFiles,
            InstallerStageProgress.UpdateTotalTasks(modpackManifest.Files.Length)
        ));

        await Parallel.ForEachAsync(modpackManifest.Files, cancellationToken, async (f, token) =>
        {
            if (!f.Required || f.ProjectId == 0 || f.ProjectId == 0)
            {
                Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
                    CurseForgeModpackInstallationStage.ParseCurseForgeFiles,
                    InstallerStageProgress.IncrementFinishedTasks()
                )); 
                return;
            }

            var curseForgeFile = new CurseForgeFile
            {
                FileId = f.FileId,
                ModId = f.ProjectId
            };

            try
            {
                (var resource, var fileDetails) = await TryParseResourceInfo(curseForgeFile, token);

                string requestUrl = fileDetails.DownloadUrl ?? throw new ArgumentNullException("fileDetails.DownloadUrl is null");
                string targetFilePath = GetResourceFolderPath(instance, fileDetails, resource) ?? throw new InvalidDataException();

                downloadRequests.Add(new DownloadRequest(requestUrl, targetFilePath));

                Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
                    CurseForgeModpackInstallationStage.ParseCurseForgeFiles,
                    InstallerStageProgress.IncrementFinishedTasks()
                ));
            }
            catch (Exception e)
            {
                failedFiles.TryAdd(f, e);
            }
        });

        if (!failedFiles.IsEmpty)
            throw new Exception("Could not parse all resourses in the modpack manifest");

        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.ParseCurseForgeFiles,
            InstallerStageProgress.Finished()
        ));

        return [.. downloadRequests];
    }

    async Task DownloadCurseForgeFiles(
        DownloadRequest[] downloadRequests,
        CancellationToken cancellationToken)
    {
        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.DownloadCurseForgeFiles,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.DownloadCurseForgeFiles,
            InstallerStageProgress.UpdateTotalTasks(downloadRequests.Length)
        ));

        var groupDownloadRequest = new GroupDownloadRequest(downloadRequests);
        groupDownloadRequest.SingleRequestCompleted += (_, _)
            => Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
                CurseForgeModpackInstallationStage.DownloadCurseForgeFiles,
                InstallerStageProgress.IncrementFinishedTasks()
            ));

        var groupDownloadResult = await Downloader.DownloadFilesAsync(groupDownloadRequest, cancellationToken);

        if (CheckAllDependencies && groupDownloadResult.Failed.Count > 0)
            throw new IncompleteDependenciesException(groupDownloadResult.Failed, "Some dependent files encountered errors during download");

        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.DownloadCurseForgeFiles,
            InstallerStageProgress.Finished()
        ));
    }

    void CopyOverriddenFiles(
        MinecraftInstance minecraftInstance,
        CurseForgeModpackManifest modpackManifest,
        ZipArchive packageArchive,
        CancellationToken cancellationToken)
    {
        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.CopyOverriddenFiles,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var entry in packageArchive.Entries)
        {
            if (!entry.FullName.StartsWith(modpackManifest.Overrides) || string.IsNullOrEmpty(entry.Name))
                continue;

            string targetFilePath = Path.Combine(
                Path.GetDirectoryName(minecraftInstance.ClientJsonPath)!,
                entry.FullName[(modpackManifest.Overrides.Length + 1)..]);
            string targetDirectory = Path.GetDirectoryName(targetFilePath)!;

            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            entry.ExtractToFile(targetFilePath, true);
        }

        Progress?.Report(new InstallerProgress<CurseForgeModpackInstallationStage>(
            CurseForgeModpackInstallationStage.CopyOverriddenFiles,
            InstallerStageProgress.Finished()
        ));
    }

    async Task<(CurseForgeResource, CurseForgeFileDetails)> TryParseResourceInfo(
        CurseForgeFile curseForgeFile,
        CancellationToken cancellationToken,
        int leftRetryCount = 3)
    {
        try
        {
            (CurseForgeResource, CurseForgeFileDetails) value =
            (
                await CurseForgeClient.GetResourceAsync(curseForgeFile.ModId),
                await CurseForgeClient.GetFileDetailsAsync(curseForgeFile, cancellationToken)
            );

            value.Item2.DownloadUrl ??= await TryGetAvailableDownloadUrl(value.Item2, cancellationToken);

            return value;
        }
        catch (Exception)
        {
            if (leftRetryCount > 0)
                return await TryParseResourceInfo(
                    curseForgeFile,
                    cancellationToken,
                    leftRetryCount - 1);

            throw;
        }
    }

    async Task<string?> TryGetAvailableDownloadUrl(CurseForgeFileDetails fileDetails, CancellationToken cancellationToken)
    {
        string fileIdStr = fileDetails.Id.ToString();
        string[] possibleDownloadUrls =
        [
            $"https://edge.forgecdn.net/files/{fileIdStr[..4]}/{fileIdStr[4..]}/{fileDetails.FileName}",
            $"https://mediafiles.forgecdn.net/files/{fileIdStr[..4]}/{fileIdStr[4..]}/{fileDetails.FileName}"
        ];

        foreach (var url in possibleDownloadUrls)
        {
            try
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Head, url);
                using var responseMessage = await Downloader.HttpClient.SendAsync(requestMessage, cancellationToken);

                if (responseMessage.IsSuccessStatusCode)
                    return url;
            }
            catch (Exception) { }
        }

        return null;
    }

    private static string? GetResourceFolderPath(
        MinecraftInstance minecraftInstance,
        CurseForgeFileDetails curseForgeFileDetails,
        CurseForgeResource curseForgeResource)
    {
        CurseForgeResourceType type = (CurseForgeResourceType)curseForgeResource.ClassId;
        string? resourceFolderName = type switch
        {
            CurseForgeResourceType.McMod => "mods",
            CurseForgeResourceType.TexturePack => "resourcepacks",
            CurseForgeResourceType.Shader => "shaderpacks",
            _ => null
        };

        if (resourceFolderName == null)
            return null;

        return Path.Combine(
            Path.GetDirectoryName(minecraftInstance.ClientJsonPath)!,
            resourceFolderName,
            curseForgeFileDetails.FileName);
    }

    public enum CurseForgeModpackInstallationStage
    {
        ParseCurseForgeModpack,
        SearchInstallData,
        InstallVanillaMinecraftInstance,
        InstallModifiedMinecraftInstance,
        ParseCurseForgeFiles,
        DownloadCurseForgeFiles,
        CopyOverriddenFiles
    }

    private static readonly CreateModLoderInstallerProgressReporterDelegate EmptyDelegate = (ModLoaderType _, out IProgress<IInstallerProgress>? progress) =>
    {
        progress = null;
        return null;
    };
}