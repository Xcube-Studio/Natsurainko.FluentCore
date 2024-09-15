using Nrk.FluentCore.Exceptions;
using Nrk.FluentCore.GameManagement.Downloader;
using Nrk.FluentCore.GameManagement.Instances;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static Nrk.FluentCore.GameManagement.Installer.VanillaInstanceInstaller;

namespace Nrk.FluentCore.GameManagement.Installer;

/// <summary>
/// OptiFine 实例安装器
/// </summary>
public class OptiFineInstanceInstaller : IInstanceInstaller
{
    public required string MinecraftFolder { get; init; }

    /// <summary>
    /// OptiFine 安装本身必须要使用 bmclapi，修改此项只会影响原版实例安装的镜像源（如果需要的话）
    /// </summary>
    public IDownloadMirror? DownloadMirror { get; init; }

    public bool CheckAllDependencies { get; init; }

    /// <summary>
    /// OptiFine 安装所需数据
    /// </summary>
    public required OptiFineInstallData InstallData { get; init; }

    /// <summary>
    /// 原版 Minecraft 版本清单项
    /// </summary>
    public required VersionManifestItem McVersionManifestItem { get; init; }

    /// <summary>
    /// 编译过程调用的 java.exe 路径
    /// </summary>
    public required string JavaPath { get; init; }

    /// <summary>
    /// 继承的原版实例（可选）
    /// </summary>
    public VanillaMinecraftInstance? InheritedInstance { get; init; }

    /// <summary>
    /// 自定义安装实例的 Id
    /// </summary>
    public string? CustomizedInstanceId { get; init; }

    public IProgress<InstallerProgress<OptiFineInstallationStage>>? Progress { get; init; }

    public IProgress<InstallerProgress<VanillaInstallationStage>>? VanillaInstallationProgress { get; init; }

    Task<MinecraftInstance> IInstanceInstaller.InstallAsync(CancellationToken cancellationToken)
        => InstallAsync(cancellationToken).ContinueWith(MinecraftInstance (t) => t.Result);

