using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Event.Process
{
    public class ProcessCrashedEventArgs : EventArgs
    {
        public IEnumerable<string> CrashData { get; set; }
    }
}
