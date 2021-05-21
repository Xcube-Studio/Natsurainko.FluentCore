using FluentCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Event.Process
{
    public class ProcessStateChangedEventArgs : EventArgs
    {
        public ProcessState ProcessState { get; set; }
    }
}
