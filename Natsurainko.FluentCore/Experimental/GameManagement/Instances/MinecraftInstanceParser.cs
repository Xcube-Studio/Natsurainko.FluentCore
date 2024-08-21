using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;
using Nrk.FluentCore.Experimental.GameManagement.ModLoaders;
using Nrk.FluentCore.Management.ModLoaders;

namespace Nrk.FluentCore.Experimental.GameManagement.Instances;

// Temporary data structure for parsing Minecraft instances
using PartialData = (
    string VersionFolderName,
    string MinecraftFolderPath,
    string ClientJsonPath,
    string AssetIndexJsonPath
    );

// TODO: Consider upgrading to MinecraftInstanceManager?
public class MinecraftInstanceParser
{
    /// <summary>
    /// Successfully parsed instances
    /// </summary>
    public IReadOnlyCollection<MinecraftInstance> ParsedInstances => _parsedInstances;
    private readonly List<MinecraftInstance> _parsedInstances = new();

    /// <summary>
    /// Erroneous directories that failed to parse
    /// </summary>
    public IReadOnlyCollection<DirectoryInfo> ErroneousDirectories => _erroneousDirectories;
    private readonly List<DirectoryInfo> _erroneousDirectories = new();

    // .minecraft folder path
    private readonly string _minecraftFolderPath;


    /// <summary>
    /// Create a <see cref="MinecraftInstanceParser"/> for parsing instances in a particular .minecraft folder
    /// </summary>
    /// <param name="minecraftFolderPath">The absolute .minecraft folder path</param>
    public MinecraftInstanceParser(string minecraftFolderPath)
    {
        _minecraftFolderPath = minecraftFolderPath;
    }

    /// <summary>
    /// Parse all instances in the .minecraft/versions folder
    /// </summary>
    /// <returns>All Minecraft instances parsed in this .minecraft profile</returns>
    public IReadOnlyCollection<MinecraftInstance> ParseAllInstances()
    {
        var versionsDirectory = new DirectoryInfo(Path.Combine(_minecraftFolderPath, "versions"));

        if (!versionsDirectory.Exists)
            return []; // .minecraft/versions folder does not exist, no instance to parse.

        foreach (DirectoryInfo dir in versionsDirectory.EnumerateDirectories())
        {
            try
            {
                var instance = ParsingHelpers.Parse(dir, _parsedInstances, out bool inheritedInstanceAlreadyFound);

                // Remove the existing instance with the same version folder name as the one just parsed
                // This might be the case when the instance is modified and ParseAllInstances is called again
                int index = _parsedInstances.FindIndex(i => i.InstanceId == instance.InstanceId);
                if (index != -1)
                {
                    _parsedInstances.RemoveAt(index);
                }

                // Add the parsed instance and the inherited instance (if not already parsed) to the list
                _parsedInstances.Add(instance);
                if (instance is ModifiedMinecraftInstance m && m.HasInheritance && !inheritedInstanceAlreadyFound)
                    _parsedInstances.Add(m.InheritedMinecraftInstance);
            }
            catch (Exception) // TODO: Consider catching specific exceptions and display more detailed error types and info
            {
                _erroneousDirectories.Add(dir);
            }
        }

        return _parsedInstances;
    }
}

public abstract partial class MinecraftInstance
{
    /// <summary>
    /// Parse a Minecraft instance from a directory
    /// </summary>
    /// <param name="clientDir">A .minecraft/versions/&lt;version&gt; directory</param>
    /// <returns>The <see cref="MinecraftInstance"/> parsed</returns>
    public static MinecraftInstance Parse(DirectoryInfo clientDir)
        => ParsingHelpers.Parse(clientDir, null, out bool _);
}

