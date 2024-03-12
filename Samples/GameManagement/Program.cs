using Nrk.FluentCore.GameManagement;
using System.Collections.Generic;
using System.Text.Json;

var mcFolderPath = @"C:\Users\jinch\Saved Games\Minecraft\.minecraft";
var versions = Directory.GetDirectories(Path.Combine(mcFolderPath, "versions"));

List<ClientJsonObject?> clientJsonObjects = new();
foreach (var version in versions)
{
    string? clientJsonFile = Directory.GetFiles(version, "*.json").FirstOrDefault();
    if (clientJsonFile == null)
        continue;
    string json = File.ReadAllText(clientJsonFile);
    clientJsonObjects.Add(JsonSerializer.Deserialize<ClientJsonObject>(json));
}
Console.WriteLine();
