using FluentCore.Service.Local;
using System;

namespace FluentCore.Exceptions.Launcher
{
    public class GameHasRanException : Exception
    {
        public ProcessContainer ProcessContainer { get; set; }
    }
}
