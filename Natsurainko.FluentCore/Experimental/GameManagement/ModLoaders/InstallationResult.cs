using System;
using System.Collections.Generic;

namespace Nrk.FluentCore.Experimental.GameManagement.ModLoaders;

public class InstallationResult
{
    public bool Success { get; set; }

    public Exception? Exception { get; set; }

    public IEnumerable<string>? Log { get; set; }
}
