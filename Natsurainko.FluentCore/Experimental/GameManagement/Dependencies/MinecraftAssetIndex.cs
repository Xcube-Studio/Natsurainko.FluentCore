using System.IO;

namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public class MinecraftAssetIndex : MinecraftDependency, IDownloadableDependency, IVerifiableDependency
{
    /// <inheritdoc/>
    public override string FilePath => Path.Combine("assets", "indexes", $"{Id}.json");

    /// <summary>
    /// Asset index file ID
    /// </summary>
    public required string Id { get; set; }

    /// <inheritdoc/>
    public string Url { get => $"https://launchermeta.mojang.com/v1/packages/{Sha1}/{Id}.json"; }

    /// <inheritdoc/>
    public required long Size { get; init; }

    long? IVerifiableDependency.Size => this.Size;

    /// <inheritdoc/>
    public required string Sha1 { get; init; }
}
