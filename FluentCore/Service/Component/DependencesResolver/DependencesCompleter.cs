using FluentCore.Interface;
using FluentCore.Model;
using FluentCore.Model.Launch;
using FluentCore.Service.Local;
using FluentCore.Service.Network;
using FluentCore.Service.Network.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FluentCore.Service.Component.DependencesResolver
{
    public class DependencesCompleter
    {
        public GameCore GameCore { get; set; }

        public static int MaxThread { get; set; } = 64;

        public DependencesCompleter(GameCore core) => this.GameCore = core;

        public List<HttpDownloadResponse> ErrorDownloadResponses = new List<HttpDownloadResponse>();

        public event EventHandler<HttpDownloadResponse> SingleDownloadDoneEvent;

        public async Task CompleteAsync()
        {
            var mainJarRequest = GetMainJarDownloadRequest();
            if (mainJarRequest != null)
            {
                var res = await HttpHelper.HttpDownloadAsync(mainJarRequest);
                File.Move(res.FileInfo.FullName, this.GameCore.MainJar);
            }

            var manyBlock = new TransformManyBlock<IEnumerable<HttpDownloadRequest>, HttpDownloadRequest>(x => x);
            var blockOptions = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = MaxThread,
                MaxDegreeOfParallelism = MaxThread
            };

            var actionBlock = new ActionBlock<HttpDownloadRequest>(async x =>
            {
                if (!x.Directory.Exists)
                    x.Directory.Create();

                var res = await HttpHelper.HttpDownloadAsync(x);
                if (res.HttpStatusCode != HttpStatusCode.OK)
                    this.ErrorDownloadResponses.Add(res);

                SingleDownloadDoneEvent?.Invoke(this, res);
            }, blockOptions);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            _ = manyBlock.LinkTo(actionBlock, linkOptions);

            _ = manyBlock.Post(await GetRequestsAsync());
            manyBlock.Complete();

            await actionBlock.Completion;
            GC.Collect();
        }

        public async Task<IEnumerable<HttpDownloadRequest>> GetRequestsAsync()
        {
            var dependences = await new AssetsResolver(this.GameCore).GetLostDependencesAsync();
            dependences = dependences.Union(new LibrariesResolver(this.GameCore).GetLostDependences());

            var requests = new List<HttpDownloadRequest>();

            foreach (IDependence dependence in dependences)
                requests.Add(dependence.GetDownloadRequest(this.GameCore.Root));

            return requests;
        }

        public HttpDownloadRequest GetMainJarDownloadRequest()
        {
            var file = new FileInfo(this.GameCore.MainJar);
            var model = this.GameCore.Downloads["client"];

            if (!file.Exists)
            {
                return new HttpDownloadRequest
                {
                    Directory = file.Directory,
                    Url = SystemConfiguration.Api != new Mojang() ? model.Url.Replace("https://launcher.mojang.com", SystemConfiguration.Api.Url) : model.Url,
                    Sha1 = model.Sha1,
                    Size = model.Size
                };
            }

            return null;
        }
    }
}
