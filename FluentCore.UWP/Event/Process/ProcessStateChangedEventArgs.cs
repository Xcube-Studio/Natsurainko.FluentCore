using FluentCore.UWP.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Event.Process
{
    public class ProcessStateChangedEventArgs : EventArgs
    {
        public ProcessState ProcessState { get; set; }
    }
}