file static class ParsingHelpers
{
    internal static MinecraftInstance Parse(DirectoryInfo clientDir, IEnumerable<MinecraftInstance>? parsedInstances, out bool foundInheritedInstanceInParsed)
    {
        foundInheritedInstanceInParsed = false;

        if (!clientDir.Exists)
            throw new DirectoryNotFoundException($"{clientDir.FullName} not found");

        // Find client.json
        var clientJsonFile = clientDir
            .GetFiles($"{clientDir.Name}.json")
            .FirstOrDefault()
            ?? throw new FileNotFoundException($"client.json not found in {clientDir.FullName}");
        string clientJsonPath = clientJsonFile.FullName;

        // Parse client.json
        string clientJson = File.ReadAllText(clientJsonPath);
        var clientJsonNode = JsonNode.Parse(clientJson)
            ?? throw new JsonException($"Failed to parse {clientJsonPath}");

        var clientJsonObject = clientJsonNode.Deserialize<ClientJsonObject>()
            ?? throw new JsonException($"Failed to deserialize {clientJsonPath} into {typeof(ClientJsonObject)}");

        // Parse MinecraftInstance data common to both vanilla, modified and inheriting instances

        // <version> folder name
        string versionFolderName = clientDir.Name;

        // .minecraft folder path
        string minecraftFolderPath = clientDir.Parent?.Parent?.FullName
            ?? throw new DirectoryNotFoundException($"Failed to find .minecraft folder for {clientDir.FullName}");

        // Asset index path
        string assetIndexId = clientJsonObject.AssetIndex?.Id
            ?? throw new InvalidDataException("Asset index ID does not exist in client.json");
        string assetIndexJsonPath = Path.Combine(minecraftFolderPath, "assets", "indexes", $"{assetIndexId}.json");

        PartialData partialData = (versionFolderName, minecraftFolderPath, clientJsonPath, assetIndexJsonPath);

        // Create MinecraftInstance
        return IsVanilla(clientJsonObject)
            ? ParseVanilla(partialData, clientJsonObject, clientJsonNode)
            : ParseModified(partialData, clientJsonObject, clientJsonNode, parsedInstances, out foundInheritedInstanceInParsed);
    }

    // Parse version ID from client.json in a non-inheriting instance
    private static string ReadVersionIdFromNonInheritingClientJson(ClientJsonObject clientJsonObject, JsonNode clientJsonNode)
    {
        string? versionId = clientJsonObject.Id;
        try
        {
            if (clientJsonNode["patches"] is JsonNode hmclPatchesNode)
            {
                // HMCL uses the "id" field in client.json to store the nickname of the game instance.
                // This is inconsistent with the standard behavior of the official Minecraft launcher, and
                // to adapt to this modification, read the version ID from the additional "version" field in the "patches" node created by HMCL.
                versionId = hmclPatchesNode[0]?["version"]?.GetValue<string>();
            }
            else if (clientJsonNode["clientVersion"] is JsonNode pclClientVersionNode)
            {
                // PCL uses the "id" field in client.json to store the nickname of the game instance.
                // This is inconsistent with the standard behavior of the official Minecraft launcher, and
                // to adapt to this modification, read the version ID from the addtional "clientVersion" field created by PCL.
                versionId = pclClientVersionNode.GetValue<string>();
            }

            if (versionId is null)
                throw new FormatException();
        }
        catch (Exception e) when (e is InvalidOperationException || e is FormatException)
        {
            throw new FormatException("Failed to parse version ID");
        }
        return versionId;
    }

    private static MinecraftInstance ParseVanilla(PartialData partialData, ClientJsonObject clientJsonObject, JsonNode clientJsonNode)
    {
        // Check if client.jar exists
        string clientJarPath = ReplaceJsonWithJar(partialData.ClientJsonPath);

        // Note:
        // client.jar 的下载地址只能从 version.json 中获取，
        // 因此在安装新的版本时，应该先解析 version.json 到 MinecraftInstance，然后才能下载 client.jar。
        // 所以此处不应该检查 client.jar 的存在

        //if (!File.Exists(clientJarPath))
        //    throw new FileNotFoundException($"{clientJarPath} not found");

        // Parse version
        string versionId = ReadVersionIdFromNonInheritingClientJson(clientJsonObject, clientJsonNode);
        MinecraftVersion version = MinecraftVersion.Parse(versionId);

        return new VanillaMinecraftInstance
        {
            AssetIndexJsonPath = partialData.AssetIndexJsonPath,
            InstanceId = partialData.VersionFolderName,
            Version = version,
            MinecraftFolderPath = partialData.MinecraftFolderPath,
            ClientJsonPath = partialData.ClientJsonPath,
            ClientJarPath = clientJarPath
        };
    }

    // Characteristic libraries of mod loaders
    // Key: Library name
    // Value1: ModLoaderType
    // Value2: Function that parses mod loader version from the library (java package) version
    private static readonly Dictionary<string, (ModLoaderType, Func<string, string>)> _modLoaderLibs = new()
    {
        { "net.minecraftforge:forge:", (ModLoaderType.Forge, libVersion => libVersion.Split('-')[1]) },
        { "net.minecraftforge:fmlloader:", (ModLoaderType.Forge, libVersion => libVersion.Split('-')[1]) },
        { "net.neoforged.fancymodloader:loader:", (ModLoaderType.NeoForge, libVersion => libVersion) },
        { "optifine:optifine", (ModLoaderType.OptiFine, libVersion => libVersion[(libVersion.IndexOf('_') + 1)..].ToUpper()) },
        { "net.fabricmc:fabric-loader:", (ModLoaderType.Fabric, libVersion => libVersion) },
        { "com.mumfrey:liteloader:", (ModLoaderType.LiteLoader, libVersion => libVersion) },
        { "org.quiltmc:quilt-loader:", (ModLoaderType.Quilt, libVersion => libVersion) },
    };

    // Parse a modified instance
    // If it has inheritance,
    // - If parsedInstances are provided, try to find inherited instance from the list
    // - If parsedinstances are not provided, or the inherited instance is not found in the list, parse inherited instance from the .minecraft folder
    private static MinecraftInstance ParseModified(
        PartialData partialData, ClientJsonObject clientJsonObject, JsonNode clientJsonNode,
        IEnumerable<MinecraftInstance>? parsedInstances,
        out bool foundInheritedInstanceInParsed)
    {
        foundInheritedInstanceInParsed = false;

        bool hasInheritance = !string.IsNullOrEmpty(clientJsonObject.InheritsFrom);
        VanillaMinecraftInstance? inheritedInstance = null!;
        if (hasInheritance)
        {
            // Find the inherited instance
            string inheritedInstanceId = clientJsonObject.InheritsFrom
                ?? throw new InvalidOperationException("InheritsFrom is not defined in client.json");

            inheritedInstance = parsedInstances?
                .Where(i => i is VanillaMinecraftInstance v && v.Version.VersionId == inheritedInstanceId)
                .FirstOrDefault() as VanillaMinecraftInstance;

            if (inheritedInstance is not null) // Found inherited instance in parsedInstances
            {
                foundInheritedInstanceInParsed = true;
            }
            else // Parse the inherited instance before parsing this modified instance
            {
                string inheritedInstancePath = Path.Combine(partialData.MinecraftFolderPath, "versions", inheritedInstanceId);
                var inheritedInstanceDir = new DirectoryInfo(inheritedInstancePath);

                inheritedInstance = MinecraftInstance.Parse(inheritedInstanceDir) as VanillaMinecraftInstance
                    ?? throw new InvalidOperationException($"Failed to parse inherited instance {inheritedInstanceId}");
            }
        }

        // Check if client.jar exists
        string clientJarPath = hasInheritance
            ? inheritedInstance.ClientJarPath // Use inherited client.jar path if has inheritance
            : ReplaceJsonWithJar(partialData.ClientJsonPath); // If there is no inheritance, replace .json with .jar file extension
        if (!File.Exists(clientJarPath))
            throw new FileNotFoundException($"{clientJarPath} not found");

        // Parse version
        MinecraftVersion? version;
        if (hasInheritance)
        {
            // Use version from the inherited instance
            version = inheritedInstance.Version;
        }
        else
        {
            // Read from client.json
            string versionId = ReadVersionIdFromNonInheritingClientJson(clientJsonObject, clientJsonNode);
            version = MinecraftVersion.Parse(versionId);
        }

        // Parse mod loaders
        List<ModLoaderInfo> modLoaders = [];
        var libraries = clientJsonObject.Libraries ?? [];
        foreach (var lib in libraries)
        {
            string? libNameLowered = lib.MavenName?.ToLower();
            if (libNameLowered is null)
                continue;

            foreach (var key in _modLoaderLibs.Keys)
            {
                if (!libNameLowered.Contains(key))
                    continue;

                // Mod loader library detected
                var id = libNameLowered.Split(':')[2];
                var loader = new ModLoaderInfo
                {
                    Type = _modLoaderLibs[key].Item1,
                    Version = _modLoaderLibs[key].Item2(id)
                };

                if (!modLoaders.Contains(loader))
                    modLoaders.Add(loader);

                break;
            }
        }

        return new ModifiedMinecraftInstance
        {
            AssetIndexJsonPath = partialData.AssetIndexJsonPath,
            InstanceId = partialData.VersionFolderName,
            Version = (MinecraftVersion)version,
            MinecraftFolderPath = partialData.MinecraftFolderPath,
            ClientJsonPath = partialData.ClientJsonPath,
            ClientJarPath = clientJarPath,
            InheritedMinecraftInstance = inheritedInstance,
            ModLoaders = modLoaders
        };
    }

    private static bool IsVanilla(ClientJsonObject clientJsonObject)
    {
        if (clientJsonObject.MainClass is null)
            throw new JsonException("MainClass is not defined in client.json");

        bool hasVanillaMainClass = clientJsonObject.MainClass is
            "net.minecraft.client.main.Main"
            or "net.minecraft.launchwrapper.Launch"
            or "com.mojang.rubydung.RubyDung";

        bool hasTweakClass =
            // Before 1.13
            clientJsonObject.MinecraftArguments?.Contains("--tweakClass") == true
            && clientJsonObject.MinecraftArguments?.Contains("net.minecraft.launchwrapper.AlphaVanillaTweaker") == false
            // Since 1.13
            || clientJsonObject.Arguments?.GameArguments?
                .Where(e => e is ClientJsonObject.ArgumentsJsonObject.DefaultClientArgument { Value: "--tweakClass" })
                .Any() == true;

        if (!string.IsNullOrEmpty(clientJsonObject.InheritsFrom)
            || !hasVanillaMainClass
            || hasVanillaMainClass && hasTweakClass)
            return false;

        return true;
    }

    private static string ReplaceJsonWithJar(string clientJsonPath)
    {
        return clientJsonPath[..^"json".Length] + "jar"; // Replace .json with .jar file extension
    }
}