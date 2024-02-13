using Nrk.FluentCore.Launch;
using Nrk.FluentCore.Services.Accounts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Nrk.FluentCore.Services.Launch;

public class DefaultLaunchService : ILaunchService
{
    protected readonly IFluentCoreSettingsService _settingsService;
    protected readonly IAccountService _accountService;
    protected readonly IGameService _gameService;

    protected readonly List<MinecraftSession> _sessions;
    public ReadOnlyCollection<MinecraftSession> Sessions { get; }

    public event EventHandler<MinecraftSession>? SessionCreated;

    public DefaultLaunchService(
        IFluentCoreSettingsService settingsService,
        IAccountService accountService,
        IGameService gameService)
    {
        _settingsService = settingsService;
        _accountService = accountService;
        _gameService = gameService;

        _sessions = [];
        Sessions = new(_sessions);
    }

    public virtual async void LaunchGame(GameInfo gameInfo)
    {
        var session = CreateMinecraftSessionFromGameInfo(gameInfo);
        _sessions.Add(session);
        SessionCreated?.Invoke(this, session);

        try
        {
            await session.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public virtual MinecraftSession CreateMinecraftSessionFromGameInfo(GameInfo gameInfo)
    {
        if (_accountService.ActiveAccount == null)
            throw new InvalidOperationException();

        return new MinecraftSession() // Launch session
        {
            Account = _accountService.ActiveAccount,
            GameInfo = gameInfo,
            GameDirectory = gameInfo.MinecraftFolderPath,
            JavaPath = _settingsService.ActiveJava,
            MaxMemory = _settingsService.JavaMemory,
            MinMemory = _settingsService.JavaMemory,
            UseDemoUser = _settingsService.EnableDemoUser
        };
    }

    protected virtual void OnSessionCreated(MinecraftSession minecraftSession)
        => this.SessionCreated?.Invoke(this, minecraftSession);
}
