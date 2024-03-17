using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement;

public abstract class GameDependency // TODO: Implement IDownloadable interface for downloading game dependencies
{
    /// <summary>
    /// Relative path to the .minecraft folder
    /// </summary>
    public abstract string BasePath { get; }

    /// <summary>
    /// Expected local path to the file from the base path
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// URL to download the file
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Expected size of the file
    /// </summary>
    public required int Size { get; init; }

    /// <summary>
    /// Expected SHA1 of the file
    /// </summary>
    public required string Sha1 { get; init; }
}

public class GameLibrary : GameDependency
{
    public override string BasePath => "libraries";
}

public class GameAsset : GameDependency
{
    public override string BasePath => "assets/objects";
}

public class GameAssetIndex : GameDependency
{
    public override string BasePath => "assets/indexes";
}
