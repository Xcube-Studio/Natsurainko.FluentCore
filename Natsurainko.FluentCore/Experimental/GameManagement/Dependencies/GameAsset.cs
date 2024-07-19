namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public class GameAsset : GameDependency
{
    /// <inheritdoc/>
    public override string BasePath => "assets/objects";

    /// <inheritdoc/>
    public override string FilePath => $"{Sha1[0..2]}/{Sha1}";

    /// <inheritdoc/>
    public override string Url => $"https://resources.download.minecraft.net/{FilePath}";

    /// <summary>
    /// Key of the asset
    /// </summary>
    public required string Key { get; set; }
}
