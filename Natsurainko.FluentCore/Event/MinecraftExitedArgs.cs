using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Natsurainko.FluentCore.Event
{
    public class MinecraftExitedArgs
    {
        public int ExitCode { get; set; }

        public bool Crashed { get; set; }

        public Stopwatch RunTime { get; set; }

        public IEnumerable<string> Outputs { get; set; }
    }
}
