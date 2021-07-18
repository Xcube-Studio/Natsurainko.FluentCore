using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Service.Network.Api
{
    public class Mcbbs : BaseApi
    {
        public Mcbbs()
        {
            Url = "https://download.mcbbs.net";
            VersionManifest = $"{Url}/mc/game/version_manifest.json";
            Assets = $"{Url}/assets";
            Libraries = $"{Url}/maven";
        }
    }
}
