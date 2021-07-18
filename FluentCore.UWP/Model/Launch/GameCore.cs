using FluentCore.UWP.Model.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Model.Launch
{
    public class GameCore
    {
        public AssetIndex AsstesIndex { get; set; }

        //public Logging Logging { get; set; }

        public Dictionary<string, FileModel> Downloads { get; set; }

        public string Root { get; set; }

        public IEnumerable<Library> Libraries { get; set; }
        
        public IEnumerable<Native> Natives { get; set; }

        public string MainClass { get; set; }

        public string MainJar { get; set; }

        public string FrontArguments { get; set; }

        public string BehindArguments { get; set; }
        
        public string Id { get; set; }

        public string Type { get; set; }
    }
}
