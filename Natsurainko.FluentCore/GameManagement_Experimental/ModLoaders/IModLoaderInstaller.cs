using Nrk.FluentCore.Management;
using System;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.ModLoaders;

public interface IModLoaderInstaller
{
    string AbsoluteId { get; }

    MinecraftInstance InheritedFrom { get; }

    event EventHandler<double> ProgressChanged;

    Task<InstallResult> ExecuteAsync();
}
