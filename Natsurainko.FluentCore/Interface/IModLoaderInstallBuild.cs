using Natsurainko.FluentCore.Model.Install;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Interface;

public interface IModLoaderInstallBuild
{
    ModLoaderType ModLoaderType { get; }

    string BuildVersion { get; }

    string DisplayVersion { get; }

    string McVersion { get; }
}
