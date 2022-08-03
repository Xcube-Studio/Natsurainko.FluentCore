using Natsurainko.FluentCore.Class.Model.Download;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Natsurainko.FluentCore.Class.Model.Launch
{
    public class XmlOutputSetting
    {
        public bool Enable { get; set; } = false;

        public FileInfo LogConfigFile { get; set; }
    }
}
