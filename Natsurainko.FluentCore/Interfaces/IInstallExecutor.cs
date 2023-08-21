using Nrk.FluentCore.Classes.Datas.Install;
using System;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Interfaces;

public interface IInstallExecutor
{
    string PackageFilePath { get; }

    event EventHandler ProgressChanged;

    Task<InstallResult> ExecuteAsync();
}
