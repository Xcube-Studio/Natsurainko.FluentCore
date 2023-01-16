using Natsurainko.FluentCore.Event;
using Natsurainko.FluentCore.Model.Install;
using System;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Interface;

public interface IGameCoreInstaller
{
    event EventHandler<GameCoreInstallerProgressChangedEventArgs> ProgressChanged;

    IGameCoreLocator<IGameCore> GameCoreLocator { get; }

    Task<GameCoreInstallerResponse> InstallAsync();

    GameCoreInstallerResponse Install();
}
