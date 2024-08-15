using Nrk.FluentCore.Experimental.GameManagement.ModLoaders;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Tomlyn;
using Tomlyn.Model;

namespace Nrk.FluentCore.Experimental.GameManagement.Mods;

public static class ModInfoParser
{
    public static MinecraftMod Parse(string filePath)
    {
        var supportedModLoaders = new List<ModLoaderType>();

        var modInfo = new MinecraftMod { AbsolutePath = filePath, IsEnabled = Path.GetExtension(filePath).Equals(".jar") };

        using var zipArchive = ZipFile.OpenRead(filePath);

        var quiltModJson = zipArchive.GetEntry("quilt.mod.json");
        var fabricModJson = zipArchive.GetEntry("fabric.mod.json");
        var modsToml = zipArchive.GetEntry("META-INF/mods.toml");
        var mcmodInfo = zipArchive.GetEntry("mcmod.info");

        if (quiltModJson != null)
            supportedModLoaders.Add(ModLoaderType.Quilt);
        if (fabricModJson != null)
            supportedModLoaders.Add(ModLoaderType.Fabric);
        if (modsToml != null || mcmodInfo != null)
            supportedModLoaders.Add(ModLoaderType.Forge);

        if (!supportedModLoaders.Any())
            supportedModLoaders.Add(ModLoaderType.Unknown);
        modInfo.SupportedModLoaders = supportedModLoaders.ToArray();

        if (quiltModJson != null)
            return ParseModJson(ref modInfo, quiltModJson.ReadAsString(), true);
        if (fabricModJson != null)
            return ParseModJson(ref modInfo, fabricModJson.ReadAsString(), false);

        if (modsToml != null)
            return ParseModsToml(ref modInfo, modsToml.ReadAsString());
        if (mcmodInfo != null)
            return ParseMcmodInfo(ref modInfo, mcmodInfo.ReadAsString());

        throw new Exception("Unknown Mod Type");
    }

    private static MinecraftMod ParseModJson(ref MinecraftMod mod, string jsonContent, bool isQuilt)
    {
        var jsonNode = JsonNode.Parse(jsonContent);

        if (isQuilt)
            jsonNode = jsonNode?["quilt_loader"]?["metadata"];

        if (jsonNode is null)
            throw new InvalidDataException($"Invalid {nameof(jsonContent)}");

        mod.DisplayName = jsonNode["name"]?.GetValue<string>();
        mod.Version = jsonNode["version"]?.GetValue<string>();
        mod.Description = jsonNode["description"]?.GetValue<string>();

        try
        {
            mod.Authors = jsonNode["authors"]
                ?.AsArray()
                .Where(x => x?.GetValue<string>() is not null)
                .Select(x => x?.GetValue<string>()!)
                .ToArray();
        } catch { }

        return mod;
    }

    private static MinecraftMod ParseModsToml(ref MinecraftMod mod, string tomlContent)
    {
        var tomlTable = (Toml.ToModel(tomlContent)["mods"] as TomlTableArray)?.First();
        if (tomlTable is null)
            throw new InvalidDataException("Invalid mods.toml");

        mod.DisplayName = tomlTable.GetString("displayName");
        mod.Version = tomlTable.GetString("version");
        mod.Description = tomlTable.GetString("description");
        mod.Authors = tomlTable.GetString("authors")?.Split(",").Select(x => x.Trim(' ')).ToArray();

        return mod;
    }

    private static MinecraftMod ParseMcmodInfo(ref MinecraftMod mod, string jsonContent)
    {
        var jsonNode =
            JsonNode.Parse(jsonContent.Replace("\u000a", "") ?? "")?.AsArray().FirstOrDefault()
            ?? throw new InvalidDataException("Invalid mcmod.info");

        mod.DisplayName = jsonNode["name"]?.GetValue<string>();
        mod.Version = jsonNode["version"]?.GetValue<string>();
        mod.Description = jsonNode["description"]?.GetValue<string>();
        mod.Authors = (jsonNode["authorList"] ?? jsonNode["authors"])
            ?.AsArray()
            .Where(x => x?.GetValue<string>() is not null)
            .Select(x => x?.GetValue<string>()!)
            .ToArray();

        return mod;
    }
}
