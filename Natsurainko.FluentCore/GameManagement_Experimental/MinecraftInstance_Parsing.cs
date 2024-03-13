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
        bool isVanilla = ParsingHelpers.IsVanilla(clientJsonObject);
        throw new NotImplementedException();
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
}