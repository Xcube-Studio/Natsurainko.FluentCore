using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public class MinecraftClient : MinecraftDependency, IDownloadableDependency, IVerifiableDependency
{
    /// <inheritdoc/>
    public override string FilePath => Path.Combine("versions", ClientId, $"{ClientId}.jar");

    public required string ClientId { get; init; }

    /// <inheritdoc/>
    public required string Url { get; init; }

    /// <inheritdoc/>
    public required long Size { get; init; }

    /// <inheritdoc/>
    public required string Sha1 { get; init; }
}
