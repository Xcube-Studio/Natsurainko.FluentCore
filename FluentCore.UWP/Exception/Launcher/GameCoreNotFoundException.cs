using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Exceptions.Launcher
{
    public class GameCoreNotFoundException : Exception
    {
        public string Id { get; set; }
    }
}
