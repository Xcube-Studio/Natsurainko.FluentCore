using Nrk.FluentCore.Classes.Datas.Install;
using Nrk.FluentCore.Classes.Datas.Launch;
using System;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Interfaces;

public interface IInstallExecutor
{
    string AbsoluteId { get; }

    GameInfo InheritedFrom { get; }

    event EventHandler<double> ProgressChanged;

    Task<InstallResult> ExecuteAsync();
}
