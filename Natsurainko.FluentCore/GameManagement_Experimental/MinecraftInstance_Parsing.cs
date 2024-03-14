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
            ? ParsingHelpers.ParseVanilla(clientJsonObject, clientDir, clientJsonFile.FullName)
            : ParsingHelpers.ParseModified(clientJsonObject);
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

    public static MinecraftInstance ParseVanilla(ClientJsonObject clientJsonObject, DirectoryInfo clientDir, string clientJsonPath)
    {
        // Parse client id
        string id = clientJsonObject.Id
            ?? throw new JsonException("Id is not defined in client.json");

        // Get .minecraft folder path
        string minecraftFolderPath = clientDir.Parent?.Parent?.FullName
            ?? throw new DirectoryNotFoundException($"Failed to find .minecraft folder for {clientDir.FullName}");

        // Check if client.jar exists
        string clientJarPath = clientJsonPath[..^"json".Length] + "jar"; // Replace .json with .jar file extension
        if (!File.Exists(clientJarPath))
            throw new FileNotFoundException($"client.jar not found in {clientDir.FullName}");

        // Parse version
        MinecraftVersionType? versionType = clientJsonObject.Type switch
        {
            "release" => MinecraftVersionType.Release,
            "old_beta" => MinecraftVersionType.OldBeta,
            "old_alpha" => MinecraftVersionType.OldAlpha,
            "snapshot" => null, // May be snapshot or pre-release, leave it for the MinecraftVersion constructor to parse from version id
            _ => null // Uncertain version type, leave it for the MinecraftVersion constructor to parse from version id
        };
        MinecraftVersion version = versionType == null
            ? new MinecraftVersion(id)
            : new MinecraftVersion(id, versionType.Value);

        return new VanillaMinecraftInstance
        {
            Id = id,
            Version = version,
            MinecraftFolderPath = minecraftFolderPath,
            ClientJsonPath = clientJsonPath,
            ClientJarPath = clientJarPath
        };
    }

    public static MinecraftInstance ParseModified(ClientJsonObject clientJsonObject)
    {
        throw new NotImplementedException();
    }
}