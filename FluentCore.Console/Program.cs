using FluentCore.Extend.Service.Local;
using FluentCore.Model;
using FluentCore.Model.Auth;
using FluentCore.Model.Game;
using FluentCore.Model.Launch;
using FluentCore.Service.Component.Launch;
using FluentCore.Service.Local;
using FluentCore.Service.Network;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FluentCore.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.Write("Minecraft Path:");
            CoreLocator coreLocator = new CoreLocator(System.Console.ReadLine());
            System.Console.Write("Java Path:");
            string javaPath = System.Console.ReadLine();

            string id = System.Console.ReadLine();
            GameCore core = coreLocator.GetGameCoreFromId(id);

            #region
            /*
            LaunchConfig launchConfig = new LaunchConfig
            {
                MoreBehindArgs = string.Empty,
                MoreFrontArgs = string.Empty,
                JavaPath = javaPath,
                MaximumMemory = 2048,
                NativesFolder = $"{PathHelper.GetVersionFolder(coreLocator.Root, id)}{PathHelper.X}natives",
                AuthDataModel = new AuthDataModel
                {
                    AccessToken = "8888-8888-8888-8888",
                    UserName = "steve",
                    Uuid = Guid.NewGuid()
                }
            };
            ArgumentsBuilder argumentsBuilder = new ArgumentsBuilder(core, launchConfig);

            System.Console.WriteLine(argumentsBuilder.BulidArguments(true));
            System.Console.ReadLine();
            foreach(Native native in core.Natives)
            {
                FileInfo file = new FileInfo(Path.Combine(PathHelper.GetLibrariesFolder(core.Root), native.GetRelativePath()));
                System.Console.WriteLine(file.FullName);
                HttpDownloadResponse response = HttpHelper.HttpDownloadAsync(native.Downloads.Classifiers[$"natives-{SystemConfiguration.PlatformName.ToLower()}"].Url, file.Directory.FullName).GetAwaiter().GetResult();
                System.Console.WriteLine($"[{response.FileInfo.Name}]{response.HttpStatusCode}");
            }

            NativesDecompressor nativesDecompressor = new NativesDecompressor(core.Root, id);
            nativesDecompressor.Decompress(core.Natives);
            */
            #endregion

            //var file = HttpHelper.HttpDownloadAsync(core.Libraries.ToList()[0].Downloads.Artifact.Url, "C:\\Users\\Admin\\Desktop").GetAwaiter().GetResult();
            string json = File.ReadAllText($"{core.Root}\\assets\\indexes\\{core.AsstesIndex.Id}.json");
            var assets = JsonConvert.DeserializeObject<Assets>(json);
            var items = new List<DownloadItem>();

            foreach(var keys in assets.Objects)
            {
                items.Add(new DownloadItem
                {
                    url = $"https://bmclapi2.bangbang93.com/assets/{keys.Value.Hash.Substring(0,2)}/{keys.Value.Hash}"
                });
            }
            List<HttpDownloadResponse> responses = new List<HttpDownloadResponse>();

            Parallel.ForEach(items, new ParallelOptions { MaxDegreeOfParallelism = -1 }, async x => 
            {
                responses.Add(await HttpHelper.HttpDownloadAsync(x.url, "C:\\Users\\Admin\\Desktop\\新建文件夹"));
                System.Console.WriteLine($"Download Done [{x.url}]");
            });

            //var tasks = new SimpleDownloadTaskQueue(items);
            //tasks.Root = "C:\\Users\\Admin\\Desktop\\新建文件夹";

            //tasks.Start();

            //tasks.Wait().Wait();
            System.Console.Read();
            System.Console.Read();
        }

        class SimpleDownloadTaskQueue
        {
            public SimpleDownloadTaskQueue(IEnumerable<DownloadItem> items) => this.Items = items;

            public string Root { get; set; }

            public IEnumerable<DownloadItem> Items { get; set; }

            public List<HttpDownloadResponse> Responses = new List<HttpDownloadResponse>();

            public List<Task> Tasks = new List<Task>();

            public int RanId = 0;

            public int RuningTask { get; private set; } = 0;

            public int MaxRunningTask { get; set; } = 16;

            public void Start()
            {
                foreach(DownloadItem item in Items)
                {
                    Tasks.Add(new Task(async delegate
                    {
                        RuningTask += 1;

                        if (RuningTask < MaxRunningTask)
                        { Tasks[RanId].Start(); RanId += 1; }

                        Responses.Add(await HttpHelper.HttpDownloadAsync(item.url, Root));

                        RuningTask -= 1;

                        if (RuningTask < MaxRunningTask)
                        { Tasks[RanId].Start(); RanId += 1; }
                    })
                    );
                }

                Tasks[0].Start();
                RanId += 1;
            }

            //public Task Wait() => Task.Run(delegate { Task.WaitAll(Tasks.ToArray()); });

            /*private Task GetTask()
            {
                foreach (Task task in Tasks)
                    if (task.Status == TaskStatus.Created)
                        return task;

                return null;
            }*/
        }

        class DownloadItem
        {
            public string url { get; set; }
        }

        class Asset
        {
            [JsonProperty("hash")]
            public string Hash { get; set; }

            [JsonProperty("size")]
            public int  Size { get; set; }
        }

        class Assets
        {
            [JsonProperty("objects")]
            public Dictionary<string, Asset> Objects { get; set; }
        }
    }
}
