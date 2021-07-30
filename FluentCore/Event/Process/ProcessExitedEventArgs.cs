using System;

namespace FluentCore.Event.Process
{
    public class ProcessExitedEventArgs : EventArgs
    {
        public TimeSpan RunTime { get; set; }

        public int ExitCode { get; set; }

        public bool IsNormal { get; set; }
    }
}
