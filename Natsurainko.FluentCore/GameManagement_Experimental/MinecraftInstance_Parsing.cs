using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;

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
        throw new NotImplementedException();
    }
}
