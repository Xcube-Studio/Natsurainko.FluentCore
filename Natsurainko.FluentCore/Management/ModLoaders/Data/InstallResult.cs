using System;
using System.Collections.Generic;

namespace Nrk.FluentCore.Management.ModLoaders;

public class InstallResult
{
    public bool Success { get; set; }

    public Exception? Exception { get; set; }

    public IEnumerable<string>? Log { get; set; }
}
