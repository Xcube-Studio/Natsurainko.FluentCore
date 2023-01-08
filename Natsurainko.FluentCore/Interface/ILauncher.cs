using Natsurainko.FluentCore.Model.Launch;
using Natsurainko.FluentCore.Module.Launcher;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Interface;

public interface ILauncher
{
    LaunchSetting LaunchSetting { get; }

    ArgumentsBuilder ArgumentsBuilder { get; }

    IAuthenticator Authenticator { get; set; }

    IGameCoreLocator<IGameCore> GameCoreLocator { get; set; }

    IResourceDownloader ResourceDownloader { get; set; }

    Task<LaunchResponse> LaunchMinecraftAsync(IGameCore core);

    LaunchResponse LaunchMinecraft(IGameCore core);
}
