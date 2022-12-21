using Natsurainko.FluentCore.Model.Launch;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Natsurainko.FluentCore.Event;

public class GameExitedArgs : EventArgs
{
    public int ExitCode { get; set; }

    public bool Crashed { get; set; }

    public Stopwatch RunTime { get; set; }

    public IEnumerable<GameProcessOutput> Outputs { get; set; }
}
