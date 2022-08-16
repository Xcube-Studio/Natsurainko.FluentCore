using System.IO;

namespace Natsurainko.FluentCore.Class.Model.Launch
{
    public class XmlOutputSetting
    {
        public bool Enable { get; set; } = false;

        public FileInfo LogConfigFile { get; set; }
    }
}
