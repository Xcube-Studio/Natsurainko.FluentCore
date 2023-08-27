using Nrk.FluentCore.Classes.Datas.Install;
using Nrk.FluentCore.Components.Install;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Nrk.FluentCore.DefaultComponents.Install;

public class ForgeInstallExecutor : BaseInstallExecutor
{
    public required string JavaPath { get; set; }

    private ZipArchive _packageArchive;

    private readonly List<string> _outputs = new();
    private readonly List<string> _errorOutputs = new();

    public override Task<InstallResult> ExecuteAsync()
    {
        throw new NotImplementedException();
    }
}
