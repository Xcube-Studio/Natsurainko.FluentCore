using FluentCore.Interface;
using FluentCore.Model;
using FluentCore.Model.Launch;
using FluentCore.Service.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FluentCore.Service.Component.DependencesResolver
{
    public class DependencesCompleter
    {
        public GameCore GameCore { get; set; }

        public int MaxThread { get; set; } = 64;

        public DependencesCompleter(GameCore core) => this.GameCore = core;

        public List<HttpDownloadResponse> ErrorDownloadResponses = new List<HttpDownloadResponse>();

        public async Task CompleteAsync()
        {
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

            }, blockOptions);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            manyBlock.LinkTo(actionBlock, linkOptions);

            manyBlock.Post(await GetRequestsAsync());
            manyBlock.Complete();

            await actionBlock.Completion;
            GC.Collect();
        }

        public async Task<IEnumerable<HttpDownloadRequest>> GetRequestsAsync()
        {
            var dependences = await new AssetsResolver(this.GameCore).GetLostDependencesAsync();
            dependences.Union(new LibrariesResolver(this.GameCore).GetLostDependences());

            var requests = new List<HttpDownloadRequest>();
            foreach (IDependence dependence in dependences)
                requests.Add(dependence.GetDownloadRequest(this.GameCore.Root));

            return requests;
        }
    }
}
