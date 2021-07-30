using FluentCore.Interface;
using FluentCore.Service.Component.Launch;

namespace FluentCore.Service.Component.Installer
{
    public class InstallerBase : InterfaceInstaller
    {
        public CoreLocator CoreLocator { get; set; }

        public InstallerBase(CoreLocator locator) => this.CoreLocator = locator;
    }
}
