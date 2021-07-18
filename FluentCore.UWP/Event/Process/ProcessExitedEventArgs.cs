using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Event.Process
{
    public class ProcessExitedEventArgs : EventArgs
    {
        public TimeSpan RunTime { get; set; }

        public int ExitCode { get; set; }

        public bool IsNormal { get; set; }
    }
}
