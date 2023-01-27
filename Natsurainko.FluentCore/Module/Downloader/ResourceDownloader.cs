using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Parser;
using Natsurainko.FluentCore.Module.Parser;
using Natsurainko.Toolkits.IO;
using Natsurainko.Toolkits.Network.Downloader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Module.Downloader;

public class ResourceDownloader : IResourceDownloader
{
    public IGameCore GameCore { get; set; }

    public static int MaxDownloadThreads { get; set; } = 128;

    public ResourceDownloader() { }

    public ResourceDownloader(IGameCore core)
    {
        GameCore = core;
    }

    public event EventHandler<ParallelDownloaderProgressChangedEventArgs> DownloadProgressChanged;

    public ParallelDownloaderResponse Download() => DownloadAsync().GetAwaiter().GetResult();

    public async Task<ParallelDownloaderResponse> DownloadAsync()
    {
        var resources = new List<IResource>();

        resources.AddRange(GameCore.LibraryResources.AsParallel().Where(x => x.IsEnable));
        resources.AddRange(GetFileResources());
        resources.AddRange(await GetAssetResourcesAsync());

        resources = resources.AsParallel().Where(x =>
        {
            if (string.IsNullOrEmpty(x.CheckSum) && x.Size == 0)
                return false;
            if (x.ToFileInfo().Verify(x.CheckSum) && x.ToFileInfo().Verify(x.Size))
                return false;

            return true;
        }).ToList();

        using var downloader = new ParallelDownloader<IResource>(
            resources, x => x.ToDownloadRequest(),
            true, MaxDownloadThreads);

        downloader.DownloadProgressChanged += DownloadProgressChanged;
        downloader.BeginDownload();

        return await downloader.CompleteAsync();
    }

    public IEnumerable<IResource> GetFileResources()
    {
        if (GameCore.ClientFile != null)
            yield return GameCore.ClientFile;
    }

    public async Task<List<IResource>> GetAssetResourcesAsync()
    {
        if (!(GameCore.AssetIndexFile.FileInfo.Verify(GameCore.AssetIndexFile.Size)
            || GameCore.AssetIndexFile.FileInfo.Verify(GameCore.AssetIndexFile.CheckSum)))
        {
            var request = GameCore.AssetIndexFile.ToDownloadRequest();

            if (!request.Directory.Exists)
                request.Directory.Create();

            var res = await SimpleDownloader.StartNewDownloadAsync(request);

            if (!res.Success)
                return new();
        }

        var entity = JsonConvert.DeserializeObject<AssetManifestJsonEntity>
            (File.ReadAllText(GameCore.AssetIndexFile.ToFileInfo().FullName));

        return new AssetParser(entity, GameCore.Root).GetAssets().Select(x => (IResource)x).ToList();
    }
}
