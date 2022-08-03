using Natsurainko.FluentCore.Class.Model.Auth;
using Natsurainko.Toolkits.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Natsurainko.FluentCore.Class.Model.Launch
{
    public class LaunchSetting : IJsonEntity
    {
        public Account Account { get; set; }

        [JsonIgnore]
        public DirectoryInfo NativesFolder { get; set; }

        public JvmSetting JvmSetting { get; set; }

        public GameWindowSetting GameWindowSetting { get; set; } = new GameWindowSetting();

        public ServerSetting ServerSetting { get; set; }

        public XmlOutputSetting XmlOutputSetting { get; set; } = new XmlOutputSetting();

        public bool IsDemoUser { get; set; } = false;

        public LaunchSetting() { }

        public LaunchSetting(JvmSetting jvmSetting)
        {
            this.JvmSetting = jvmSetting;
        }
    }
}
