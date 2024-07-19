using System;
using System.IO;
using System.Linq;

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
    public abstract string Url { get; init; } // Generated from the dependency name and other metadata, depending on the type of dependency

    /// <summary>
    /// Expected size of the file in bytes
    /// </summary>
    public required int Size { get; init; }

    /// <summary>
    /// Expected SHA1 of the file
    /// </summary>
    public required string Sha1 { get; init; }
}

public class GameLibrary : GameDependency
{
    /// <inheritdoc/>
    public override string BasePath => "libraries";

    /// <inheritdoc/>
    public override string FilePath => GetLibraryPath();

    /// <inheritdoc/>
    //public override string Url => $"https://libraries.minecraft.net/{FilePath}";

    public required override string Url { get; init; }

    /// <summary>
    /// Full package name of the Java library in the format "group_id:artifact_id:version[:classifier]"
    /// </summary>
    public required string MavenName { get; init; }

    // TODO: Quick accessors to package details

    //public required string Domain { get; init; } = "";

    //public required string Name { get; init; } = "";

    //public required string Version { get; init; } = "";

    //public string? Classifier { get; init; }

    public bool IsNativeLibrary { get; init; }

    private string GetLibraryPath()
    {
        string path = "";

        var extension = MavenName.Contains('@') ? MavenName.Split('@') : Array.Empty<string>();
        var subString = extension.Any()
            ? MavenName.Replace($"@{extension[1]}", string.Empty).Split(':')
            : MavenName.Split(':');

        // Group name
        foreach (string item in subString[0].Split('.'))
            path = Path.Combine(path, item);

        // Artifact name + version
        path = Path.Combine(path, subString[1], subString[2]);

        // Filename of the library
        string filename = $"{subString[1]}-{subString[2]}{(subString.Length > 3 ? $"-{subString[3]}" : string.Empty)}.";
        if (extension.Any())
            filename += extension[1];
        else
            filename += "jar";

        return Path.Combine(path, filename);
    }

    // <summary>
    // Parse a library from the full name of a Java library
    // </summary>
    // <remarks>If <paramref name="packageName"/> is not a Java library name, then it is set for <see cref="Name"/> and other fields are <see cref="string.Empty"/></remarks>
    // <param name="packageName">Full library name in the format of DOMAIN:NAME:VER:CLASSIFIER</param>
    //public MinecraftLibrary(string packageName)
    //{
    //    Regex regex = new(@"^(?<domain>[^:]+):(?<name>[^:]+):(?<version>[^:]+)(?::(?<classifier>[^:]+))?");
    //    Match match = regex.Match(packageName);

    //    if (!match.Success)
    //    {
    //        Name = packageName;
    //        return;
    //    }

    //    Domain = match.Groups["domain"].Value;
    //    Name = match.Groups["name"].Value;
    //    Version = match.Groups["version"].Value;
    //    if (match.Groups["classifier"].Success)
    //        Classifier = match.Groups["classifier"].Value;
    //}
}

public class GameAsset : GameDependency
{
    /// <inheritdoc/>
    public override string BasePath => "assets/objects";

    /// <inheritdoc/>
    public override string FilePath => $"{Sha1[0..2]}/{Sha1}";

    /// <inheritdoc/>
    public override string Url { get; init; }

    /// <summary>
    /// Key of the asset
    /// </summary>
    public required string Key { get; set; }

    public GameAsset()
    {
        Url = $"https://resources.download.minecraft.net/{FilePath}";
    }
}

public class GameAssetIndex : GameDependency
{
    /// <inheritdoc/>
    public override string BasePath => "assets/indexes";

    /// <inheritdoc/>
    public override string FilePath => $"{Id}.json";

    /// <inheritdoc/>
    public override string Url { get; init; }

    /// <summary>
    /// Asset index file ID
    /// </summary>
    public required string Id { get; set; }

    public GameAssetIndex()
    {
        Url = $"https://launchermeta.mojang.com/v1/packages/{Sha1}/{Id}.json";
    }
}
