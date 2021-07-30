using System;
using System.Collections.Generic;

namespace FluentCore.Event.Process
{
    public class ProcessCrashedEventArgs : EventArgs
    {
        public IEnumerable<string> CrashData { get; set; }
    }
}
