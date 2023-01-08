using Natsurainko.FluentCore.Model.Launch;
using System.Collections.Generic;

namespace Natsurainko.FluentCore.Interface;

public interface IArgumentsBuilder
{
    IGameCore GameCore { get; }

    LaunchSetting LaunchSetting { get; }

    IEnumerable<string> Build();
}
