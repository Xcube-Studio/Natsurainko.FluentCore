using FluentCore.Model.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Model.Launch
{
    public class GameCore
    {
        public string Root { get; set; }

        public IEnumerable<Library> Libraries { get; set; }
        
        public IEnumerable<Native> Natives { get; set; }

        public string MainClass { get; set; }

        public IEnumerable<string> FrontArguments { get; set; }

        public IEnumerable<string> BehindArguments { get; set; }
        
        public string Id { get; set; }
    }
}
