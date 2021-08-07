using FluentCore.Model;
using FluentCore.Model.Game;
using FluentCore.Model.Install.Forge;
using FluentCore.Service.Component.DependencesResolver;
using FluentCore.Service.Component.Launch;
using FluentCore.Service.Local;
using FluentCore.Service.Network;
using FluentCore.Service.Network.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FluentCore.Service.Component.Installer.ForgeInstaller
{
    public class LegacyForgeInstaller : InstallerBase
    {
        public string ForgeInstallerPackagePath { get; set; }

        public LegacyForgeInstaller(CoreLocator locator, string forgeInstallerPackagePath)
            : base(locator)
        {
            this.ForgeInstallerPackagePath = forgeInstallerPackagePath;
        }

        public ForgeInstallerResult Install() => InstallAsync().GetAwaiter().GetResult();

        public async Task<ForgeInstallerResult> InstallAsync()
        {
            using var archive = ZipFile.OpenRead(this.ForgeInstallerPackagePath);

            #region Get install_profile.json

            var forgeInstallProfile = await ZipFileHelper.GetObjectFromJsonEntryAsync<LegacyForgeInstallProfileModel>
                (archive.Entries.First(x => x.Name.Equals("install_profile.json", StringComparison.OrdinalIgnoreCase)));

            #endregion

            #region Get version.json

            var versionJsonFile = new FileInfo
                ($"{PathHelper.GetVersionFolder(this.CoreLocator.Root, forgeInstallProfile.Install.Target)}{PathHelper.X}{forgeInstallProfile.Install.Target}.json");
            if (!versionJsonFile.Directory.Exists)
                versionJsonFile.Directory.Create();

            File.WriteAllText(versionJsonFile.FullName, forgeInstallProfile.VersionInfo.ToString());

            #endregion

            #region Extract Forge Jar

            var legacyJarEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals(forgeInstallProfile.Install.FilePath, StringComparison.OrdinalIgnoreCase));
            var model = JsonConvert.DeserializeObject<CoreModel>(forgeInstallProfile.VersionInfo.ToString());

            var lib = model.Libraries.First(x => x.Name.StartsWith("net.minecraftforge:forge", StringComparison.OrdinalIgnoreCase));
            var fileLib = new FileInfo(Path.Combine(PathHelper.GetLibrariesFolder(this.CoreLocator.Root), lib.GetRelativePath()));
            await ZipFileHelper.WriteAsync(legacyJarEntry, fileLib.Directory.FullName, fileLib.Name);

            #endregion

            #region Download Libraries

            var api = SystemConfiguration.Api;
            if (api.Equals(new Mojang()))
                SystemConfiguration.Api = new Mcbbs();

            var downloadList = model.Libraries.Select(x => x.GetDownloadRequest(this.CoreLocator.Root)).ToList();
            downloadList.Remove(downloadList.First(x => x.Url.Equals(lib.GetDownloadRequest(this.CoreLocator.Root).Url)));

            var manyBlock = new TransformManyBlock<IEnumerable<HttpDownloadRequest>, HttpDownloadRequest>(x => x);
            var blockOptions = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = DependencesCompleter.MaxThread,
                MaxDegreeOfParallelism = DependencesCompleter.MaxThread
            };

            var actionBlock = new ActionBlock<HttpDownloadRequest>(async x =>
            {
                if (!x.Directory.Exists)
                    x.Directory.Create();

                var res = await HttpHelper.HttpDownloadAsync(x);
            }, blockOptions);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            _ = manyBlock.LinkTo(actionBlock, linkOptions);

            _ = manyBlock.Post(downloadList);
            manyBlock.Complete();

            await actionBlock.Completion;
            GC.Collect();

            SystemConfiguration.Api = api;

            #endregion

            return new ForgeInstallerResult
            {
                IsSuccessful = true,
                Message = $"Successfully Install {forgeInstallProfile.Install.Target}!"
            };
        }
    }
}
