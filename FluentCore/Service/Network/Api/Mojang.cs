using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Service.Network.Api
{
    public class Mojang : BaseApi
    {
        public Mojang()
        {
            Url = string.Empty;
            VersionManifest = $"http://launchermeta.mojang.com/mc/game/version_manifest.json";
            Assets = $"http://resources.download.minecraft.net/assets";
            Libraries = $"https://libraries.minecraft.net//maven";
        }
    }
}
