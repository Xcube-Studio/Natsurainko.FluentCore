using Nrk.FluentCore.Launch;
using System;
using System.Collections.ObjectModel;

namespace Nrk.FluentCore.Services.Launch;

public interface ILaunchService
{
    ReadOnlyCollection<MinecraftSession> Sessions { get; }

    event EventHandler<MinecraftSession>? SessionCreated;

    void LaunchGame(GameInfo gameInfo);

    MinecraftSession CreateMinecraftSessionFromGameInfo(GameInfo gameInfo);
}
