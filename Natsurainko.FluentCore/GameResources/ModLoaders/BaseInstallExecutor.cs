using Nrk.FluentCore.Classes.Datas.Install;
using Nrk.FluentCore.Launch;
using System;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameResources.ModLoaders;

public abstract class BaseInstallExecutor : IInstallExecutor
{
    public string AbsoluteId { get; set; }

    public required GameInfo InheritedFrom { get; set; }

    public event EventHandler<double> ProgressChanged;

    public abstract Task<InstallResult> ExecuteAsync();

    protected void OnProgressChanged(double progress) => ProgressChanged?.Invoke(this, progress);
}
