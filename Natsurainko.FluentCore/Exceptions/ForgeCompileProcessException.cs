using System;
using System.Collections.Generic;

namespace Nrk.FluentCore.Exceptions;

public class ForgeCompileProcessException(List<string> errorOutput)
    : Exception("An exception occurred in the Forge compilation process")
{
    public IReadOnlyList<string> Errors { get; init; } = errorOutput;
}
