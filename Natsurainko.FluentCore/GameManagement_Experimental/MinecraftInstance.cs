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
    /// Id in client.json
    /// <para>Used as the identifier of this instance, must match the folder name</para>
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Minecraft versino of this instance
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
    public MinecraftInstance? InheritedMinecraftInstance { get; init; }
}