using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Download;
using Natsurainko.FluentCore.Model.Install;
using Natsurainko.FluentCore.Model.Install.Quilt;
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
using System.Linq;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Module.Installer;

public class MinecraftQuiltInstaller : BaseGameCoreInstaller
{
    public QuiltInstallBuild QuiltBuild { get; private set; }

    protected override Dictionary<string, GameCoreInstallerStepProgress> StepsProgress { get; set; } = new()
    {
        { "Parse Install Build", new () { StepName= "Parse Install Build"} },
        { "Download Libraries", new () { StepName = "Download Libraries", IsIndeterminate = false } },
        { "Check Inherited Core", new () { StepName = "Check Inherited Core", IsIndeterminate = false } },
        { "Write Files", new () { StepName = "Write Files" } }
    };

    public MinecraftQuiltInstaller(
        IGameCoreLocator<IGameCore> coreLocator,
        QuiltInstallBuild quiltInstallBuild,
        string customId = null) : base(coreLocator, quiltInstallBuild.McVersion, customId)
    {
        QuiltBuild = quiltInstallBuild;
    }

    public override Task<GameCoreInstallerResponse> InstallAsync()
    {
        ParallelDownloaderResponse parallelDownloaderResponse = default;
        List<LibraryJsonEntity> libraries = default;
        VersionJsonEntity jsonEntity = default;
        Stopwatch stopwatch = Stopwatch.StartNew();

        async Task ParseBuild()
        {
            OnProgressChanged("Parse Install Build", 0);

            var responseMessage = await HttpWrapper.HttpGetAsync($"https://meta.quiltmc.org/v3/versions/loader/{McVersion}/{QuiltBuild.BuildVersion}/profile/json");
            jsonEntity = JsonConvert.DeserializeObject<VersionJsonEntity>(await responseMessage.Content.ReadAsStringAsync());

            if (!string.IsNullOrEmpty(CustomId))
                jsonEntity.Id = CustomId;

            libraries = jsonEntity.Libraries;

            OnProgressChanged("Parse Install Build", 1);
        }

        async Task DownloadLibraries()
        {
            OnProgressChanged("Download Libraries", 0);

            libraries.ForEach(x => x.Url = UrlExtension.Combine(x.Url, UrlExtension.Combine(LibraryResource.FormatName(x.Name).ToArray())));

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

            var versionJsonFile = new FileInfo(Path.Combine(GameCoreLocator.Root.FullName, "versions", jsonEntity.Id, $"{jsonEntity.Id}.json"));

            if (!versionJsonFile.Directory.Exists)
                versionJsonFile.Directory.Create();

            File.WriteAllText(versionJsonFile.FullName, jsonEntity.ToJson());

            OnProgressChanged("Write Files", 1);
        }

        return Task.Run(async () =>
        {
            await ParseBuild();
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
            using var responseMessage = await HttpWrapper.HttpGetAsync("https://meta.quiltmc.org/v3/versions/game");
            responseMessage.EnsureSuccessStatusCode();

            return JArray.Parse(await responseMessage.Content.ReadAsStringAsync()).Select(x => (string)x["version"]).ToArray();
        }
        catch (Exception ex)
        {
            return Array.Empty<string>();
        }
    }

    public static async Task<QuiltMavenItem[]> GetQuiltLoaderMavensAsync()
    {
        try
        {
            using var responseMessage = await HttpWrapper.HttpGetAsync("https://meta.quiltmc.org/v3/versions/loader");
            responseMessage.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<QuiltMavenItem[]>(await responseMessage.Content.ReadAsStringAsync());
        }
        catch
        {
            return Array.Empty<QuiltMavenItem>();
        }
    }

    public static async Task<QuiltInstallBuild[]> GetQuiltBuildsFromMcVersionAsync(string mcVersion)
    {
        try
        {
            using var responseMessage = await HttpWrapper.HttpGetAsync($"https://meta.quiltmc.org/v3/versions/loader/{mcVersion}");
            responseMessage.EnsureSuccessStatusCode();

            var list = JsonConvert.DeserializeObject<List<QuiltInstallBuild>>(await responseMessage.Content.ReadAsStringAsync());

            list.Sort((a, b) => a.Loader.Version.CompareTo(b.Loader.Version));
            list.Reverse();

            return list.ToArray();
        }
        catch // (Exception ex)
        {
            return Array.Empty<QuiltInstallBuild>();
        }
    }
}
