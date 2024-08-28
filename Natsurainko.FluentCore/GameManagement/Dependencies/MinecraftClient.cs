using System.IO;

namespace Nrk.FluentCore.GameManagement.Dependencies;

public class MinecraftClient : MinecraftDependency, IDownloadableDependency, IVerifiableDependency
{
    /// <inheritdoc/>
    public override string FilePath => Path.Combine("versions", ClientId, $"{ClientId}.jar");

    public required string ClientId { get; init; }

    /// <inheritdoc/>
    public required string Url { get; init; }

    /// <inheritdoc/>
    public required long Size { get; init; }

    long? IVerifiableDependency.Size => Size;

    /// <inheritdoc/>
    public required string Sha1 { get; init; }
}
