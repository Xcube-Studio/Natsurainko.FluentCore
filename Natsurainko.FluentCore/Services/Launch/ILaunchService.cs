using Nrk.FluentCore.Authentication;
using Nrk.FluentCore.Launch;
using System;
using System.Collections.ObjectModel;

namespace Nrk.FluentCore.Services.Launch;

public interface ILaunchService
{
    ReadOnlyCollection<MinecraftSession> Sessions { get; }

    event EventHandler<MinecraftSession>? SessionCreated;

    void LaunchGame(GameInfo gameInfo, Account account);

    MinecraftSession CreateMinecraftSessionFromGameInfo(GameInfo gameInfo, Account account);
}
