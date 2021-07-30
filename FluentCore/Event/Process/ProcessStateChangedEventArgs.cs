using FluentCore.Model;
using System;

namespace FluentCore.Event.Process
{
    public class ProcessStateChangedEventArgs : EventArgs
    {
        public ProcessState ProcessState { get; set; }
    }
}
