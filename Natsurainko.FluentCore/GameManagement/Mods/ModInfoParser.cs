using Nrk.FluentCore.GameManagement.Installer;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Tomlyn;
using Tomlyn.Model;

namespace Nrk.FluentCore.GameManagement.Mods;

public static class ModInfoParser
{
    public static MinecraftMod Parse(string filePath)
    {
        var supportedModLoaders = new List<ModLoaderType>();

        var modInfo = new MinecraftMod { AbsolutePath = filePath, IsEnabled = Path.GetExtension(filePath).Equals(".jar") };

        using var zipArchive = ZipFile.OpenRead(filePath);

        var quiltModJson = zipArchive.GetEntry("quilt.mod.json");
        var fabricModJson = zipArchive.GetEntry("fabric.mod.json");
        var forgeModsToml = zipArchive.GetEntry("META-INF/mods.toml");
        var neoForgeModsToml = zipArchive.GetEntry("META-INF/neoforge.mods.toml");
        var mcmodInfo = zipArchive.GetEntry("mcmod.info");

        if (quiltModJson != null)
            supportedModLoaders.Add(ModLoaderType.Quilt);
        if (fabricModJson != null)
            supportedModLoaders.Add(ModLoaderType.Fabric);
        if (forgeModsToml != null || mcmodInfo != null)
            supportedModLoaders.Add(ModLoaderType.Forge);
        if (neoForgeModsToml != null)
            supportedModLoaders.Add(ModLoaderType.NeoForge);

        if (supportedModLoaders.Count == 0)
            supportedModLoaders.Add(ModLoaderType.Unknown);
        modInfo.SupportedModLoaders = [.. supportedModLoaders];

        if (quiltModJson != null)
            return ParseModJson(ref modInfo, quiltModJson.ReadAsString(), true);
        if (fabricModJson != null)
            return ParseModJson(ref modInfo, fabricModJson.ReadAsString(), false);

        if (forgeModsToml != null)
            return ParseModsToml(ref modInfo, forgeModsToml.ReadAsString());
        if (mcmodInfo != null)
            return ParseForgeMcmodInfo(ref modInfo, mcmodInfo.ReadAsString());

        if (neoForgeModsToml != null)
            return ParseModsToml(ref modInfo, neoForgeModsToml.ReadAsString());

        throw new Exception("Unknown Mod Type");
    }

    public static bool TryParse(string filePath, [NotNullWhen(true)] out MinecraftMod? minecraftMod)
    {
        try
        {
            minecraftMod = Parse(filePath);
            return true;
        }
        catch
        {
            minecraftMod = null;
        }

        return false;
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
        mod.Description = jsonNode["description"]?.GetValue<string>().TrimEnd('\n').TrimEnd('\r');

        try
        {
            mod.Authors = jsonNode["authors"]
                ?.AsArray()
                .Where(x => x?.GetValue<string>() is not null)
                .Select(x => x?.GetValue<string>()!)
                .ToArray();
        }
        catch { }

        return mod;
    }

    private static MinecraftMod ParseModsToml(ref MinecraftMod mod, string tomlContent)
    {
        var tomlTable = ((Toml.ToModel(tomlContent)["mods"] as TomlTableArray)?.FirstOrDefault())
            ?? throw new InvalidDataException("Invalid mods.toml");

        mod.DisplayName = tomlTable.GetString("displayName");
        mod.Version = tomlTable.GetString("version");
        mod.Description = tomlTable.GetString("description")?.TrimEnd('\n').TrimEnd('\r');
        mod.Authors = tomlTable.GetString("authors")?.Split(",").Select(x => x.Trim(' ')).ToArray();

        if (mod.Version == "${file.jarVersion}") mod.Version = null;

        return mod;
    }

    private static MinecraftMod ParseForgeMcmodInfo(ref MinecraftMod mod, string jsonContent)
    {
        var jsonNode =
            JsonNode.Parse(jsonContent.Replace("\u000a", "") ?? "")?.AsArray().FirstOrDefault()
            ?? throw new InvalidDataException("Invalid mcmod.info");

        mod.DisplayName = jsonNode["name"]?.GetValue<string>();
        mod.Version = jsonNode["version"]?.GetValue<string>();
        mod.Description = jsonNode["description"]?.GetValue<string>().TrimEnd('\n').TrimEnd('\r');
        mod.Authors = (jsonNode["authorList"] ?? jsonNode["authors"])
            ?.AsArray()
            .Where(x => x?.GetValue<string>() is not null)
            .Select(x => x?.GetValue<string>()!)
            .ToArray();

        return mod;
    }
}
