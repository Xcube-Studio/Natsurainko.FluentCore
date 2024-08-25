using Nrk.FluentCore.Environment;
using System;
using System.IO;
using System.Linq;
using static Nrk.FluentCore.Experimental.GameManagement.ClientJsonObject;
using static Nrk.FluentCore.Experimental.GameManagement.ClientJsonObject.LibraryJsonObject;

namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public abstract class MinecraftLibrary : MinecraftDependency
{
    /// <inheritdoc/>
    public override string FilePath => Path.Combine("libraries", GetLibraryPath());

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

    internal string GetLibraryPath()
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
                return new VanillaLibrary()
                {
                    MinecraftFolderPath = minecraftFolderPath,
                    MavenName = libNode.MavenName,
                    Sha1 = artifactNode.Sha1,
                    Size = (long)artifactNode.Size,
                    IsNativeLibrary = libNode.NativeClassifierNames is not null
                };
            }

            #endregion

            #region Forge Pattern

            if (artifactNode.Url.StartsWith("https://maven.minecraftforge.net/"))
            {
                return new ForgeLibrary()
                {
                    MinecraftFolderPath = minecraftFolderPath,
                    MavenName = libNode.MavenName,
                    Sha1 = artifactNode.Sha1,
                    Size = (long)artifactNode.Size,
                    Url = artifactNode.Url,
                    IsNativeLibrary = false
                };
            }

            #endregion
        }

        #region Legacy Forge Pattern

        if (libNode.MavenUrl == "https://maven.minecraftforge.net/"
            || libNode.ClientRequest != null
            || libNode.ServerRequest != null)
        {
            return new LegacyForgeLibrary()
            {
                MinecraftFolderPath = minecraftFolderPath,
                MavenName = libNode.MavenName,
                IsNativeLibrary = false
            };
        }

        #endregion

        #region Fabric Pattern

        if (libNode.MavenUrl == "https://maven.fabricmc.net/")
            //&& libNode.Sha1 != null
            //&& libNode.Size != null)
        {
            return new FabricLibrary()
            {
                MinecraftFolderPath = minecraftFolderPath,
                MavenName = libNode.MavenName,
                //Sha1 = libNode.Sha1,
                //Size = (long)libNode.Size,
                IsNativeLibrary = false
            };
        }

        #endregion
            
        #region Quilt Pattern

        if ((libNode.MavenUrl == "https://maven.quiltmc.org/repository/release/")
            && libNode.Sha1 == null && libNode.Size == null && libNode.DownloadInformation == null)
        {
            return new QuiltLibrary()
            {
                MinecraftFolderPath = minecraftFolderPath,
                MavenName = libNode.MavenName,
                IsNativeLibrary = false
            };
        }

        #endregion

        #region OptiFine Pattern

        if (libNode.MavenName.ToLower().StartsWith("optifine:optifine")
            || libNode.MavenName.ToLower().StartsWith("optifine:launchwrapper-of"))
        {
            return new OptiFineLibrary
            { 
                MavenName = libNode.MavenName,
                IsNativeLibrary = false,
                MinecraftFolderPath = minecraftFolderPath
            };
        }

        #endregion

        return new UnknownLibrary
        {
            IsNativeLibrary = false,
            MavenName = libNode.MavenName,
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
            return library.FullPath.Equals(this.FullPath);

        return false;
    }

    public override int GetHashCode() => this.FullPath.GetHashCode();

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

public class VanillaLibrary : MinecraftLibrary, IDownloadableDependency, IVerifiableDependency
{
    /// <inheritdoc/>
    public string Url { get => $"https://libraries.minecraft.net/{GetLibraryPath().Replace("\\", "/")}"; }

    /// <inheritdoc/>
    public required long Size { get; init; }

    long? IVerifiableDependency.Size => this.Size;

    /// <inheritdoc/>
    public required string Sha1 { get; init; }
}

public class ForgeLibrary : MinecraftLibrary, IDownloadableDependency, IVerifiableDependency
{
    /// <inheritdoc/>
    public required string Url { get; init; }

    /// <inheritdoc/>
    public required long Size { get; init; }
    long? IVerifiableDependency.Size => this.Size;

    /// <inheritdoc/>
    public required string Sha1 { get; init; }
}

public class LegacyForgeLibrary : MinecraftLibrary { }

public class OptiFineLibrary : MinecraftLibrary { }

public class FabricLibrary : MinecraftLibrary, IDownloadableDependency //, IVerifiableDependency
{
    /// <inheritdoc/>
    public string Url { get => $"https://maven.fabricmc.net/{GetLibraryPath().Replace("\\", "/")}"; }

    ///// <inheritdoc/>
    //public required long Size { get; init; }

    //long? IVerifiableDependency.Size => this.Size;

    ///// <inheritdoc/>
    //public required string Sha1 { get; init; }
}

public class QuiltLibrary : MinecraftLibrary, IDownloadableDependency
{
    /// <inheritdoc/>
    public string Url { get => $"https://maven.quiltmc.org/{GetLibraryPath().Replace("\\", "/")}"; }
}

public class UnknownLibrary : MinecraftLibrary { }