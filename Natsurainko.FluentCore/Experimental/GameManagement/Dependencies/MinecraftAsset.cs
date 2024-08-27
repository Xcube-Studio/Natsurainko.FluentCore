using System.IO;

namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public class MinecraftAsset : MinecraftDependency, IDownloadableDependency, IVerifiableDependency
{
    /// <inheritdoc/>
    public override string FilePath => Path.Combine("assets", "objects", Sha1[0..2], Sha1);

    /// <summary>
    /// Key of the asset
    /// </summary>
    public required string Key { get; set; }

    /// <inheritdoc/>
    public string Url { get => $"https://resources.download.minecraft.net/{Sha1[0..2]}/{Sha1}"; }

    /// <inheritdoc/>
    public required long Size { get; init; }

    long? IVerifiableDependency.Size => this.Size;

    /// <inheritdoc/>
    public required string Sha1 { get; init; }
}
