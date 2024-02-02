using Nrk.FluentCore.Launch;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Nrk.FluentCore.Services.Launch;

public class DefaultGameService : IGameService
{
    #region Games 

    protected GameInfo? _activeGame;
    public GameInfo? ActiveGame
    {
        get => _activeGame;
        protected set
        {
            if (_activeGame != value)
                WhenActiveGameChanged(_activeGame, value);

            _activeGame = value;
        }
    }

    protected ObservableCollection<GameInfo> _games;
    public ReadOnlyObservableCollection<GameInfo> Games { get; }

    #endregion

    #region Folders

    public string? _activeMinecraftFolder;
    public string? ActiveMinecraftFolder
    {
        get => _activeMinecraftFolder;
        protected set
        {
            if (_activeMinecraftFolder != value)
                WhenActiveMinecraftFolderChanged(_activeMinecraftFolder, value);

            _activeMinecraftFolder = value;
        }
    }

    protected ObservableCollection<string> _minecraftFolders;
    public ReadOnlyObservableCollection<string> MinecraftFolders { get; }

    #endregion

    protected DefaultGameLocator? _locator;
    protected IFluentCoreSettingsService _settingsService;

    public DefaultGameService(IFluentCoreSettingsService settingsService)
    {
        _settingsService = settingsService;

        _minecraftFolders = settingsService.MinecraftFolders ?? [];
        _games= [];

        Games = new(_games);
        MinecraftFolders = new(_minecraftFolders);

        ActivateMinecraftFolder(
            !string.IsNullOrEmpty(_settingsService.ActiveMinecraftFolder) 
            && MinecraftFolders.Contains(_settingsService.ActiveMinecraftFolder) 
            ? _settingsService.ActiveMinecraftFolder
            : null);
    }

    public virtual void WhenActiveGameChanged(GameInfo? oldGame, GameInfo? newGame) 
    {
        _settingsService.ActiveGameInfo = newGame;
    }

    public virtual void WhenActiveMinecraftFolderChanged(string? oldFolder, string? newFolder) 
    {
        _settingsService.ActiveMinecraftFolder = newFolder;

        if (newFolder == null)
        {
            _games.Clear();
            return;
        }

        _locator = new DefaultGameLocator(newFolder);

        //_versionsFolderWatcher?.Dispose(); 
        //_versionsFolderWatcher = new FileSystemWatcher(ActiveMinecraftFolder);

        RefreshGames();
    }

    public virtual void ActivateMinecraftFolder(string? folder)
    {
        if (folder != null && !_minecraftFolders.Contains(folder))
            throw new ArgumentException("Not an folder managed by GameService", nameof(folder));

        ActiveMinecraftFolder = folder;
    }

    public virtual void ActivateGame(GameInfo? gameInfo)
    {
        if (gameInfo != null && !_games.Contains(gameInfo))
            throw new ArgumentException("Not an game managed by GameService", nameof(gameInfo));

        ActiveGame = gameInfo;
    }

    public virtual void RefreshGames()
    {
        _games.Clear();

        if (_locator == null)
            throw new InvalidOperationException();

        foreach (var game in _locator.EnumerateGames())
            _games.Add(game);

        GameInfo? gameInfo = _settingsService.ActiveGameInfo != null && _games.Contains(_settingsService.ActiveGameInfo) 
            ? _settingsService.ActiveGameInfo
            : _games.Count > 0 ? _games[0] : null;

        ActivateGame(gameInfo);
    }

    public virtual void AddMinecraftFolder(string folder)
    {
        _minecraftFolders.Add(folder);
        ActivateMinecraftFolder(folder);
    }

    public void RemoveMinecraftFolder(string folder)
    {
        _minecraftFolders.Remove(folder);

        if (ActiveMinecraftFolder == folder)
            ActivateMinecraftFolder(MinecraftFolders.Count > 0 ? MinecraftFolders[0] : null);
    }
}
