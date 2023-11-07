using Nrk.FluentCore.Classes.Datas.Install;
using Nrk.FluentCore.Launch;
using System;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameResources.ModLoaders;

public interface IInstallExecutor
{
    string AbsoluteId { get; }

    GameInfo InheritedFrom { get; }

    event EventHandler<double> ProgressChanged;

    Task<InstallResult> ExecuteAsync();
}
