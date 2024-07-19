using Nrk.FluentCore.Experimental.GameManagement.Instances;
using System;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.ModLoaders;

public interface IModLoaderInstaller
{
    string AbsoluteId { get; }

    MinecraftInstance InheritedInstance { get; }

    event EventHandler<double> ProgressChanged;

    Task<InstallationResult> ExecuteAsync();
}
