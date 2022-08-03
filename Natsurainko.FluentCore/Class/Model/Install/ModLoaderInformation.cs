using System;
using System.Collections.Generic;
using System.Text;

namespace Natsurainko.FluentCore.Class.Model.Install
{
    public class ModLoaderInformation
    {
        public ModLoaderType LoaderType { get; set; }

        public string Version { get; set; }

        public enum ModLoaderType
        {
            Forge = 0,
            Fabric = 1,
            OptiFine = 2,
            Unknown = 3
        }
    }
}
