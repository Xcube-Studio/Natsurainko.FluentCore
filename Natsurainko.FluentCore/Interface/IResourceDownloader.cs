using Natsurainko.FluentCore.Class.Model.Download;
using Natsurainko.FluentCore.Class.Model.Launch;
using Natsurainko.Toolkits.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Interface
{
    public interface IResourceDownloader
    {
        GameCore GameCore { get; set; }

        Action<float> DownloadProgressChangedAction { get; }


        event EventHandler<HttpDownloadResponse> ItemDownloaded;

        Task<ResourceDownloadResponse> DownloadAsync(Action<float> func);

        Task<ResourceDownloadResponse> DownloadAsync();
    }
}
