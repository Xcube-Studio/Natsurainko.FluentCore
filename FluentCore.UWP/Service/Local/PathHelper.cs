using FluentCore.UWP.Model.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentCore.UWP.Service.Local
{
    public class PathHelper
    {
        public static string GetVersionsFolder(string root) => $"{root}\\versions";

        public static string GetVersionFolder(string root, string id) => $"{root}\\versions\\{id}";

        public static string GetLibrariesFolder(string root) => $"{root}\\libraries";

        public static string GetAssetsFolder(string root) => $"{root}\\assets";

        public static string GetAssetIndexFolder(string root) => $"{root}\\assets\\indexes";

        public static string GetLogConfigsFolder(string root) => $"{GetAssetsFolder(root)}\\log_configs";
    }
}
