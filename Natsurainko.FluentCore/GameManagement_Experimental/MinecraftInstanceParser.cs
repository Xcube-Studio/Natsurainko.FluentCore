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

public abstract partial class MinecraftInstance
{
    public static MinecraftInstance Parse(DirectoryInfo clientDir)
    {
        // Find client.json
        var clientJsonFile = clientDir
            .GetFiles($"{clientDir.Name}.json")
            .FirstOrDefault()
            ?? throw new FileNotFoundException($"client.json not found in {clientDir.FullName}");

        // Parse client.json
        string clientJson = File.ReadAllText(clientJsonFile.FullName);
        var clientJsonNode = JsonNode.Parse(clientJson)
            ?? throw new JsonException($"Failed to parse {clientJsonFile.FullName}");

        var clientJsonObject = clientJsonNode.Deserialize<ClientJsonObject>()
            ?? throw new JsonException($"Failed to deserialize {clientJsonFile.FullName} into {typeof(ClientJsonObject)}");

        // Create MinecraftInstance
        return ParsingHelpers.IsVanilla(clientJsonObject)
            ? ParsingHelpers.ParseVanilla(clientJsonObject, clientJsonNode, clientDir, clientJsonFile.FullName)
            : ParsingHelpers.ParseModified(clientJsonObject, clientJsonNode, clientDir, clientJsonFile.FullName);
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

    public static MinecraftInstance ParseVanilla(ClientJsonObject clientJsonObject, JsonNode clientJsonNode, DirectoryInfo clientDir, string clientJsonPath)
    {
        // Parse <version> folder name
        string versionFolderName = clientDir.Name;

        // Get .minecraft folder path
        string minecraftFolderPath = clientDir.Parent?.Parent?.FullName
            ?? throw new DirectoryNotFoundException($"Failed to find .minecraft folder for {clientDir.FullName}");

        // Check if client.jar exists
        string clientJarPath = clientJsonPath[..^"json".Length] + "jar"; // Replace .json with .jar file extension
        if (!File.Exists(clientJarPath))
            throw new FileNotFoundException($"{clientJarPath} not found");

        // Parse version
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

        MinecraftVersion version = MinecraftVersion.Parse(versionId);

        return new VanillaMinecraftInstance
        {
            VersionFolderName = versionFolderName,
            Version = version,
            MinecraftFolderPath = minecraftFolderPath,
            ClientJsonPath = clientJsonPath,
            ClientJarPath = clientJarPath
        };
    }

    public static MinecraftInstance ParseModified(ClientJsonObject clientJsonObject, JsonNode clientJsonNode, DirectoryInfo clientDir, string clientJsonPath, IEnumerable<MinecraftInstance>? parsedInstances = null)
    {
        bool hasInheritance = !string.IsNullOrEmpty(clientJsonObject.InheritsFrom);
        VanillaMinecraftInstance? inheritedInstance = null!;
        if (hasInheritance)
        {
            // Parsed inherited instance if has inheritance
            inheritedInstance = (VanillaMinecraftInstance)parsedInstances?.FirstOrDefault()!;
        }

        // Parse <version> folder name
        string versionFolderName = clientDir.Name;

        // Get .minecraft folder path
        string minecraftFolderPath = clientDir.Parent?.Parent?.FullName
            ?? throw new DirectoryNotFoundException($"Failed to find .minecraft folder for {clientDir.FullName}");

        // Check if client.jar exists
        string clientJarPath = hasInheritance
            ? inheritedInstance.ClientJarPath // Use inherited client.jar path if has inheritance
            : clientJsonPath[..^"json".Length] + "jar"; // If there is no inheritance, replace .json with .jar file extension
        if (!File.Exists(clientJarPath))
            throw new FileNotFoundException($"{clientJarPath} not found");

        // Parse version
        string? versionId = clientJsonObject.Id; // By default, use the id in client.json

        // Read the version ID from the additional fields created by HMCL and PCL, regardless of whether it inherits from another instance or not, because
        // HMCL and PCL may modify the "id" field in client.json to store the nickname.
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

        MinecraftVersion version = hasInheritance
            ? inheritedInstance.Version
            : MinecraftVersion.Parse(versionId);

        return new ModifiedMinecraftInstance
        {
            VersionFolderName = versionFolderName,
            Version = version,
            MinecraftFolderPath = minecraftFolderPath,
            ClientJsonPath = clientJsonPath,
            ClientJarPath = clientJarPath,
            InheritedMinecraftInstance = inheritedInstance,
            ModLoaders = null! // TODO: Parse mod loaders (this requires parsing the "libraries" section in client.json)
        };
    }
}