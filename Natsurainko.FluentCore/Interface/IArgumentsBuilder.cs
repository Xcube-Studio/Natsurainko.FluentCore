using Natsurainko.FluentCore.Class.Model.Launch;
using System;
using System.Collections.Generic;
using System.Text;

namespace Natsurainko.FluentCore.Interface
{
    public interface IArgumentsBuilder
    {
        GameCore GameCore { get; }

        LaunchSetting LaunchSetting { get; }

        IEnumerable<string> Build();
    }
}
