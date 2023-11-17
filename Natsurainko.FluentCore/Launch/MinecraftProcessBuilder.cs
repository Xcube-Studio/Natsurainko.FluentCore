using Nrk.FluentCore.Launch;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Nrk.FluentCore.Launch;

public class MinecraftProcessBuilder
{
    protected readonly GameInfo _gameInfo;

    public MinecraftProcessBuilder(GameInfo gameInfo)
    {
        _gameInfo = gameInfo;
    }

    public MinecraftProcess Build()
    {
        return null!;
    }
}
