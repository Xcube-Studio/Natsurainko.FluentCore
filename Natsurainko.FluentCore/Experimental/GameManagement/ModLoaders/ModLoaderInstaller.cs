using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Experimental.GameManagement.Instances;
using System;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.ModLoaders;

public abstract class ModLoaderInstaller : IModLoaderInstaller
{
    #region IModLoaderInstaller Members

    public required string AbsoluteId { get; set; }

    public required MinecraftInstance InheritedInstance { get; set; }

    public event EventHandler<double>? ProgressChanged;

    public abstract Task<InstallationResult> ExecuteAsync();

    #endregion

    protected void OnProgressChanged(double progress) => ProgressChanged?.Invoke(this, progress);
}
