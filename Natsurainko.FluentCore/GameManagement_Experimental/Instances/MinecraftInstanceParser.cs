using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using Nrk.FluentCore.Management;

namespace Nrk.FluentCore.GameManagement;

using PartialData = (
    string VersionFolderName,
    string MinecraftFolderPath,
    string ClientJsonPath
    );

public abstract partial class MinecraftInstance
{
    public static MinecraftInstance Parse(DirectoryInfo clientDir)
    {
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

        PartialData partialData = (versionFolderName, minecraftFolderPath, clientJsonPath);

        // Create MinecraftInstance
        return ParsingHelpers.IsVanilla(clientJsonObject)
            ? ParsingHelpers.ParseVanilla(partialData, clientJsonObject, clientJsonNode)
            : ParsingHelpers.ParseModified(partialData, clientJsonObject, clientJsonNode);
    }
}

file static class ParsingHelpers
{
    public static bool IsVanilla(ClientJsonObject clientJsonObject)
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

    /// <summary>
    /// Parse version ID from client.json in a non-inheriting instance
    /// </summary>
    /// <param name="clientJsonObject"></param>
    /// <param name="clientJsonNode"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
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

    public static MinecraftInstance ParseVanilla(PartialData partialData, ClientJsonObject clientJsonObject, JsonNode clientJsonNode)
    {
        // Check if client.jar exists
        string clientJarPath = ReplaceJsonWithJar(partialData.ClientJsonPath);
        if (!File.Exists(clientJarPath))
            throw new FileNotFoundException($"{clientJarPath} not found");

        // Parse version
        string versionId = ReadVersionIdFromNonInheritingClientJson(clientJsonObject, clientJsonNode);
        MinecraftVersion version = MinecraftVersion.Parse(versionId);

        return new VanillaMinecraftInstance
        {
            VersionFolderName = partialData.VersionFolderName,
            Version = version,
            MinecraftFolderPath = partialData.MinecraftFolderPath,
            ClientJsonPath = partialData.ClientJsonPath,
            ClientJarPath = clientJarPath
        };
    }

    // Parse a modified instance
    // If it has inheritance,
    // - If parsedInstances are provided, try to find inherited instance from the list
    // - If parsedinstances are not provided, or the inherited instance is not found in the list, parse inherited instance from the .minecraft folder
    public static MinecraftInstance ParseModified(PartialData partialData, ClientJsonObject clientJsonObject, JsonNode clientJsonNode, IEnumerable<MinecraftInstance>? parsedInstances = null)
    {
        bool hasInheritance = !string.IsNullOrEmpty(clientJsonObject.InheritsFrom);
        VanillaMinecraftInstance? inheritedInstance = null!;
        if (hasInheritance)
        {
            // Get inherited instance
            string inheritedInstanceId = clientJsonObject.InheritsFrom
                ?? throw new InvalidOperationException("InheritsFrom is not defined in client.json");

            inheritedInstance = parsedInstances?
                .Where(i => i is VanillaMinecraftInstance v && v.Version.VersionId == inheritedInstanceId)
                .FirstOrDefault() as VanillaMinecraftInstance;

            if (inheritedInstance is null)
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

        // TODO: Parse mod loaders (this requires parsing the "libraries" section in client.json)

        return new ModifiedMinecraftInstance
        {
            VersionFolderName = partialData.VersionFolderName,
            Version = (MinecraftVersion)version,
            MinecraftFolderPath = partialData.MinecraftFolderPath,
            ClientJsonPath = partialData.ClientJsonPath,
            ClientJarPath = clientJarPath,
            InheritedMinecraftInstance = inheritedInstance,
            ModLoaders = null!
        };
    }
}