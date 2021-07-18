using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Service.Network.Api
{
    public class Bmclapi : BaseApi
    {
        public Bmclapi()
        {
            Url = "https://bmclapi2.bangbang93.com";
            VersionManifest = $"{Url}/mc/game/version_manifest.json";
            Assets = $"{Url}/assets";
            Libraries = $"{Url}/maven";
        }
    }
}
