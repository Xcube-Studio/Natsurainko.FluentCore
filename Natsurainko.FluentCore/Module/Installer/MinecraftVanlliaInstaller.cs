using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Install;
using Natsurainko.FluentCore.Model.Install.Vanilla;
using Natsurainko.FluentCore.Model.Parser;
using Natsurainko.FluentCore.Module.Downloader;
using Natsurainko.FluentCore.Service;
using Natsurainko.Toolkits.Network;
using Natsurainko.Toolkits.Network.Downloader;
using Natsurainko.Toolkits.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Module.Installer;

public class MinecraftVanlliaInstaller : BaseGameCoreInstaller
{
    public CoreManifestItem CoreManifestItem { get; private set; }

    protected override Dictionary<string, GameCoreInstallerStepProgress> StepsProgress { get; set; } = new()
    {
        { "Get Core Json", new () { StepName = "Get Core Json" } },
        { "Download Resources", new () { StepName= "Get Core Json", IsIndeterminate = false } }
    };

    public MinecraftVanlliaInstaller(
        IGameCoreLocator<IGameCore> coreLocator,
        string mcVersion,
        string customId = default)
        : base(coreLocator, mcVersion, customId)
    {
        CoreManifestItem = GetCoreManifest().Cores
            .Where(x => x.Id.Equals(mcVersion))
            .First();
    }

    public MinecraftVanlliaInstaller(
        IGameCoreLocator<IGameCore> coreLocator,
        CoreManifestItem coreManifestItem,
        string customId = default)
        : base(coreLocator, coreManifestItem.Id, customId)
    {
        CoreManifestItem = coreManifestItem;
    }

    public override Task<GameCoreInstallerResponse> InstallAsync()
    {
        VersionJsonEntity jsonEntity = default;
        FileInfo jsonFile = default;
        IGameCore gameCore = default;
        ParallelDownloaderResponse parallelDownloaderResponse = default;
        Stopwatch stopwatch = Stopwatch.StartNew();

        async Task GetCoreJson()
        {
            OnProgressChanged("Get Core Json", 0);

            using var responseMessage = await HttpWrapper.HttpGetAsync(CoreManifestItem.Url);
            responseMessage.EnsureSuccessStatusCode();

            jsonEntity = JsonConvert.DeserializeObject<VersionJsonEntity>(await responseMessage.Content.ReadAsStringAsync());

            if (!string.IsNullOrEmpty(CustomId))
                jsonEntity.Id = CustomId;

            jsonFile = new(Path.Combine(GameCoreLocator.Root.FullName, "versions", jsonEntity.Id, $"{jsonEntity.Id}.json")); ;

            if (!jsonFile.Directory.Exists)
                jsonFile.Directory.Create();

            File.WriteAllText(jsonFile.FullName, jsonEntity.ToJson());
            gameCore = GameCoreLocator.GetGameCore(jsonEntity.Id);

            OnProgressChanged("Get Core Json", 1);
        }

        async Task DownloadResources()
        {
            OnProgressChanged("Download Resources", 0);

            var resourceDownloader = new ResourceDownloader(gameCore);

            resourceDownloader.DownloadProgressChanged += (sender, e)
                => OnProgressChanged($"Download Resources", e.Progress, e.TotleTasks, e.CompletedTasks);

            parallelDownloaderResponse = await resourceDownloader.DownloadAsync();

            OnProgressChanged("Download Resources", 1);
        }

        return Task.Run<GameCoreInstallerResponse>(async () =>
        {
            await GetCoreJson();
            await DownloadResources();

            stopwatch.Stop();

            return new()
            {
                Success = true,
                GameCore = gameCore,
                Stopwatch = stopwatch,
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

    public static CoreManifest GetCoreManifest() => GetCoreManifestAsync().GetAwaiter().GetResult();

    public static async Task<CoreManifest> GetCoreManifestAsync()
    {
        using var res = await HttpWrapper.HttpGetAsync(DownloadApiManager.Current.VersionManifest);
        var entity = JsonConvert.DeserializeObject<CoreManifest>(await res.Content.ReadAsStringAsync());

        foreach (var core in entity.Cores)
            if (DownloadApiManager.Current.Host != DownloadApiManager.Mojang.Host)
                core.Url = core.Url
                    .Replace("https://piston-meta.mojang.com", DownloadApiManager.Current.Host)
                    .Replace("https://launchermeta.mojang.com", DownloadApiManager.Current.Host);

        return entity;
    }
}