    public async Task<ModifiedMinecraftInstance> InstallAsync(CancellationToken cancellationToken = default)
    {
        VanillaMinecraftInstance? vanillaInstance;
        ModifiedMinecraftInstance? instance = null;

        FileInfo? optifinePackageFile = null;
        FileInfo? optifineClientJson = null;
        ZipArchive? packageArchive = null;

        var stage = OptiFineInstallationStage.ParseOrInstallVanillaInstance;
        try
        {
            vanillaInstance = await ParseOrInstallVanillaInstance(cancellationToken);

            stage = OptiFineInstallationStage.DownloadOptiFinePackage;
            optifinePackageFile = await DownloadOptiFinePackage(cancellationToken);

            stage = OptiFineInstallationStage.WriteDependenciesAndVersionFiles;
            ParseOptiFinePackage(optifinePackageFile.FullName, cancellationToken, out var _packageArchive, out var launchwrapperVersion, out string launchwrapperName);
            packageArchive = _packageArchive;
            optifineClientJson = await WriteDependenciesAndVersionFiles(vanillaInstance, packageArchive, launchwrapperVersion, launchwrapperName, cancellationToken);

            stage = OptiFineInstallationStage.RunCompileProcess;
            instance = ParseModifiedMinecraftInstance(optifineClientJson, cancellationToken);
            await RunCompileProcess(vanillaInstance, optifinePackageFile.FullName, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 取消后清理产生的部分文件

            if (instance != null)
            {
                optifineClientJson!.Directory?.DeleteAllFiles();
                optifineClientJson!.Directory?.Delete();
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
            optifinePackageFile?.Delete();
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
            OptiFineInstallationStage.ParseOrInstallVanillaInstance,
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
            Progress = VanillaInstallationProgress,
            CleanAfterCancelled = false
        };

        var instance = await vanillaInstanceInstaller.InstallAsync(cancellationToken);

        Progress?.Report(new(
            OptiFineInstallationStage.ParseOrInstallVanillaInstance,
            InstallerStageProgress.Finished()
        ));

        return instance;
    }

    /// <summary>
    /// 下载 OptiFine 安装包
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    async Task<FileInfo> DownloadOptiFinePackage(CancellationToken cancellationToken)
    {
        Progress?.Report(new(
            OptiFineInstallationStage.DownloadOptiFinePackage,
            InstallerStageProgress.Starting()
        ));

        string packageUrl = $"https://bmclapi2.bangbang93.com/optifine/{McVersionManifestItem.Id}/{InstallData.Type}/{InstallData.Patch}";
        var packageFile = new FileInfo(Path.Combine(MinecraftFolder, InstallData.FileName));

        var downloadRequest = new DownloadRequest(packageUrl, packageFile.FullName);
        var downloadResult = await HttpUtils.Downloader.DownloadFileAsync(downloadRequest, cancellationToken);

        if (downloadResult.Type == DownloadResultType.Failed)
            throw downloadResult.Exception!;

        Progress?.Report(new(
            OptiFineInstallationStage.DownloadOptiFinePackage,
            InstallerStageProgress.Finished()
        ));

        return packageFile;
    }

    /// <summary>
    /// 解析 OptiFine 安装包信息
    /// </summary>
    /// <param name="packageFilePath"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="packageArchive"></param>
    /// <param name="launchwrapperVersion"></param>
    /// <param name="launchwrapperName"></param>
    static void ParseOptiFinePackage(string packageFilePath, CancellationToken cancellationToken,
        out ZipArchive packageArchive, out string launchwrapperVersion, out string launchwrapperName)
    {
        cancellationToken.ThrowIfCancellationRequested();

        packageArchive = ZipFile.OpenRead(packageFilePath);
        launchwrapperVersion = packageArchive.GetEntry("launchwrapper-of.txt")?.ReadAsString() ?? "1.12";
        launchwrapperName = launchwrapperVersion.Equals("1.12")
                        ? "net.minecraft:launchwrapper:1.12"
                        : $"optifine:launchwrapper-of:{launchwrapperVersion}";
    }

    /// <summary>
    /// 写入部份依赖和版本文件 (json、jar)，返回 version.json
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="packageArchive"></param>
    /// <param name="launchwrapper"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    async Task<FileInfo> WriteDependenciesAndVersionFiles(VanillaMinecraftInstance instance, ZipArchive packageArchive,
        string launchwrapperVersion, string launchwrapperName, CancellationToken cancellationToken)
    {
        Progress?.Report(new(
            OptiFineInstallationStage.WriteDependenciesAndVersionFiles,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        if (launchwrapperVersion != "1.12")
        {
            var launchwrapperJar = packageArchive.GetEntry($"launchwrapper-of-{launchwrapperVersion}.jar")
                ?? throw new FileNotFoundException("Invalid OptiFine package");

            launchwrapperJar.ExtractTo(Path.Combine(MinecraftFolder, "libraries",
                StringExtensions.FormatLibraryNameToRelativePath(launchwrapperName)));
        }

        string instanceId = CustomizedInstanceId ?? $"{McVersionManifestItem.Id}-OptiFine-{InstallData.Patch}";
        var jsonFile = new FileInfo(Path.Combine(MinecraftFolder, "versions", instanceId, $"{instanceId}.json"));

        if (!jsonFile.Directory!.Exists)
            jsonFile.Directory.Create();

        var time = DateTime.Now.ToString("s");

        var jsonEntity = new OptiFineClientJson
        {
            Id = instanceId,
            InheritsFrom = instance.InstanceId,
            Time = time,
            ReleaseTime = time,
            Type = "release",
            Libraries = [
                new() { Name = $"optifine:Optifine:{McVersionManifestItem.Id}_{InstallData.Type}_{InstallData.Patch}" },
                new() { Name = launchwrapperName }
            ],
            MainClass = "net.minecraft.launchwrapper.Launch",
            MinecraftArguments = "--tweakClass optifine.OptiFineTweaker"
        };

        await File.WriteAllTextAsync(jsonFile.FullName,
            JsonSerializer.Serialize(
                jsonEntity,
                OptiFineInstallerJsonSerializerContext.Default.OptiFineClientJson),
            cancellationToken
        );

        if (instance.ClientJarPath is null || !File.Exists(instance.ClientJarPath))
            throw new FileNotFoundException("Unable to find the original client client.jar file");

        File.Copy(instance.ClientJarPath, jsonFile.FullName.Replace(".json", ".jar"), true);

        Progress?.Report(new(
            OptiFineInstallationStage.WriteDependenciesAndVersionFiles,
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
    /// 运行 OptiFine 编译进程
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="optifinePackagePath"></param>
    /// <param name="optifineLibName"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="OptiFineCompileProcessException"></exception>
    async Task RunCompileProcess(VanillaMinecraftInstance instance, string optifinePackagePath, CancellationToken cancellationToken)
    {
        Progress?.Report(new(
            OptiFineInstallationStage.RunCompileProcess,
            InstallerStageProgress.Starting()
        ));
        cancellationToken.ThrowIfCancellationRequested();

        string optifineLibName = $"optifine:Optifine:{McVersionManifestItem.Id}_{InstallData.Type}_{InstallData.Patch}";
        var optifineLibraryFile = new FileInfo(Path.Combine(MinecraftFolder, "libraries",
            StringExtensions.FormatLibraryNameToRelativePath(optifineLibName)));

        if (!optifineLibraryFile.Directory!.Exists)
            optifineLibraryFile.Directory.Create();

        using var process = Process.Start(
            new ProcessStartInfo(JavaPath)
            {
                UseShellExecute = false,
                WorkingDirectory = MinecraftFolder,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = string.Join(
                    " ",
                    [
                        "-cp",
                        optifinePackagePath.ToPathParameter(),
                        "optifine.Patcher",
                        instance.ClientJarPath.ToPathParameter(),
                        optifinePackagePath.ToPathParameter(),
                        optifineLibraryFile.FullName.ToPathParameter()
                    ]
                )
            }
        ) ?? throw new InvalidOperationException("Unable to run the compilation process");

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
            throw new OptiFineCompileProcessException(_errorOutputs);

        Progress?.Report(new(
            OptiFineInstallationStage.RunCompileProcess,
            InstallerStageProgress.Finished()
        ));
    }

    public enum OptiFineInstallationStage
    {
        ParseOrInstallVanillaInstance,
        DownloadOptiFinePackage,
        WriteDependenciesAndVersionFiles,
        RunCompileProcess
    }

    internal class OptiFineClientJson
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("inheritsFrom")]
        public string? InheritsFrom { get; set; }

        [JsonPropertyName("time")]
        public string? Time { get; set; }

        [JsonPropertyName("releaseTime")]
        public string? ReleaseTime { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("libraries")]
        public IEnumerable<OptiFineInstanceLibrary>? Libraries { get; set; }

        [JsonPropertyName("mainClass")]
        public string? MainClass { get; set; }

        [JsonPropertyName("minecraftArguments")]
        public string? MinecraftArguments { get; set; }
    }

    internal struct OptiFineInstanceLibrary
    {
        [JsonPropertyName("name")]
        [JsonRequired]
        public string Name { get; set; }
    }
}
