using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Service.Local
{
    public class SystemConfiguration
    {
        public static string Arch
        {
            get
            {
                if (Environment.Is64BitOperatingSystem)
                    return "64";
                else return "32";
            }
        }

        public static OSPlatform Platform
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return OSPlatform.OSX;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return OSPlatform.Linux;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return OSPlatform.Windows;
                return OSPlatform.Create("Unknown");
            }
        }

        public static string PlatformName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "OSX";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "Linux";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "Windows";
                return "Unknown";
            }
        }

    }
}
