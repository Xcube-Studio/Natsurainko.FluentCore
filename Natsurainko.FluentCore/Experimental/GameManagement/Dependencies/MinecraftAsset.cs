using System.IO;

namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public class MinecraftAsset : MinecraftDependency
{
    /// <inheritdoc/>
    public override string FilePath => Path.Combine("assets", "objects", Sha1[0..2], Sha1);

    /// <inheritdoc/>
    public override string Url => $"https://resources.download.minecraft.net/{FilePath}";

    /// <summary>
    /// Key of the asset
    /// </summary>
    public required string Key { get; set; }
}
