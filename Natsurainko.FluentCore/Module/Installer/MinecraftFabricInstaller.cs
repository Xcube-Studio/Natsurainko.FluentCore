using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Download;
using Natsurainko.FluentCore.Model.Install;
using Natsurainko.FluentCore.Model.Install.Fabric;
using Natsurainko.FluentCore.Model.Parser;
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
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Module.Installer;

public class MinecraftFabricInstaller : BaseGameCoreInstaller
{
    public FabricInstallBuild FabricBuild { get; private set; }

    protected override Dictionary<string, GameCoreInstallerStepProgress> StepsProgress { get; set; } = new()
    {
        { "Parse Install Build", new () { StepName= "Parse Install Build"} },
        { "Download Libraries", new () { StepName = "Download Libraries", IsIndeterminate = false } },
        { "Check Inherited Core", new () { StepName = "Check Inherited Core", IsIndeterminate = false } },
        { "Write Files", new () { StepName = "Write Files" } }
    };

    public MinecraftFabricInstaller(
        IGameCoreLocator<IGameCore> coreLocator,
        FabricInstallBuild fabricInstallBuild,
        string customId = null) : base(coreLocator, fabricInstallBuild.McVersion ,customId)
    {
        FabricBuild = fabricInstallBuild;
    }

    public override Task<GameCoreInstallerResponse> InstallAsync()
    {
        ParallelDownloaderResponse parallelDownloaderResponse = default;
        List<LibraryJsonEntity> libraries = default;
        VersionJsonEntity jsonEntity = default;
        Stopwatch stopwatch = Stopwatch.StartNew();
        string mainClass = default;

        void ParseBuild()
        {
            OnProgressChanged("Parse Install Build", 0);

            libraries = FabricBuild.LauncherMeta.Libraries["common"];

            if (FabricBuild.LauncherMeta.Libraries["common"] != null)
                libraries.AddRange(FabricBuild.LauncherMeta.Libraries["client"]);

            libraries.Insert(0, new() { Name = FabricBuild.Intermediary.Maven });
            libraries.Insert(0, new() { Name = FabricBuild.Loader.Maven });

            mainClass = FabricBuild.LauncherMeta.MainClass.Type == JTokenType.Object
                ? FabricBuild.LauncherMeta.MainClass.ToObject<Dictionary<string, string>>()["client"]
                : string.IsNullOrEmpty(FabricBuild.LauncherMeta.MainClass.ToString())
                    ? "net.minecraft.client.main.Main"
                    : FabricBuild.LauncherMeta.MainClass.ToString();

            if (mainClass == "net.minecraft.client.main.Main")
                throw new ArgumentNullException("MainClass");

            OnProgressChanged("Parse Install Build", 1);
        }

        async Task DownloadLibraries()
        {
            OnProgressChanged("Download Libraries", 0);

            libraries.ForEach(x => x.Url = UrlExtension.Combine("https://maven.fabricmc.net", UrlExtension.Combine(LibraryResource.FormatName(x.Name).ToArray())));

            using var downloader = new ParallelDownloader<LibraryResource>(
                libraries.Select(x => new LibraryResource { Root = GameCoreLocator.Root, Name = x.Name, Url = x.Url }),
                x => x.ToDownloadRequest()); 
            downloader.DownloadProgressChanged += (sender, e) =>
                OnProgressChanged($"Download Libraries", e.Progress, e.TotleTasks, e.CompletedTasks);

            downloader.BeginDownload();
            parallelDownloaderResponse = await downloader.CompleteAsync();

            OnProgressChanged("Download Libraries", 1);
        }

        void WriteFiles()
        {
            OnProgressChanged("Write Files", 0);

            jsonEntity = new VersionJsonEntity
            {
                Id = string.IsNullOrEmpty(CustomId) ? $"fabric-loader-{FabricBuild.Loader.Version}-{FabricBuild.Intermediary.Version}" : CustomId,
                InheritsFrom = FabricBuild.McVersion,
                ReleaseTime = DateTime.Now.ToString("O"),
                Time = DateTime.Now.ToString("O"),
                Type = "release",
                JavaVersion = null,
                MainClass = mainClass,
                Arguments = new() { Jvm = new() { "-DFabricMcEmu= net.minecraft.client.main.Main" } },
                Libraries = libraries
            };

            if (!string.IsNullOrEmpty(CustomId))
                jsonEntity.Id = CustomId;

            var versionJsonFile = new FileInfo(Path.Combine(GameCoreLocator.Root.FullName, "versions", jsonEntity.Id, $"{jsonEntity.Id}.json"));

            if (!versionJsonFile.Directory.Exists)
                versionJsonFile.Directory.Create();

            File.WriteAllText(versionJsonFile.FullName, jsonEntity.ToJson());

            OnProgressChanged("Write Files", 1);
        }

        return Task.Run(async () =>
        {
            ParseBuild();
            await DownloadLibraries();
            await CheckInheritedCore();
            WriteFiles();

            stopwatch.Stop();

            return new GameCoreInstallerResponse
            {
                Success = true,
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
            using var responseMessage = await HttpWrapper.HttpGetAsync("https://meta.fabricmc.net/v2/versions/game");
            responseMessage.EnsureSuccessStatusCode();

            return JArray.Parse(await responseMessage.Content.ReadAsStringAsync()).Select(x => (string)x["version"]).ToArray();
        }
        catch (Exception ex)
        {
            return Array.Empty<string>();
        }
    }

    public static async Task<FabricMavenItem[]> GetFabricLoaderMavensAsync()
    {
        try
        {
            using var responseMessage = await HttpWrapper.HttpGetAsync("https://meta.fabricmc.net/v2/versions/loader");
            responseMessage.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<FabricMavenItem[]>(await responseMessage.Content.ReadAsStringAsync());
        }
        catch
        {
            return Array.Empty<FabricMavenItem>();
        }
    }

    public static async Task<FabricInstallBuild[]> GetFabricBuildsFromMcVersionAsync(string mcVersion)
    {
        try
        {
            using var responseMessage = await HttpWrapper.HttpGetAsync($"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}");
            responseMessage.EnsureSuccessStatusCode();

            var list = JsonConvert.DeserializeObject<List<FabricInstallBuild>>(await responseMessage.Content.ReadAsStringAsync());

            list.Sort((a, b) => new Version(a.Loader.Version.Replace(a.Loader.Separator, ".")).CompareTo(new Version(b.Loader.Version.Replace(b.Loader.Separator, "."))));
            list.Reverse();

            return list.ToArray();
        }
        catch // (Exception ex)
        {
            return Array.Empty<FabricInstallBuild>();
        }
    }
}
