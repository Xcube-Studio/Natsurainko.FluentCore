using System;
using System.Collections.Generic;

namespace Nrk.FluentCore.GameManagement.Installer;

public class OptiFineCompileProcessException(List<string> errorOutput)
    : Exception("An exception occurred in the OptiFine compilation process")
{
    public IReadOnlyList<string> Errors { get; init; } = errorOutput;
}
