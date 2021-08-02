using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Model.Install.Forge
{
    public class ForgeInstallDataModel
    {
        [JsonProperty("client")]
        public string Client { get; set; }

        [JsonProperty("server")]
        public string Server { get; set; }
    }
}
