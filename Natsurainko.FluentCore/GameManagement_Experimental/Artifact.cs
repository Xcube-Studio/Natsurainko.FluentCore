using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement;

public abstract class Artifact
{
    /// <summary>
    /// Relative path to the .minecraft folder
    /// </summary>
    public abstract string BasePath { get; }

    /// <summary>
    /// Expected SHA1 of the file
    /// </summary>
    public required string Sha1 { get; init; }

    /// <summary>
    /// Expected size of the file
    /// </summary>
    public required int Size { get; init; }

    /// <summary>
    /// URL to download the file
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Path to the file from the base path
    /// </summary>
    public required string FilePath { get; init; }
}

public class LibraryArtifact : Artifact
{
    public override string BasePath => "libraries";
}

public class AssetArtifact : Artifact
{
    public override string BasePath => "assets/objects";
}

public class AssetIndexArtifact : Artifact
{
    public override string BasePath => "assets/indexes";
}
