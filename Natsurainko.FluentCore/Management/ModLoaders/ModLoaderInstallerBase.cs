using Nrk.FluentCore.Launch;
using System;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Management.ModLoaders;

public abstract class ModLoaderInstallerBase : IModLoaderInstaller
{
    public string AbsoluteId { get; set; }

    public required GameInfo InheritedFrom { get; set; }

    public event EventHandler<double> ProgressChanged;

    public abstract Task<InstallResult> ExecuteAsync();

    protected void OnProgressChanged(double progress) => ProgressChanged?.Invoke(this, progress);
}
