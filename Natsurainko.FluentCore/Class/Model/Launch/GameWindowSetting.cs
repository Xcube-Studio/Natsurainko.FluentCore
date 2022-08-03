using System;
using System.Collections.Generic;
using System.Text;

namespace Natsurainko.FluentCore.Class.Model.Launch
{
    public class GameWindowSetting
    {
        public int Width { get; set; } = 854;

        public int Height { get; set; } = 480;

        public bool IsFullscreen { get; set; } = false;
    }
}
