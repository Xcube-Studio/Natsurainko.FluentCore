using Nrk.FluentCore.Environment;
using Nrk.FluentCore.Experimental.GameManagement.Dependencies;
using Nrk.FluentCore.Management.ModLoaders;
using Nrk.FluentCore.Utils;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Nrk.FluentCore.Experimental.GameManagement.ClientJsonObject;

namespace Nrk.FluentCore.Experimental.GameManagement.Instances;

public abstract partial class MinecraftInstance
{
    /// <summary>
    /// Name of the folder of this instance
    /// <para>This matches the client.json filename and must be unique in a <see cref="MinecraftProfile"/></para>
    /// </summary>
    public required string InstanceId { get; init; }
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

    #region Parse assets

    // Replaces DefaultAssetParser.GetAssetIndexJson
    public MinecraftAssetIndex GetAssetIndex()
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
        string sha1 = assetIndex.Sha1 ?? throw new InvalidDataException();
        int size = assetIndex.Size ?? throw new InvalidDataException();

        return new MinecraftAssetIndex
        {
            MinecraftFolderPath = MinecraftFolderPath,
            Id = id,
            Sha1 = sha1,
            Size = size
        };
    }

    public IEnumerable<MinecraftAsset> GetRequiredAssets()
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

            yield return new MinecraftAsset
            {
                MinecraftFolderPath = MinecraftFolderPath,
                Key = key,
                Sha1 = hash,
                Size = size
            };
        }
    }

    #endregion

    #region Parse libraries

    public (IEnumerable<MinecraftLibrary> Libraries, IEnumerable<MinecraftLibrary> NativeLibraries) GetRequiredLibraries()
    {
        List<MinecraftLibrary> libs = new();
        List<MinecraftLibrary> nativeLibs = new();

        var libNodes = JsonNodeUtils.ParseFile(ClientJsonPath)?["libraries"]?
            .Deserialize<IEnumerable<LibraryJsonObject>>()
            ?? throw new InvalidDataException("client.json does not contain library information");

        foreach (var libNode in libNodes)
        {
            if (libNode is null)
                continue;

            // Check if a library is enabled
            if (libNode.Rules is IEnumerable<OsRule> libRules)
            {
                if (!IsLibraryEnabled(libRules))
                    continue;
            }

            // Parse library
            var gameLib = MinecraftLibrary.ParseJsonNode(libNode, MinecraftFolderPath);

            // Add to the list of enabled libraries
            if (gameLib.IsNativeLibrary)
                nativeLibs.Add(gameLib);
            else
                libs.Add(gameLib);
        }

        return (libs, nativeLibs);
    }

    // Check if a library is enabled on the current platform given its OS rules 
    private static bool IsLibraryEnabled(IEnumerable<OsRule> rules)
    {
        bool windows, linux, osx;
        windows = linux = osx = false;

        foreach (var item in rules)
        {
            if (item.Action == "allow")
            {
                if (item.Os == null)
                {
                    windows = linux = osx = true;
                    continue;
                }

                switch (item.Os.Name)
                {
                    case "windows":
                        windows = true;
                        break;
                    case "linux":
                        linux = true;
                        break;
                    case "osx":
                        osx = true;
                        break;
                }
            }
            else if (item.Action == "disallow")
            {
                if (item.Os == null)
                {
                    windows = linux = osx = false;
                    continue;
                }

                switch (item.Os.Name)
                {
                    case "windows":
                        windows = false;
                        break;
                    case "linux":
                        linux = false;
                        break;
                    case "osx":
                        osx = false;
                        break;
                }
            }
        }

        // TODO: Check OS version and architecture?

        return EnvironmentUtils.PlatformName switch
        {
            "windows" => windows,
            "linux" => linux,
            "osx" => osx,
            _ => false,
        };
    }

    #endregion
}

public class VanillaMinecraftInstance : MinecraftInstance { }

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