using Nrk.FluentCore.Experimental.GameManagement;
using Nrk.FluentCore.Experimental.GameManagement.Dependencies;
using Nrk.FluentCore.Experimental.GameManagement.ModLoaders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static Nrk.FluentCore.Experimental.GameManagement.ClientJsonObject;

namespace Nrk.FluentCore.GameManagement;

public abstract partial class MinecraftInstance
{
    /// <summary>
    /// Name of the folder of this instance
    /// <para>This matches the client.json filename and must be unique in a <see cref="MinecraftProfile"/></para>
    /// </summary>
    public required string VersionFolderName { get; init; }
    // The isolated instance feature implemented by third party launchers allows multiple
    // instances of the same version id, so the "id" field in client.json may not be unique.

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

    public required string AssetIndexJsonPath { get; init; }


    // Replaces DefaultAssetParser.GetAssetIndexJson
    public GameAssetIndex GetAssetIndex()
    {
        // Identify file paths
        string clientJsonPath = ClientJsonPath;
        string assetIndexJsonPath = AssetIndexJsonPath;
        if (this is ModifiedMinecraftInstance { HasInheritance: true } instance)
        {
            clientJsonPath = instance.InheritedMinecraftInstance.ClientJsonPath;
            assetIndexJsonPath = instance.InheritedMinecraftInstance.AssetIndexJsonPath;
        }

        // Parse client.json
        JsonNode? jsonNode = JsonNode.Parse(File.ReadAllText(clientJsonPath));
        AssstIndexJsonObject assetIndex = jsonNode?["assetIndex"]?.Deserialize<AssstIndexJsonObject>()
            ?? throw new InvalidDataException("Error in parsing version.json");

        // TODO: Handle nullable check in Json deserialization (requires .NET 9)
        string id = assetIndex.Id ?? throw new InvalidDataException();
        id = $"{id}.json";
        string sha1 = assetIndex.Sha1 ?? throw new InvalidDataException();
        int size = assetIndex.Size ?? throw new InvalidDataException();

        return new GameAssetIndex
        {
            Id = id,
            Sha1 = sha1,
            Size = size
        };
    }

    public IEnumerable<GameAsset> GetRequiredAssets()
    {
        // Identify file paths
        string assetIndexJsonPath = AssetIndexJsonPath;
        if (this is ModifiedMinecraftInstance { HasInheritance: true } instance)
            assetIndexJsonPath = instance.InheritedMinecraftInstance.AssetIndexJsonPath;

        // Parse asset index json
        JsonNode? jsonNode = JsonNode.Parse(File.ReadAllText(assetIndexJsonPath));
        Dictionary<string, AssetJsonNode> assets = jsonNode?["objects"]?
            .Deserialize<Dictionary<string, AssetJsonNode>>()
            ?? throw new InvalidDataException("Error in parsing asset index json file");

        // Parse GameAsset objects
        foreach (var (key, assetJsonNode) in assets)
        {
            string hash = assetJsonNode.Hash ?? throw new InvalidDataException("Invalid asset index");
            int size = assetJsonNode.Size ?? throw new InvalidDataException();

            yield return new GameAsset
            {
                Key = key,
                Sha1 = hash,
                Size = size
            };
        }
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
    public bool HasInheritance { get => InheritedMinecraftInstance is not null; }

    /// <summary>
    /// The instance from which this instance inherits
    /// </summary>
    public VanillaMinecraftInstance? InheritedMinecraftInstance { get; init; }
}