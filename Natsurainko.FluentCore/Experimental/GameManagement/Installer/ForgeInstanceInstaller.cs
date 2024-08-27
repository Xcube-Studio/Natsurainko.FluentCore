using Nrk.FluentCore.Experimental.Exceptions;
using Nrk.FluentCore.Experimental.GameManagement.Dependencies;
using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Experimental.GameManagement.Installer.Data;
using Nrk.FluentCore.Experimental.GameManagement.Instances;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using static Nrk.FluentCore.Experimental.GameManagement.Installer.VanillaInstanceInstaller;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer;

/// <summary>
/// (Neo)Forge 实例安装器
/// </summary>
public class ForgeInstanceInstaller : IInstanceInstaller
{
    public required string MinecraftFolder { get; init; }

    public IDownloadMirror? DownloadMirror { get; init; }

    public bool CheckAllDependencies { get; init; }

    /// <summary>
    /// (Neo)Forge 安装所需数据
    /// </summary>
    public required ForgeInstallData InstallData { get; init; }

    /// <summary>
    /// 原版 Minecraft 版本清单项
    /// </summary>
    public required VersionManifestItem McVersionManifestItem { get; init; }

    /// <summary>
    /// 编译过程调用的 java.exe 路径
    /// </summary>
    public required string JavaPath { get; init; }

    /// <summary>
    /// 确定安装 Forge 还是 NeoForge
    /// </summary>
    public required bool IsNeoForgeInstaller { get; init; }

    /// <summary>
    /// 继承的原版实例（可选）
    /// </summary>
    public VanillaMinecraftInstance? InheritedInstance { get; init; }

    /// <summary>
    /// 自定义安装实例的 Id
    /// </summary>
    public string? CustomizedInstanceId { get; init; }

    public IProgress<InstallerProgress<ForgeInstallationStage>>? Progress { get; init; }

    public IProgress<InstallerProgress<VanillaInstallationStage>>? VanillaInstallationProgress { get; init; }

    Task<MinecraftInstance> IInstanceInstaller.InstallAsync(CancellationToken cancellationToken)
        => InstallAsync(cancellationToken).ContinueWith(MinecraftInstance (t) => t.Result);

    public async Task<ModifiedMinecraftInstance> InstallAsync(CancellationToken cancellationToken = default)
    {
        VanillaMinecraftInstance? vanillaInstance;
        ModifiedMinecraftInstance? instance = null;

        FileInfo? forgePackageFile = null;
        FileInfo? forgeClientFile = null;
        ZipArchive? packageArchive = null;

        var stage = ForgeInstallationStage.ParseOrInstallVanillaInstance;
        try
        {
            vanillaInstance = await ParseOrInstallVanillaInstance(cancellationToken);

            stage = ForgeInstallationStage.DownloadForgePackage;
            forgePackageFile = await DownloadForgePackage(cancellationToken);

            stage = ForgeInstallationStage.WriteDependenciesAndVersionFiles;
            ParseForgePackage(forgePackageFile.FullName, cancellationToken, out var _packageArchive, out var installProfileJsonNode, out var isLegacyForgeVersion);
            packageArchive = _packageArchive;
            forgeClientFile = await WriteDependenciesAndVersionFiles(isLegacyForgeVersion, installProfileJsonNode, packageArchive, cancellationToken);

            stage = ForgeInstallationStage.DownloadForgeDependencies;
            instance = ParseModifiedMinecraftInstance(forgeClientFile, cancellationToken);
            await DownloadForgeDependencies(isLegacyForgeVersion, installProfileJsonNode, instance, cancellationToken);

            if (!isLegacyForgeVersion)
            {
                stage = ForgeInstallationStage.RunCompileProcess;
                await RunCompileProcess(installProfileJsonNode, vanillaInstance, forgePackageFile.FullName, cancellationToken);
            }
            else
            {
                Progress?.Report(new(
                    ForgeInstallationStage.RunCompileProcess,
                    InstallerStageProgress.Skiped()
                ));
            }
        }
        catch (OperationCanceledException)
        {
            // 取消后清理产生的部分文件

            if (instance != null)
            {
                forgeClientFile!.Directory?.DeleteAllFiles();
                forgeClientFile!.Directory?.Delete();
            }

            Progress?.Report(new(stage, InstallerStageProgress.Failed()));
            throw;
        }
        catch
        {
            Progress?.Report(new(stage, InstallerStageProgress.Failed()));
            throw;
        }
        finally
        {
            packageArchive?.Dispose();
            forgePackageFile?.Delete();
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
            ForgeInstallationStage.ParseOrInstallVanillaInstance,
            InstallerStageProgress.Starting()
        ));

        if (InheritedInstance != null)
            return InheritedInstance;

