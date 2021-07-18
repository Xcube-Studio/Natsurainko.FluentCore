using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Service.Network.Api
{
    public abstract class BaseApi
    {
        public string Url { get; set; }

        public string VersionManifest;

        public string Assets;

        public string Libraries;
    }
}
