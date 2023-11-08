using Nrk.FluentCore.Launch;
using System;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Management.ModLoaders;

public interface IInstallExecutor
{
    string AbsoluteId { get; }

    GameInfo InheritedFrom { get; }

    event EventHandler<double> ProgressChanged;

    Task<InstallResult> ExecuteAsync();
}
