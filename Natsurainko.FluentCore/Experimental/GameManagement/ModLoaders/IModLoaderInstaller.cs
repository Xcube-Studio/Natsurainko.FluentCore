using Nrk.FluentCore.GameManagement;
using Nrk.FluentCore.Management;
using System;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.ModLoaders;

public interface IModLoaderInstaller
{
    string AbsoluteId { get; }

    MinecraftInstance InheritedFrom { get; }

    event EventHandler<double> ProgressChanged;

    Task<InstallResult> ExecuteAsync();
}
