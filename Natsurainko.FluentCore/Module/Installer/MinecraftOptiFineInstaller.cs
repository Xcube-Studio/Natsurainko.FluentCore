using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Download;
using Natsurainko.FluentCore.Model.Install;
using Natsurainko.FluentCore.Model.Install.OptiFine;
using Natsurainko.FluentCore.Model.Parser;
using Natsurainko.FluentCore.Service;
using Natsurainko.Toolkits.IO;
using Natsurainko.Toolkits.Network;
using Natsurainko.Toolkits.Network.Downloader;
using Natsurainko.Toolkits.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Module.Installer;

public class MinecraftOptiFineInstaller : BaseGameCoreInstaller
{
    public string JavaPath { get; private set; }

    public OptiFineInstallBuild OptiFineBuild { get; private set; }

    protected override Dictionary<string, GameCoreInstallerStepProgress> StepsProgress { get; set; } = new()
    {
        { "Download OptiFine Package", new () { StepName = "Download OptiFine Package", IsIndeterminate = false } },
        { "Parse Installer Package", new () { StepName= "Parse Installer Package"} },
        { "Check Inherited Core", new () { StepName = "Check Inherited Core", IsIndeterminate = false } },
        { "Write Files", new () { StepName = "Write Files" } },
        { "Run Processor", new() { StepName = "Run Processor" } }
    };

    public string PackageFile { get; private set; }

    public MinecraftOptiFineInstaller(
        IGameCoreLocator<IGameCore> coreLocator,
        OptiFineInstallBuild build,
        string javaPath,
        string packageFile = null,
        string customId = null) : base(coreLocator, build.McVersion, customId)
    {
        OptiFineBuild = build;
        JavaPath = javaPath;
        PackageFile = packageFile;
    }

    public override Task<GameCoreInstallerResponse> InstallAsync()
    {
        string launchwrapper = default;
        string inheritsFromFile = default;
        ZipArchive archive = default;
        FileInfo optiFineLibraryFile = default;
        Stopwatch stopwatch = Stopwatch.StartNew();
        VersionJsonEntity jsonEntity = default;

        var outputs = new List<string>();
        var errorOutputs = new List<string>();

        async Task DownloadPackage()
        {
            OnProgressChanged("Download OptiFine Package", 0);

            if (string.IsNullOrEmpty(PackageFile) || !File.Exists(PackageFile))
            {
                var downloadUrl = (DownloadApiManager.Current.Host.Equals(DownloadApiManager.Mojang.Host) ? DownloadApiManager.Bmcl.Host : DownloadApiManager.Current.Host) +
                    $"/optifine/{OptiFineBuild.McVersion}/{OptiFineBuild.Type}/{OptiFineBuild.Patch}";

                using var packageDownloader = new SimpleDownloader(new DownloadRequest()
                {
                    Url = downloadUrl,
                    Directory = GameCoreLocator.Root
                });

                packageDownloader.DownloadProgressChanged += (sender, e) =>
                    OnProgressChanged("Download OptiFine Package", e.Progress);

                packageDownloader.BeginDownload();
                var downloadResponse = await packageDownloader.CompleteAsync();

                if (downloadResponse.HttpStatusCode != HttpStatusCode.OK)
                    throw new HttpRequestException(downloadResponse.HttpStatusCode.ToString());

                PackageFile = downloadResponse.Result.FullName;
            }

            OnProgressChanged("Download OptiFine Package", 1);
        }

        void ParsePackage()
        {
            OnProgressChanged("Parse Installer Package", 0);

            archive = ZipFile.OpenRead(PackageFile);
            launchwrapper = archive.GetEntry("launchwrapper-of.txt") != null
                ? archive.GetEntry("launchwrapper-of.txt").GetString()
                : "1.12";

            OnProgressChanged("Parse Installer Package", 1);
        }

        async Task WriteFiles()
        {
            OnProgressChanged("Write Files", 0);

            jsonEntity = new VersionJsonEntity
            {
                Id = string.IsNullOrEmpty(CustomId) ? $"{OptiFineBuild.McVersion}-OptiFine-{OptiFineBuild.Type}_{OptiFineBuild.Patch}" : CustomId,
                InheritsFrom = OptiFineBuild.McVersion,
                Time = DateTime.Now.ToString("O"),
                ReleaseTime = DateTime.Now.ToString("O"),
                Type = "release",
                Libraries = new()
                {
                    new () { Name = $"optifine:Optifine:{OptiFineBuild.DisplayVersion}" },
                    new () { Name = launchwrapper.Equals("1.12") ? "net.minecraft:launchwrapper:1.12" : $"optifine:launchwrapper-of:{launchwrapper}" }
                },
                MainClass = "net.minecraft.launchwrapper.Launch",
                Arguments = new()
                {
                    Game = new()
                    {
                        "--tweakClass",
                        "optifine.OptiFineTweaker"
                    }
                }
            };

            var versionJsonFile = new FileInfo(Path.Combine(GameCoreLocator.Root.FullName, "versions", jsonEntity.Id, $"{jsonEntity.Id}.json"));

            if (!versionJsonFile.Directory.Exists)
                versionJsonFile.Directory.Create();

            File.WriteAllText(versionJsonFile.FullName, jsonEntity.ToJson());

            var launchwrapperLibrary = new LibraryResource() { Name = jsonEntity.Libraries[1].Name, Root = GameCoreLocator.Root };
            var launchwrapperFile = launchwrapperLibrary.ToFileInfo();

            if (!launchwrapper.Equals("1.12"))
            {
                if (!launchwrapperFile.Directory.Exists)
                    launchwrapperFile.Directory.Create();

                archive.GetEntry($"launchwrapper-of-{launchwrapper}.jar").ExtractToFile(launchwrapperFile.FullName, true);
            }
            else if (!launchwrapperFile.Exists)
                await SimpleDownloader.StartNewDownloadAsync(launchwrapperLibrary.ToDownloadRequest());

            inheritsFromFile = Path.Combine(GameCoreLocator.Root.FullName, "versions", OptiFineBuild.McVersion, $"{OptiFineBuild.McVersion}.jar");
            File.Copy(inheritsFromFile, Path.Combine(versionJsonFile.Directory.FullName, $"{jsonEntity.Id}.jar"), true);

            optiFineLibraryFile = new LibraryResource { Name = jsonEntity.Libraries[0].Name, Root = GameCoreLocator.Root }.ToFileInfo();

            if (!optiFineLibraryFile.Directory.Exists)
                optiFineLibraryFile.Directory.Create();

            OnProgressChanged("Write Files", 1);
        }

        void RunProcessor()
        {
            OnProgressChanged("Run Processor", 0);

            using var process = Process.Start(new ProcessStartInfo(JavaPath)
            {
                UseShellExecute = false,
                WorkingDirectory = GameCoreLocator.Root.FullName,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = string.Join(" ", new string[]
                {
                    "-cp",
                    PackageFile.ToPath(),
                    "optifine.Patcher",
                    inheritsFromFile.ToPath(),
                    PackageFile.ToPath(),
                    optiFineLibraryFile.FullName.ToPath()
                })
            });

            void AddOutput(string data, bool error = false)
            {
                if (!string.IsNullOrEmpty(data))
                {
                    outputs.Add(data);

                    if (error)
                        errorOutputs.Add(data);
                }
            }

            process.OutputDataReceived += (_, args) => AddOutput(args.Data);
            process.ErrorDataReceived += (_, args) => AddOutput(args.Data, true);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            OnProgressChanged("Run Processor", 1);
        }

        return Task.Run(async () =>
        {
            await DownloadPackage();
            ParsePackage();
            await CheckInheritedCore();
            await WriteFiles();
            RunProcessor();

            stopwatch.Stop();
            archive.Dispose();
            File.Delete(PackageFile);

            return new GameCoreInstallerResponse
            {
                Success = errorOutputs.Count <= 0,
                Stopwatch = stopwatch,
                GameCore = GameCoreLocator.GetGameCore(jsonEntity.Id)
            };
        }).ContinueWith(task =>
        {
            if (stopwatch.IsRunning)
                stopwatch.Stop();

            return task.IsFaulted ? new()
            {
                Success = false,
                Stopwatch = stopwatch,
                Exception = task.Exception
            } : task.Result;
        });
    }

