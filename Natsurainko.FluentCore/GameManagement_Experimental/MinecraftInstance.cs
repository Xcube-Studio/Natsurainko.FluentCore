using Nrk.FluentCore.GameManagement.ModLoaders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement;

public abstract partial class MinecraftInstance
{
    /// <summary>
    /// Name of the folder of this instance
    /// <para>This matches the client.json filename and must be unique in a <see cref="MinecraftProfile"/></para>
    /// </summary>
    public required string VersionFolderName { get; init; }
    // This choice is made because the isolated instance feature implemented by third party launchers
    // allows multiple instances of the same version id, so the "id" field in client.json may not be unique.

    /// <summary>
    /// Minecraft version of this instance
    /// <para>Parsed from id in client.json</para>
    /// </summary>
    public required MinecraftVersion Version { get; init; }

    /// <summary>
    /// If the instance is a vanilla instance
    /// </summary>
    public bool IsVanilla { get => this is VanillaMinecraftInstance; }

    /// <summary>
    /// Absolute path of the .minecraft folder
    /// </summary>
    public required string MinecraftFolderPath { get; init; }

    /// <summary>
    /// Absolute path of client.json
    /// </summary>
    public required string ClientJsonPath { get; init; }

    /// <summary>
    /// Absolute path of client.jar
    /// </summary>
    public required string ClientJarPath { get; init; }


    public IEnumerable<GameAsset> GetRequiredAssets()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<GameLibrary> GetRequiredLibraries()
    {
        throw new NotImplementedException();
    }
}

public class VanillaMinecraftInstance : MinecraftInstance { }

/// <summary>
/// Mod loader information
/// </summary>
/// <param name="Type">Type of a mod loader</param>
/// <param name="Version">Version of a mod loader</param>
public record struct ModLoaderInfo(ModLoaderType Type, string Version);

public class ModifiedMinecraftInstance : MinecraftInstance
{
    /// <summary>
    /// List of mod loaders installed in this instance
    /// </summary>
    public required IEnumerable<ModLoaderInfo> ModLoaders { get; init; }

    /// <summary>
    /// If the instance inherits from another instance
    /// </summary>
    [MemberNotNullWhen(true, nameof(InheritedMinecraftInstance))]
    public bool HasInheritence { get => InheritedMinecraftInstance is not null; }

    /// <summary>
    /// The instance from which this instance inherits
    /// </summary>
    public VanillaMinecraftInstance? InheritedMinecraftInstance { get; init; }
}