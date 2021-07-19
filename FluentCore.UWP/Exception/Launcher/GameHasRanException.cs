using FluentCore.UWP.Service.Local;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Exceptions.Launcher
{
    public class GameHasRanException : Exception
    {
        public ProcessContainer ProcessContainer { get; set; }
    }
}
