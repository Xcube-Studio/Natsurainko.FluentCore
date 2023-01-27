using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Download;
using Natsurainko.FluentCore.Model.Install;
using Natsurainko.FluentCore.Model.Install.Forge;
using Natsurainko.FluentCore.Model.Parser;
using Natsurainko.FluentCore.Module.Parser;
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

public class MinecraftForgeInstaller : BaseGameCoreInstaller
{
    public string JavaPath { get; private set; }

    public ForgeInstallBuild ForgeBuild { get; private set; }

    protected override Dictionary<string, GameCoreInstallerStepProgress> StepsProgress { get; set; } = new()
    {
        { "Download Forge Package", new () { StepName = "Download Forge Package", IsIndeterminate = false } },
        { "Parse Installer Package", new () { StepName= "Parse Installer Package"} },
        { "Download Libraries", new () { StepName = "Download Libraries", IsIndeterminate = false } },
        { "Write Files", new () { StepName = "Write Files" } },
        { "Check Inherited Core", new () { StepName = "Check Inherited Core", IsIndeterminate = false } },
    };

    public string PackageFile { get; private set; }

    public MinecraftForgeInstaller(
        IGameCoreLocator<IGameCore> coreLocator,
        ForgeInstallBuild build,
        string javaPath,
        string packageFile = null,
        string customId = null) : base(coreLocator, build.McVersion, customId)
    {
        ForgeBuild = build;
        JavaPath = javaPath;
        PackageFile = packageFile;
    }

