using Nrk.FluentCore.Environment;
using System;
using System.IO;
using System.Text.RegularExpressions;
using static Nrk.FluentCore.GameManagement.ClientJsonObject;
using static Nrk.FluentCore.GameManagement.ClientJsonObject.LibraryJsonObject;

namespace Nrk.FluentCore.GameManagement.Dependencies;

public abstract partial class MinecraftLibrary : MinecraftDependency
{
    private readonly static Regex MavenParseRegex = GenerateMavenParseRegex();

    /// <inheritdoc/>
    public override string FilePath => Path.Combine("libraries", GetLibraryPath());

    /// <summary>
    /// Full package name of the Java library in the format "group_id:artifact_id:version[:classifier]"
    /// </summary>
    public string MavenName { get; init; }

    // <summary>
    // Parse a library from the full name of a Java library
    // </summary>
    // <remarks>If <paramref name="packageName"/> is not a Java library name, then it is set for <see cref="Name"/> and other fields are <see cref="string.Empty"/></remarks>
    // <param name="packageName">Full library name in the format of DOMAIN:NAME:VER:CLASSIFIER</param>
    public MinecraftLibrary(string mavenName)
    {
        this.MavenName = mavenName;
        Match match = MavenParseRegex.Match(mavenName);

        if (match.Success)
        {
            Domain = match.Groups["domain"].Value;
            Name = match.Groups["name"].Value;
            Version = match.Groups["version"].Value;

            if (match.Groups["classifier"].Success)
                Classifier = match.Groups["classifier"].Value;
        }
    }

    #region Maven Package Info

    public string? Domain { get; init; }

    public string? Name { get; init; }

    public string? Version { get; init; }

    public string? Classifier { get; init; }

    #endregion

    public required bool IsNativeLibrary { get; init; }

    internal string GetLibraryPath() => GetLibraryPath(this.MavenName);

    internal static string GetLibraryPath(string mavenName)
    {
        string path = "";

        var extension = mavenName.Contains('@') ? mavenName.Split('@') : [];
        var subString = extension.Length != 0
            ? mavenName.Replace($"@{extension[1]}", string.Empty).Split(':')
            : mavenName.Split(':');

        // Group name
        foreach (string item in subString[0].Split('.'))
            path = Path.Combine(path, item);

        // Artifact name + version
        path = Path.Combine(path, subString[1], subString[2]);

        // Filename of the library
        string filename = $"{subString[1]}-{subString[2]}{(subString.Length > 3 ? $"-{subString[3]}" : string.Empty)}.";
        filename += extension.Length != 0 ? extension[1] : "jar";

        return Path.Combine(path, filename);
    }

    internal static MinecraftLibrary ParseJsonNode(LibraryJsonObject libNode, string minecraftFolderPath)
    {
        // Check platform-specific library name
        if (libNode.MavenName is null)
            throw new InvalidDataException("Invalid library name");

        if (libNode.NativeClassifierNames is not null)
            libNode.MavenName += ":" + libNode.NativeClassifierNames[EnvironmentUtils.PlatformName].Replace("${arch}", EnvironmentUtils.SystemArch);

        if (libNode.DownloadInformation != null)
        {
            DownloadArtifactJsonObject artifactNode = GetLibraryArtifactInfo(libNode);
            if (artifactNode.Sha1 is null || artifactNode.Size is null || artifactNode.Url is null)
                throw new InvalidDataException("Invalid artifact node");

            #region Vanilla Pattern

            if (artifactNode.Url.StartsWith("https://libraries.minecraft.net/"))
            {
                return new VanillaLibrary(libNode.MavenName)
                {
                    MinecraftFolderPath = minecraftFolderPath,
                    Sha1 = artifactNode.Sha1,
                    Size = (long)artifactNode.Size,
                    IsNativeLibrary = libNode.NativeClassifierNames is not null
                };
            }

            #endregion

            #region Forge Pattern

            if (artifactNode.Url.StartsWith("https://maven.minecraftforge.net/"))
            {
                return new ForgeLibrary(libNode.MavenName)
                {
                    MinecraftFolderPath = minecraftFolderPath,
                    Sha1 = artifactNode.Sha1,
                    Size = (long)artifactNode.Size,
                    Url = artifactNode.Url,
                    IsNativeLibrary = false
                };
            }

            #endregion

            #region NeoForge Pattern

            if (artifactNode.Url.StartsWith("https://maven.neoforged.net/"))
            {
                return new NeoForgeLibrary(libNode.MavenName)
                {
                    MinecraftFolderPath = minecraftFolderPath,
                    Sha1 = artifactNode.Sha1,
                    Size = (long)artifactNode.Size,
                    Url = artifactNode.Url,
                    IsNativeLibrary = false
                };
            }

            #endregion
        }

        #region Other Patterns

        if (libNode.MavenName.StartsWith("net.minecraft:launchwrapper"))
        {
            return new DownloadableDependency(libNode.MavenName, $"https://libraries.minecraft.net/{GetLibraryPath(libNode.MavenName).Replace("\\", "/")}")
            {
                MinecraftFolderPath = minecraftFolderPath,
                IsNativeLibrary = libNode.NativeClassifierNames is not null
            };
        }

        #endregion

        #region Legacy Forge Pattern

        if (libNode.MavenUrl == "https://maven.minecraftforge.net/"
            || libNode.ClientRequest != null
            || libNode.ServerRequest != null)
        {
            string legacyForgeLibraryUrl = (libNode.MavenUrl == "https://maven.minecraftforge.net/"
                ? "https://maven.minecraftforge.net/"
                : "https://libraries.minecraft.net/") + GetLibraryPath(libNode.MavenName).Replace("\\", "/");

            return new LegacyForgeLibrary(libNode.MavenName, legacyForgeLibraryUrl)
            {
                MinecraftFolderPath = minecraftFolderPath,
                IsNativeLibrary = false,
                ClientRequest = libNode.ClientRequest.GetValueOrDefault() || (libNode.ClientRequest == null && libNode.ServerRequest == null)
            };
        }

        #endregion

        #region Fabric Pattern

        if (libNode.MavenUrl == "https://maven.fabricmc.net/")
        //&& libNode.Sha1 != null
        //&& libNode.Size != null)
        {
            return new FabricLibrary(libNode.MavenName)
            {
                MinecraftFolderPath = minecraftFolderPath,
                //Sha1 = libNode.Sha1,
                //Size = (long)libNode.Size,
                IsNativeLibrary = false
            };
        }

        #endregion

        #region Quilt Pattern

        if (libNode.MavenUrl == "https://maven.quiltmc.org/repository/release/"
            && libNode.Sha1 == null && libNode.Size == null && libNode.DownloadInformation == null)
        {
            return new QuiltLibrary(libNode.MavenName)
            {
                MinecraftFolderPath = minecraftFolderPath,
                IsNativeLibrary = false
            };
        }

        #endregion

        #region OptiFine Pattern

        if (libNode.MavenName.StartsWith("optifine:optifine", StringComparison.CurrentCultureIgnoreCase)
            || libNode.MavenName.StartsWith("optifine:launchwrapper-of", StringComparison.CurrentCultureIgnoreCase))
        {
            return new OptiFineLibrary(libNode.MavenName)
            {
                IsNativeLibrary = false,
                MinecraftFolderPath = minecraftFolderPath
            };
        }

        #endregion

        return new UnknownLibrary(libNode.MavenName)
        {
            IsNativeLibrary = false,
            MinecraftFolderPath = minecraftFolderPath
        };
    }

