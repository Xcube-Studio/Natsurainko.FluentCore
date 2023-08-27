using Nrk.FluentCore.Classes.Datas.Download;
using Nrk.FluentCore.Classes.Datas.Install;
using Nrk.FluentCore.Components.Install;
using Nrk.FluentCore.DefaultComponents.Download;
using Nrk.FluentCore.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Nrk.FluentCore.DefaultComponents.Install;

public class QuiltInstallExecutor : BaseInstallExecutor
{
    public override Task<InstallResult> ExecuteAsync() => Task.Run(() =>
    {


        OnProgressChanged(1.0);

    }).ContinueWith(task =>
    {
        if (task.IsFaulted)
            return new InstallResult
            {
                Success = false,
                Exception = task.Exception,
                Log = null
            };

        return new InstallResult
        {
            Success = true,
            Exception = null,
            Log = null
        };
    });

}
