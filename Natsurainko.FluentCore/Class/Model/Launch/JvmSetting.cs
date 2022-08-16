using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Natsurainko.FluentCore.Class.Model.Launch
{
    public class JvmSetting
    {
        [JsonIgnore]
        public FileInfo Javaw { get; set; }

        public int MaxMemory { get; set; } = 2048;

        public int MinMemory { get; set; } = 512;

        public IEnumerable<string> AdvancedArguments { get; set; }

        public IEnumerable<string> GCArguments { get; set; }

        public JvmSetting() { }

        public JvmSetting(string file) => this.Javaw = new FileInfo(file);

        public JvmSetting(FileInfo fileInfo) => this.Javaw = fileInfo;
    }
}
