using FluentCore.Extend.Service.Local;
using FluentCore.Model;
using FluentCore.Model.Auth;
using FluentCore.Model.Game;
using FluentCore.Model.Launch;
using FluentCore.Service.Component.DependencesResolver;
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
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FluentCore.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpHelper.SetTimeout(60000);
            System.Console.Write("Minecraft Path:");
            CoreLocator coreLocator = new CoreLocator(System.Console.ReadLine());
            System.Console.Write("Java Path:");
            string javaPath = System.Console.ReadLine();
            System.Console.Write("Version:");
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

            var completer = new DependencesCompleter(core);
            completer.CompleteAsync().Wait();

            System.Console.ReadLine();
        }
    }
}