    private static DownloadArtifactJsonObject GetLibraryArtifactInfo(LibraryJsonObject libNode)
    {
        if (libNode.DownloadInformation is null)
            throw new InvalidDataException("The library does not contain download information");

        DownloadArtifactJsonObject? artifact = libNode.DownloadInformation.Artifact;
        if (libNode.NativeClassifierNames is not null)
        {
            string nativeClassifier = libNode.NativeClassifierNames[EnvironmentUtils.PlatformName]
                .Replace("${arch}", EnvironmentUtils.SystemArch);
            artifact = libNode.DownloadInformation.Classifiers?[nativeClassifier];
        }

        return artifact ?? throw new InvalidDataException("Invalid artifact information");
    }

    public override bool Equals(object? obj)
    {
        if (obj is MinecraftLibrary library)
            return library.FullPath.Equals(FullPath);

        return false;
    }

    public override int GetHashCode() => FullPath.GetHashCode();

    [GeneratedRegex(@"^(?<domain>[^:]+):(?<name>[^:]+):(?<version>[^:]+)(?::(?<classifier>[^:]+))?")]
    private static partial Regex GenerateMavenParseRegex();
}

public class VanillaLibrary(string mavenName) : MinecraftLibrary(mavenName), IDownloadableDependency, IVerifiableDependency
{

    /// <inheritdoc/>
    public string Url { get => $"https://libraries.minecraft.net/{GetLibraryPath().Replace("\\", "/")}"; }

    /// <inheritdoc/>
    public required long Size { get; init; }

    long? IVerifiableDependency.Size => Size;

    /// <inheritdoc/>
    public required string Sha1 { get; init; }
}

public class ForgeLibrary(string mavenName) : MinecraftLibrary(mavenName), IDownloadableDependency, IVerifiableDependency
{
    /// <inheritdoc/>
    public required string Url { get; init; }

    /// <inheritdoc/>
    public required long Size { get; init; }
    long? IVerifiableDependency.Size => Size;

    /// <inheritdoc/>
    public required string Sha1 { get; init; }
}

public class NeoForgeLibrary(string mavenName) : ForgeLibrary(mavenName) { }

public class LegacyForgeLibrary(string mavenName, string url) : MinecraftLibrary(mavenName), IDownloadableDependency
{
    public string Url { get; init; } = url;

    public required bool ClientRequest { get; init; }
}

public class OptiFineLibrary(string mavenName) : MinecraftLibrary(mavenName) { }

public class FabricLibrary(string mavenName) : MinecraftLibrary(mavenName), IDownloadableDependency //, IVerifiableDependency
{
    /// <inheritdoc/>
    public string Url { get => $"https://maven.fabricmc.net/{GetLibraryPath().Replace("\\", "/")}"; }

    ///// <inheritdoc/>
    //public required long Size { get; init; }

    //long? IVerifiableDependency.Size => this.Size;

    ///// <inheritdoc/>
    //public required string Sha1 { get; init; }
}

public class QuiltLibrary(string mavenName) : MinecraftLibrary(mavenName), IDownloadableDependency
{
    /// <inheritdoc/>
    public string Url { get => $"https://maven.quiltmc.org/repository/release/{GetLibraryPath().Replace("\\", "/")}"; }
}

public class DownloadableDependency(string mavenName, string url) : MinecraftLibrary(mavenName), IDownloadableDependency
{
    public string Url { get; init; } = url;
}

public class UnknownLibrary(string mavenName) : MinecraftLibrary(mavenName) { }