    public static async Task<string[]> GetSupportedMcVersionsAsync()
    {
        try
        {
            using var responseMessage = await HttpWrapper.HttpGetAsync($"{(DownloadApiManager.Current.Host.Equals(DownloadApiManager.Mojang.Host) ? DownloadApiManager.Bmcl.Host : DownloadApiManager.Current.Host)}/optifine/versionList");
            responseMessage.EnsureSuccessStatusCode();

            return JArray.Parse(await responseMessage.Content.ReadAsStringAsync()).Select(x => (string)x["mcversion"]).ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public static async Task<OptiFineInstallBuild[]> GetOptiFineBuildsFromMcVersionAsync(string mcVersion)
    {
        try
        {
            using var responseMessage = await HttpWrapper.HttpGetAsync($"{(DownloadApiManager.Current.Host.Equals(DownloadApiManager.Mojang.Host) ? DownloadApiManager.Bmcl.Host : DownloadApiManager.Current.Host)}/optifine/{mcVersion}");
            responseMessage.EnsureSuccessStatusCode();

            var list = JsonConvert.DeserializeObject<List<OptiFineInstallBuild>>(await responseMessage.Content.ReadAsStringAsync());

            var preview = list.Where(x => x.Patch.StartsWith("pre")).ToList();
            var release = list.Where(x => !x.Patch.StartsWith("pre")).ToList();

            release.Sort((a, b) => $"{a.Type}_{a.Patch}".CompareTo($"{b.Type}_{b.Patch}"));
            preview.Sort((a, b) => $"{a.Type}_{a.Patch}".CompareTo($"{b.Type}_{b.Patch}"));

            var builds = preview.Union(release).ToList();
            builds.Reverse();

            return builds.ToArray();
        }
        catch
        {
            return Array.Empty<OptiFineInstallBuild>();
        }
    }

    public static async Task<DownloadResponse> DownloadOptiFinePackageFromBuildAsync(OptiFineInstallBuild build, DirectoryInfo directory)
    {
        var downloadUrl = $"{(DownloadApiManager.Current.Host.Equals(DownloadApiManager.Mojang.Host) ? DownloadApiManager.Bmcl.Host : DownloadApiManager.Current.Host)}/optifine/{build.McVersion}/{build.Type}/{build.Patch}";

        return await SimpleDownloader.StartNewDownloadAsync(new DownloadRequest()
        {
            Url = downloadUrl,
            Directory = directory
        });
    }
}
