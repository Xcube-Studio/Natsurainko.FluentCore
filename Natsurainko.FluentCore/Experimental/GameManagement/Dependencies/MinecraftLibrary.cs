using System;
using System.IO;
using System.Linq;

namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public class MinecraftLibrary : MinecraftDependency
{
    /// <inheritdoc/>
    public override string FilePath => Path.Combine("libraries", GetLibraryPath());

    /// <inheritdoc/>
    //public override string Url => $"https://libraries.minecraft.net/{GetLibraryPath()}";

    public override string Url => _url;

    private readonly string _url;

    /// <summary>
    /// Full package name of the Java library in the format "group_id:artifact_id:version[:classifier]"
    /// </summary>
    public required string MavenName { get; init; }

    // TODO: Quick accessors to package details

    //public required string Domain { get; init; } = "";

    //public required string Name { get; init; } = "";

    //public required string Version { get; init; } = "";

    //public string? Classifier { get; init; }

    public required bool IsNativeLibrary { get; init; }

    public MinecraftLibrary(string url)
    {
        _url = url;
    }

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
