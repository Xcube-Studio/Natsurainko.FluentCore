using System;
using System.Collections.Generic;
using System.Text;

namespace Natsurainko.FluentCore.Service
{
    public class DownloadApi
    {
        public string Host { get; set; }

        public string VersionManifest { get; set; }

        public string Assets { get; set; }

        public string Libraries { get; set; }
    }

    public static class DownloadApiExtension
    {
        
    }
}
