using Natsurainko.FluentCore.Interface;
using Natsurainko.Toolkits.IO;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Natsurainko.FluentCore.Extension;

public static class GameCoreExtension
{
    public static void Delete(this IGameCore core)
    {
        var directory = new DirectoryInfo(Path.Combine(core.Root.FullName, "versions", core.Id));

        if (directory.Exists)
            directory.DeleteAllFiles();

        directory.Delete();
    }

    public static void RenameWithoutResponse(this IGameCore core, string newName)
    {
        var directory = new DirectoryInfo(Path.Combine(core.Root.FullName, "versions", core.Id));
        var newDirectory = new DirectoryInfo(Path.Combine(core.Root.FullName, "versions", newName));

        directory.MoveTo(newDirectory.FullName);

        var jsonFile = new FileInfo(Path.Combine(newDirectory.FullName, $"{core.Id}.json"));
        var jarFile = new FileInfo(Path.Combine(newDirectory.FullName, $"{core.Id}.jar"));

        var newJsonFile = new FileInfo(jsonFile.FullName.Replace($"{core.Id}.json", $"{newName}.json"));

        jsonFile.MoveTo(newJsonFile.FullName);

        if (jarFile.Exists)
            jarFile.MoveTo(jarFile.FullName.Replace($"{core.Id}.jar", $"{newName}.jar"));

        var keyValuePairs = JObject.Parse(File.ReadAllText(newJsonFile.FullName));
        keyValuePairs["id"] = newName;

        File.WriteAllText(newJsonFile.FullName, keyValuePairs.ToString(formatting: Newtonsoft.Json.Formatting.Indented));
    }

    public static T Rename<T>(this T core, string newName) where T : IGameCore
    {
        RenameWithoutResponse(core, newName);
        core.Id = newName;

        return core;
    }
}
