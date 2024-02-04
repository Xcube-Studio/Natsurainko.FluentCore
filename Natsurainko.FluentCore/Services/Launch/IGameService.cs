using Nrk.FluentCore.Launch;
using System.Collections.ObjectModel;

namespace Nrk.FluentCore.Services.Launch;

public interface IGameService
{
    public GameInfo? ActiveGame { get; }

    public ReadOnlyObservableCollection<GameInfo> Games { get; }

    public string? ActiveMinecraftFolder { get; }

    public ReadOnlyObservableCollection<string> MinecraftFolders { get; }

    void ActivateMinecraftFolder(string? folder);

    void ActivateGame(GameInfo? gameInfo);

    void RefreshGames();
}
