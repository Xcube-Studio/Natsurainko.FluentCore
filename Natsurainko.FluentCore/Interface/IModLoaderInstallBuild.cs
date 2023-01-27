using Natsurainko.FluentCore.Model.Install;

namespace Natsurainko.FluentCore.Interface;

public interface IModLoaderInstallBuild
{
    ModLoaderType ModLoaderType { get; }

    string BuildVersion { get; }

    string DisplayVersion { get; }

    string McVersion { get; }
}
