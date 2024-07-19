namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public abstract class GameDependency // TODO: Implement IDownloadable interface for downloading game dependencies
{
    /// <summary>
    /// Relative path to the .minecraft folder
    /// </summary>
    public abstract string BasePath { get; } // Determined by the type of dependency

    /// <summary>
    /// Expected local path to the file from the base path
    /// </summary>
    public abstract string FilePath { get; } // Generated from the dependency name and other metadata, depending on the type of dependency

    /// <summary>
    /// URL to download the file
    /// </summary>
    public abstract string Url { get; } // Generated from the dependency name and other metadata, depending on the type of dependency

    /// <summary>
    /// Expected size of the file in bytes
    /// </summary>
    public required int Size { get; init; }

    /// <summary>
    /// Expected SHA1 of the file
    /// </summary>
    public required string Sha1 { get; init; }
}
