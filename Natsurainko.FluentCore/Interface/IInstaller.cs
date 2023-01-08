using Natsurainko.FluentCore.Model.Install;
using System;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Interface;

public interface IInstaller
{
    event EventHandler<(string, float)> ProgressChanged;

    IGameCoreLocator<IGameCore> GameCoreLocator { get; }

    Task<InstallerResponse> InstallAsync();

    InstallerResponse Install();
}