        var vanillaInstanceInstaller = new VanillaInstanceInstaller()
        {
            DownloadMirror = DownloadMirror,
            McVersionManifestItem = McVersionManifestItem,
            MinecraftFolder = MinecraftFolder,
            CheckAllDependencies = true,
            Progress = VanillaInstallationProgress
        };

        var instance = await vanillaInstanceInstaller.InstallAsync(cancellationToken);

        Progress?.Report(new(
            ForgeInstallationStage.ParseOrInstallVanillaInstance,
            InstallerStageProgress.Finished()
        ));

        return instance;
    }

    /// <summary>
    /// 下载 (Neo)Forge 安装包
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    async Task<FileInfo> DownloadForgePackage(CancellationToken cancellationToken)
    {
        Progress?.Report(new(
            ForgeInstallationStage.DownloadForgePackage,
            InstallerStageProgress.Starting()
        ));

        string packageUrl = IsNeoForgeInstaller 
            ? $"https://bmclapi2.bangbang93.com/neoforge/version/{InstallData.Version}/download/installer.jar"
            : $"https://bmclapi2.bangbang93.com/forge/download?mcversion={InstallData.McVersion}" +
                $"{(string.IsNullOrEmpty(InstallData.Branch) ? string.Empty : $"&branch={InstallData.Branch}")}" +
                $"&version={InstallData.Version}" +
                $"&category=installer&format=jar";

        string fileName = IsNeoForgeInstaller
            ? $"neoforge-{InstallData.Version}-installer.jar"
            : $"forge-{InstallData.McVersion}-{InstallData.Version}" +
                $"{(string.IsNullOrEmpty(InstallData.Branch) ? string.Empty : $"-{InstallData.Branch}")}" +
                $"-installer.jar";

        var packageFile = new FileInfo(Path.Combine(MinecraftFolder, fileName));

        var downloadRequest = new DownloadRequest(packageUrl, packageFile.FullName);
        var downloadResult = await HttpUtils.Downloader.DownloadFileAsync(downloadRequest, cancellationToken);

        if (downloadResult.Type == DownloadResultType.Failed)
            throw downloadResult.Exception!;

        Progress?.Report(new(
            ForgeInstallationStage.DownloadForgePackage,
            InstallerStageProgress.Finished()
        ));

        return packageFile;
    }

    /// <summary>
    /// 解析 Forge 安装包信息
    /// </summary>
    /// <param name="packageFilePath"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="packageArchive"></param>
    /// <param name="installProfileJsonNode"></param>
    /// <param name="isLegacyForgeVersion"></param>
    /// <exception cref="Exception"></exception>
    static void ParseForgePackage(
        string packageFilePath, 
        CancellationToken cancellationToken,
        out ZipArchive packageArchive,
        out JsonNode installProfileJsonNode,
        out bool isLegacyForgeVersion)
    {
        cancellationToken.ThrowIfCancellationRequested();

        packageArchive = ZipFile.OpenRead(packageFilePath);
        installProfileJsonNode = packageArchive
            .GetEntry("install_profile.json")
            ?.ReadAsString()
            .ToJsonNode()
            ?? throw new Exception("Failed to parse install_profile.json");

        isLegacyForgeVersion = installProfileJsonNode["install"] != null;
    }

    /// <summary>
    /// 写入部份依赖和版本文件 (json、jar)，返回 version.json
    /// </summary>
    /// <param name="isLegacyForgeVersion"></param>
    /// <param name="installProfileJsonNode"></param>
    /// <param name="packageArchive"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    async Task<FileInfo> WriteDependenciesAndVersionFiles(
        bool isLegacyForgeVersion,
        JsonNode installProfileJsonNode,
        ZipArchive packageArchive, 
        CancellationToken cancellationToken)
    {
        Progress?.Report(new(
            ForgeInstallationStage.WriteDependenciesAndVersionFiles,
            InstallerStageProgress.Starting()
        ));

        string forgeVersion = $"{InstallData.McVersion}-{InstallData.Version}";
        string forgeLibsFolder = Path.Combine(
            MinecraftFolder,
            "libraries\\net\\minecraftforge\\forge",
            forgeVersion);

        if (isLegacyForgeVersion)
        {
            var universalFilePath = installProfileJsonNode["install"]?["filePath"]?.GetValue<string>()
                ?? throw new InvalidDataException("Unable to resolve location of universal file in archive");
            var universalFileEntry = packageArchive.GetEntry(universalFilePath) ?? throw new FileNotFoundException("The universal file was not found in the archive");
            universalFileEntry.ExtractTo(Path.Combine(forgeLibsFolder, universalFileEntry.Name));
        }

        if (packageArchive.GetEntry($"maven/net/minecraftforge/forge/{forgeVersion}") != null)
            foreach (var entry in packageArchive.Entries.Where(x => x.FullName.StartsWith($"maven/net/minecraftforge/forge/{forgeVersion}")))
                entry.ExtractTo(Path.Combine(forgeLibsFolder, entry.Name));

        packageArchive.GetEntry("data/client.lzma")?.ExtractTo(Path.Combine(forgeLibsFolder, $"forge-{forgeVersion}-clientdata.lzma"));

        string jsonContent = (isLegacyForgeVersion
            ? installProfileJsonNode["versionInfo"]!.GetValue<string>()
            : packageArchive.GetEntry("version.json")?.ReadAsString())
            ?? throw new Exception("Failed to read version.json");
        var jsonNode = JsonNode.Parse(jsonContent);

        string instanceId = CustomizedInstanceId ?? $"{McVersionManifestItem.Id}-{(IsNeoForgeInstaller ? "neoforge" : "forge")}-{InstallData.Version}";
        var jsonFile = new FileInfo(Path.Combine(MinecraftFolder, "versions", instanceId, $"{instanceId}.json"));

        if (!jsonFile.Directory!.Exists)
            jsonFile.Directory.Create();

        jsonNode!["id"] = instanceId;
        await File.WriteAllTextAsync(jsonFile.FullName, jsonNode.ToJsonString(), cancellationToken);

        Progress?.Report(new(
            ForgeInstallationStage.WriteDependenciesAndVersionFiles,
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
    /// 使用 MultipartDownloader 下载 Dependencies
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    async Task DownloadForgeDependencies(
        bool isLegacyForgeVersion,
        JsonNode installProfileJsonNode,
        MinecraftInstance instance, 
        CancellationToken cancellationToken)
    {
        Progress?.Report(new(
            ForgeInstallationStage.DownloadForgeDependencies,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        string forgeVersion = $"{InstallData.McVersion}-{InstallData.Version}";
        var dependencies = new List<MinecraftLibrary>();

        var libraries = instance.GetRequiredLibraries().Libraries.ToList();
        //foreach (var lib in libraries.Where(x => x.MavenName.Equals($"net.minecraftforge:forge:{forgeVersion}") 
        //    || x.MavenName.Equals($"net.minecraftforge:forge:{forgeVersion}:client") || x.Url == null))
        //    libraries.Remove(lib);

        foreach (var lib in libraries.Where(x => x.MavenName.Equals($"net.minecraftforge:forge:{forgeVersion}")
            || x.MavenName.Equals($"net.minecraftforge:forge:{forgeVersion}:client") || x is not IDownloadableDependency).ToArray())
            libraries.Remove(lib);

        dependencies.AddRange(libraries);

        if (!isLegacyForgeVersion)
        {
            var processorLibraries = installProfileJsonNode["libraries"]!
                .AsArray()
                .Deserialize<IEnumerable<ClientJsonObject.LibraryJsonObject>>()?
                .Select(lib => MinecraftLibrary.ParseJsonNode(lib, MinecraftFolder))
                ?? throw new InvalidDataException();

            foreach (var item in processorLibraries)
                if (!dependencies.Contains(item))
                    dependencies.Add(item);
        }

        Progress?.Report(new(
            ForgeInstallationStage.DownloadForgeDependencies,
            InstallerStageProgress.UpdateTotalTasks(dependencies.Count)));

        var groupDownloadRequest = new GroupDownloadRequest(dependencies.OfType<IDownloadableDependency>().Select(x => new DownloadRequest(
            DownloadMirror != null ? DownloadMirror.GetMirrorUrl(x.Url) : x.Url, x.FullPath)));

        groupDownloadRequest.SingleRequestCompleted += (_, _) 
            => Progress?.Report(new(
                ForgeInstallationStage.DownloadForgeDependencies,
                InstallerStageProgress.IncrementFinishedTasks()
            ));

        var groupDownloadResult = await HttpUtils.Downloader.DownloadFilesAsync(groupDownloadRequest, cancellationToken);

        if (CheckAllDependencies && groupDownloadResult.Failed.Count > 0)
            throw new IncompleteDependenciesException(groupDownloadResult.Failed, "Some dependent files encountered errors during download");

        Progress?.Report(new(
            ForgeInstallationStage.DownloadForgeDependencies,
            InstallerStageProgress.Finished()
        ));
    }

    /// <summary>
    /// 运行 Forge 编译进程
    /// </summary>
    /// <param name="installProfileJsonNode"></param>
    /// <param name="vanillaMinecraftInstance"></param>
    /// <param name="packageFilePath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ForgeCompileProcessException"></exception>
    async Task RunCompileProcess(
        JsonNode installProfileJsonNode,
        VanillaMinecraftInstance vanillaMinecraftInstance,
        string packageFilePath,
        CancellationToken cancellationToken)
    {
        Progress?.Report(new(
            ForgeInstallationStage.RunCompileProcess,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        var forgeDataDictionary = installProfileJsonNode["data"]
            .Deserialize<Dictionary<string, Dictionary<string, string>>>()
            ?? throw new Exception("Failed to parse install profile data");

        string forgeVersion = $"{InstallData.McVersion}-{InstallData.Version}";

        if (forgeDataDictionary.TryGetValue("BINPATCH", out Dictionary<string, string>? value))
        {
            value["client"] = $"[net.minecraftforge:forge:{forgeVersion}:clientdata@lzma]";
            value["server"] = $"[net.minecraftforge:forge:{forgeVersion}:serverdata@lzma]";
        }

        var replaceValues = new Dictionary<string, string>
        {
            { "{SIDE}", "client" },
            { "{MINECRAFT_JAR}", vanillaMinecraftInstance.ClientJarPath },
            { "{MINECRAFT_VERSION}", InstallData.McVersion },
            { "{ROOT}", MinecraftFolder.ToPathParameter() },
            { "{INSTALLER}", packageFilePath.ToPathParameter() },
            { "{LIBRARY_DIR}", Path.Combine(MinecraftFolder, "libraries").ToPathParameter() }
        };

        var replaceProcessorArgs = forgeDataDictionary.ToDictionary(
            kvp => $"{{{kvp.Key}}}", kvp =>
            {
                var value = kvp.Value["client"];
                if (!value.StartsWith('[')) return value;

                return Path.Combine(MinecraftFolder, "libraries", StringExtensions.FormatLibraryNameToRelativePath(value.TrimStart('[').TrimEnd(']')))
                    .ToPathParameter();
            });

        var forgeProcessors = installProfileJsonNode["processors"]?
            .Deserialize<IEnumerable<ForgeProcessorData>>()?
            .Where(x => !(x.Sides.Count == 1 && x.Sides.Contains("server")))
            .ToArray() 
            ?? throw new InvalidDataException("Unable to parse Forge Processors");

        Progress?.Report(new(
            ForgeInstallationStage.RunCompileProcess,
            InstallerStageProgress.UpdateTotalTasks(forgeProcessors.Length)
        ));

        foreach (var processor in forgeProcessors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            processor.Args = processor.Args.Select(x =>
            {
                if (x.StartsWith("["))
                    return Path.Combine(MinecraftFolder, "libraries", StringExtensions.FormatLibraryNameToRelativePath(x.TrimStart('[').TrimEnd(']')))
                        .ToPathParameter();

                return x.ReplaceFromDictionary(replaceProcessorArgs)
                    .ReplaceFromDictionary(replaceValues);
            });

            processor.Outputs = processor.Outputs.ToDictionary(
                kvp => kvp.Key.ReplaceFromDictionary(replaceProcessorArgs),
                kvp => kvp.Value.ReplaceFromDictionary(replaceProcessorArgs));

            var fileName = Path.Combine(MinecraftFolder, "libraries", StringExtensions.FormatLibraryNameToRelativePath(processor.Jar));

            using var fileArchive = ZipFile.OpenRead(fileName);
            string mainClass = fileArchive.GetEntry("META-INF/MANIFEST.MF")?
                .ReadAsString()
                .Split("\r\n".ToCharArray())
                .First(x => x.Contains("Main-Class: "))
                .Replace("Main-Class: ", string.Empty)
                ?? throw new InvalidDataException("Unable to find MainClass for Processor");

            string classPath = string.Join(Path.PathSeparator.ToString(), new List<string>() { fileName }
                .Concat(processor.Classpath.Select(x => Path.Combine(MinecraftFolder, "libraries", StringExtensions.FormatLibraryNameToRelativePath(x)))));

            var args = new List<string>
            {
                "-cp",
                classPath.ToPathParameter(),
                mainClass
            };

            args.AddRange(processor.Args);

            using var process = Process.Start(new ProcessStartInfo(JavaPath)
            {
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                WorkingDirectory = MinecraftFolder,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }) ?? throw new Exception("Failed to start Java");

            List<string> _errorOutputs = [];

            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is string data && !string.IsNullOrEmpty(data))
                    _errorOutputs.Add(args.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (_errorOutputs.Count > 0)
                throw new ForgeCompileProcessException(_errorOutputs);

            Progress?.Report(new(
                ForgeInstallationStage.RunCompileProcess,
                InstallerStageProgress.IncrementFinishedTasks()
            ));
        }

        Progress?.Report(new(
            ForgeInstallationStage.RunCompileProcess,
            InstallerStageProgress.Finished()
        ));
    }

    public enum ForgeInstallationStage
    {
        ParseOrInstallVanillaInstance,
        DownloadForgePackage,
        WriteDependenciesAndVersionFiles,
        DownloadForgeDependencies,
        RunCompileProcess
    }
}
