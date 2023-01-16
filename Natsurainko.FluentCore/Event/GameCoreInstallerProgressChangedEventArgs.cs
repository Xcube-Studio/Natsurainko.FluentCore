using Natsurainko.FluentCore.Model.Install;
using System.Collections.Generic;

namespace Natsurainko.FluentCore.Event;

public class GameCoreInstallerProgressChangedEventArgs
{
    public Dictionary<string, GameCoreInstallerStepProgress> StepsProgress { get; internal set; }

    public double TotleProgress { get; internal set; }
}