    public override Task<GameCoreInstallerResponse> InstallAsync()
    {
        VersionJsonEntity jsonEntity = default;
        FileInfo jsonFile = default;
        Stopwatch stopwatch = Stopwatch.StartNew();

        ParallelDownloaderResponse parallelDownloaderResponse = default;
        IEnumerable<ForgeInstallProcessorModel> processors = default;
        IEnumerable<LibraryResource> libraryResources = default;
        Dictionary<string, JObject> dataDictionary = default;
        JObject installProfile = default;
        ZipArchive archive = default;

        async Task DownloadPackage()
        {
            OnProgressChanged("Download Forge Package", 0);

            if (string.IsNullOrEmpty(PackageFile) || !File.Exists(PackageFile))
            {
                var downloadUrl = (DownloadApiManager.Current.Host.Equals(DownloadApiManager.Mojang.Host) ? DownloadApiManager.Bmcl.Host : DownloadApiManager.Current.Host) +
                    $"/forge/download/{ForgeBuild.Build}";

                using var packageDownloader = new SimpleDownloader(new DownloadRequest()
                {
                    Url = downloadUrl,
                    Directory = GameCoreLocator.Root
                });

                packageDownloader.DownloadProgressChanged += (sender, e) =>
                    OnProgressChanged("Download Forge Package", e.Progress);

                packageDownloader.BeginDownload();
                var downloadResponse = await packageDownloader.CompleteAsync();

                if (downloadResponse.HttpStatusCode != HttpStatusCode.OK)
                    throw new HttpRequestException(downloadResponse.HttpStatusCode.ToString());

                PackageFile = downloadResponse.Result.FullName;
            }

            OnProgressChanged("Download Forge Package", 1);
        }

        void ParsePackage()
        {
            OnProgressChanged("Parse Installer Package", 0);

            archive = ZipFile.OpenRead(PackageFile);

            installProfile = JObject.Parse(archive.GetEntry("install_profile.json").GetString());
            jsonEntity = GetVersionJsonEntity(archive, installProfile);

            dataDictionary = installProfile.ContainsKey("data")
                ? installProfile["data"].ToObject<Dictionary<string, JObject>>()
                : new();

            var installerLibraries = installProfile.ContainsKey("libraries")
                ? new LibraryParser(installProfile["libraries"].ToObject<IEnumerable<LibraryJsonEntity>>(), GameCoreLocator.Root).GetLibraries()
                : Array.Empty<LibraryResource>();

            libraryResources = new LibraryParser(jsonEntity.Libraries, GameCoreLocator.Root)
                .GetLibraries()
                .Union(installerLibraries);

            if (!string.IsNullOrEmpty(CustomId))
                jsonEntity.Id = CustomId;

            OnProgressChanged("Parse Installer Package", 1);

            if (!installProfile.ContainsKey("versionInfo"))
            {
                StepsProgress.Add("Parse Processor", new() { StepName = "Parse Processor" });
                StepsProgress.Add("Run Processor", new() { StepName = "Run Processor" });
            }
        }

        async Task DownloadLibraries()
        {
            OnProgressChanged("Download Libraries", 0);

            using var downloader = new ParallelDownloader<LibraryResource>(libraryResources, x => x.ToDownloadRequest());
            downloader.DownloadProgressChanged += (sender, e) =>
                OnProgressChanged($"Download Libraries", e.Progress, e.TotleTasks, e.CompletedTasks);

            if (DownloadApiManager.Current.Host.Equals(DownloadApiManager.Mojang.Host))
            {
                DownloadApiManager.Current = DownloadApiManager.Bmcl;
                downloader.DownloadCompleted += (_, args) => DownloadApiManager.Current = DownloadApiManager.Mojang;
            }

            downloader.BeginDownload();
            parallelDownloaderResponse = await downloader.CompleteAsync();

            OnProgressChanged("Download Libraries", 1);
        }

        void WriteFiles()
        {
            OnProgressChanged("Write Files", 0);

            string forgeLibrariesFolder = Path.Combine(GameCoreLocator.Root.FullName, "libraries", "net", "minecraftforge", "forge", ForgeBuild.DisplayVersion);

            if (installProfile.ContainsKey("install"))
            {
                var libraryPath = new LibraryResource
                {
                    Root = GameCoreLocator.Root,
                    Name = installProfile["install"]["path"].ToString()
                }.ToFileInfo().FullName;

                archive.GetEntry(installProfile["install"]["filePath"].ToString())
                    .ExtractTo(libraryPath);
            }

            if (archive.GetEntry("maven/") != null)
            {
                archive.GetEntry($"maven/net/minecraftforge/forge/{ForgeBuild.DisplayVersion}/forge-{ForgeBuild.DisplayVersion}.jar")?
                    .ExtractTo(Path.Combine(forgeLibrariesFolder, $"forge-{ForgeBuild.DisplayVersion}.jar"));
                archive.GetEntry($"maven/net/minecraftforge/forge/{ForgeBuild.DisplayVersion}/forge-{ForgeBuild.DisplayVersion}-universal.jar")?
                    .ExtractTo(Path.Combine(forgeLibrariesFolder, $"forge-{ForgeBuild.DisplayVersion}-universal.jar"));
            }

            if (dataDictionary.Any())
            {
                archive.GetEntry("data/client.lzma").ExtractTo(Path.Combine(forgeLibrariesFolder, $"forge-{ForgeBuild.DisplayVersion}-clientdata.lzma"));
                archive.GetEntry("data/server.lzma").ExtractTo(Path.Combine(forgeLibrariesFolder, $"forge-{ForgeBuild.DisplayVersion}-serverdata.lzma"));
            }

            jsonFile = new FileInfo(Path.Combine(GameCoreLocator.Root.FullName, "versions", jsonEntity.Id, $"{jsonEntity.Id}.json"));

            if (!jsonFile.Directory.Exists)
                jsonFile.Directory.Create();

            File.WriteAllText(jsonFile.FullName, jsonEntity.ToJson());

            OnProgressChanged("Write Files", 1);
        }

        void ParseProcessor()
        {
            OnProgressChanged("Parse Processor", 0);

            dataDictionary["BINPATCH"]["client"] = $"[net.minecraftforge:forge:{ForgeBuild.DisplayVersion}:clientdata@lzma]";
            dataDictionary["BINPATCH"]["server"] = $"[net.minecraftforge:forge:{ForgeBuild.DisplayVersion}:serverdata@lzma]";

            var replaceValues = new Dictionary<string, string>
            {
                { "{SIDE}", "client" },
                { "{MINECRAFT_JAR}", Path.Combine(GameCoreLocator.Root.FullName, "versions", ForgeBuild.McVersion, $"{ForgeBuild.McVersion}.jar") },
                { "{MINECRAFT_VERSION}", ForgeBuild.McVersion },
                { "{ROOT}", GameCoreLocator.Root.FullName },
                { "{INSTALLER}", PackageFile },
                { "{LIBRARY_DIR}", Path.Combine(GameCoreLocator.Root.FullName, "libraries") }
            };

            var replaceProcessorArgs = dataDictionary.ToDictionary(
                x => $"{{{x.Key}}}",
                x => x.Value["client"].ToString().StartsWith("[")
                    ? CombineLibraryName(x.Value["client"].ToString())
                    : x.Value["client"].ToString());

            processors = installProfile["processors"].ToObject<IEnumerable<ForgeInstallProcessorModel>>()
                .Where(x => !x.Sides.Any() || x.Sides.Contains("client"));

            foreach (var processor in processors)
            {
                processor.Args = processor.Args.Select(x => x.StartsWith("[")
                    ? CombineLibraryName(x)
                    : x.Replace(replaceProcessorArgs).Replace(replaceValues));

                processor.Outputs = processor.Outputs.ToDictionary(
                    kvp => kvp.Key.Replace(replaceProcessorArgs),
                    kvp => kvp.Value.Replace(replaceProcessorArgs));
            }

            OnProgressChanged("Parse Processor", 1);
        }

        void RunProcessor()
        {
            OnProgressChanged("Run Processor", 0);

            var processes = new Dictionary<List<string>, List<string>>();

            var total = processors.Count();
            var completed = 0;

            foreach (var forgeInstallProcessor in processors)
            {
                var fileName = CombineLibraryName(forgeInstallProcessor.Jar);
                using var fileArchive = ZipFile.OpenRead(fileName);

                string mainClass = fileArchive.GetEntry("META-INF/MANIFEST.MF")
                    .GetString()
                    .Split("\r\n".ToCharArray())
                    .First(x => x.Contains("Main-Class: "))
                    .Replace("Main-Class: ", string.Empty);

                string classPath = string.Join(Path.PathSeparator.ToString(), new List<string> { forgeInstallProcessor.Jar }
                    .Concat(forgeInstallProcessor.Classpath)
                    .Select(x => new LibraryResource { Name = x, Root = GameCoreLocator.Root })
                    .Select(x => x.ToFileInfo().FullName));

                var args = new List<string>
                {
                    "-cp",
                    $"\"{classPath}\"",
                    mainClass
                };

                args.AddRange(forgeInstallProcessor.Args);

                using var process = Process.Start(new ProcessStartInfo(JavaPath)
                {
                    Arguments = string.Join(" ", args),
                    UseShellExecute = false,
                    WorkingDirectory = GameCoreLocator.Root.FullName,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                });

                var outputs = new List<string>();
                var errorOutputs = new List<string>();

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
                processes.Add(outputs, errorOutputs);

                completed++;

                OnProgressChanged($"Run Processor", (double)completed / total, total, completed);
            }

            OnProgressChanged("Run Processor", 1);
        }

        return Task.Run(async () =>
        {
            await DownloadPackage();
            ParsePackage();
            await DownloadLibraries();
            WriteFiles();
            await CheckInheritedCore();

            if (installProfile.ContainsKey("versionInfo"))
            {
                stopwatch.Stop();

                return new()
                {
                    Success = true,
                    Stopwatch = stopwatch,
                    GameCore = GameCoreLocator.GetGameCore(jsonEntity.Id),
                    DownloaderResponse = parallelDownloaderResponse
                };
            }

            ParseProcessor();
            RunProcessor();


            stopwatch.Stop();
            archive.Dispose();
            File.Delete(PackageFile);

            return new GameCoreInstallerResponse
            {
                Success = true,
                Stopwatch = stopwatch,
                GameCore = GameCoreLocator.GetGameCore(jsonEntity.Id),
                DownloaderResponse = parallelDownloaderResponse
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

    private static VersionJsonEntity GetVersionJsonEntity(ZipArchive archive, JObject installProfile)
    {
        if (installProfile.ContainsKey("versionInfo"))
            return installProfile["versionInfo"].ToObject<VersionJsonEntity>();

        var entry = archive.GetEntry("version.json");

        if (entry != null)
            return JsonConvert.DeserializeObject<VersionJsonEntity>(entry.GetString());

        return null;
    }

    private string CombineLibraryName(string name)
    {
        string libraries = Path.Combine(GameCoreLocator.Root.FullName, "libraries");

        foreach (var subPath in LibraryResource.FormatName(name.TrimStart('[').TrimEnd(']')))
            libraries = Path.Combine(libraries, subPath);

        return libraries;
    }

    public static async Task<string[]> GetSupportedMcVersionsAsync()
    {
        try
        {
            using var responseMessage = await HttpWrapper.HttpGetAsync($"{(DownloadApiManager.Current.Host.Equals(DownloadApiManager.Mojang.Host) ? DownloadApiManager.Bmcl.Host : DownloadApiManager.Current.Host)}/forge/minecraft");
            responseMessage.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<string[]>(await responseMessage.Content.ReadAsStringAsync());
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public static async Task<ForgeInstallBuild[]> GetForgeBuildsFromMcVersionAsync(string mcVersion)
    {
        try
        {
            using var responseMessage = await HttpWrapper.HttpGetAsync($"{(DownloadApiManager.Current.Host.Equals(DownloadApiManager.Mojang.Host) ? DownloadApiManager.Bmcl.Host : DownloadApiManager.Current.Host)}/forge/minecraft/{mcVersion}");
            responseMessage.EnsureSuccessStatusCode();

            var list = JsonConvert.DeserializeObject<List<ForgeInstallBuild>>(await responseMessage.Content.ReadAsStringAsync());

            list.Sort((a, b) => a.Build.CompareTo(b.Build));
            list.Reverse();

            return list.ToArray();
        }
        catch
        {
            return Array.Empty<ForgeInstallBuild>();
        }
    }

    public static async Task<DownloadResponse> DownloadForgePackageFromBuildAsync(int build, DirectoryInfo directory)
    {
        var downloadUrl = $"{(DownloadApiManager.Current.Host.Equals(DownloadApiManager.Mojang.Host) ? DownloadApiManager.Bmcl.Host : DownloadApiManager.Current.Host)}" +
            $"/forge/download/{build}";

        return await SimpleDownloader.StartNewDownloadAsync(new DownloadRequest()
        {
            Url = downloadUrl,
            Directory = directory
        });
    }
}
