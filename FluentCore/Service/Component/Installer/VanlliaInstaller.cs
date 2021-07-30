using FluentCore.Service.Component.DependencesResolver;
using FluentCore.Service.Component.Launch;
using FluentCore.Service.Local;
using FluentCore.Service.Network;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace FluentCore.Service.Component.Installer
{
    public class VanlliaInstaller : InstallerBase
    {
        public VanlliaInstaller(CoreLocator locator) : base(locator)
        {

        }

        public async Task<bool> InstallAsync(string mcVersion)
        {
            foreach (var item in (await SystemConfiguration.Api.GetVersionManifest()).Versions)
            {
                if (item.Id == mcVersion)
                {
                    var directory = new DirectoryInfo(PathHelper.GetVersionFolder(this.CoreLocator.Root, mcVersion));

                    if (!directory.Exists)
                        directory.Create();

                    var res = await HttpHelper.HttpDownloadAsync(item.Url, directory.FullName);
                    if (res.HttpStatusCode != HttpStatusCode.OK)
                        return false;

                    await new DependencesCompleter(this.CoreLocator.GetGameCoreFromId(mcVersion)).CompleteAsync();

                    return true;
                }
            }

            return false;
        }
    }
}
