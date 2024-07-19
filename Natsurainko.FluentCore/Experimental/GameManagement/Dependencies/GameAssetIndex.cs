namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public class GameAssetIndex : GameDependency
{
    /// <inheritdoc/>
    public override string BasePath => "assets/indexes";

    /// <inheritdoc/>
    public override string FilePath => $"{Id}.json";

    /// <inheritdoc/>
    public override string Url => $"https://launchermeta.mojang.com/v1/packages/{Sha1}/{Id}.json";

    /// <summary>
    /// Asset index file ID
    /// </summary>
    public required string Id { get; set; }
}
