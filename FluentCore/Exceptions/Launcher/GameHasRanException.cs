using FluentCore.Service.Local;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Exceptions.Launcher
{
    public class GameHasRanException : Exception
    {
        public ProcessContainer ProcessContainer { get; set; }
    }
}
