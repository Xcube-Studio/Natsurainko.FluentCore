using FluentCore.Extend.Service.Local;
using FluentCore.Model.Auth;
using FluentCore.Model.Launch;
using FluentCore.Service.Component.Launch;
using FluentCore.Service.Local;
using FluentCore.Service.Network;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FluentCore.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            CoreLocator coreLocator = new CoreLocator("D:\\Minecraft\\.minecraft");

            string id = System.Console.ReadLine();
            GameCore core = coreLocator.GetGameCoreFromId(id);
            LaunchConfig launchConfig = new LaunchConfig
            {
                MoreBehindArgs = string.Empty,
                MoreFrontArgs = string.Empty,
                JavaPath = "D:\\Java\\jdk1.8.0_291\\bin\\java.exe",
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
            */

            var file = HttpHelper.HttpDownloadAsync("https://bmclapi.bangbang93.com/java/jre_x64.exe", "C:\\Users\\Admin\\Desktop").GetAwaiter().GetResult();

            System.Console.Read();
        }
    }
}
