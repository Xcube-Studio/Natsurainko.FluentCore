using Nrk.FluentCore.GameManagement.Installer;
using Nrk.FluentCore.Resources;
using Nrk.FluentCore.Resources.CurseForge;
using Nrk.FluentCore.Resources.Modrinth;
using Nrk.FluentCore.Utils;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
namespace Nrk.FluentCore.Experimental.GameManagement.Modpacks;

public static class ModpackInfoParser
{
    public static bool TryParseModpack(string packageFilePath, [NotNullWhen(true)] out ModpackInfo? modpackInfo)
    {
        using ZipArchive zipArchive = ZipFile.OpenRead(packageFilePath);

        try
        {
            if (zipArchive.GetEntry("manifest.json") is not null)
            {
                modpackInfo = ParseCurseForgeModpack(zipArchive, out _);
                return true;
            }
            else if (zipArchive.GetEntry("modrinth.index.json") is not null)
            {
                modpackInfo = ParseModrinthModpack(zipArchive, out _);
                return true;
            }
        }
        catch (Exception) { }

        modpackInfo = null;
        return false;
    }

    public static ModpackInfo ParseCurseForgeModpack(ZipArchive packageArchive, out CurseForgeModpackManifest modpackManifest)
    {
        modpackManifest = JsonSerializer.Deserialize(
            packageArchive.GetEntry("manifest.json")?.ReadAsString() ?? throw new InvalidDataException(),
            ResourcesJsonSerializerContext.Default.CurseForgeModpackManifest) ?? throw new InvalidDataException();

        if (string.IsNullOrEmpty(modpackManifest.Minecraft.McVersion))
            throw new InvalidDataException("could not parse modpack minecraft version");
        if (modpackManifest.Minecraft.ModLoaders.Length >= 2)
            throw new InvalidDataException("could not parse modpack modloader, count of modloader >= 2");

        ModLoaderInfo? modLoaderInfo = null;
        if (modpackManifest.Minecraft.ModLoaders.Length != 0)
        {
            var primaryModLoader = modpackManifest.Minecraft.ModLoaders.First(m => m.Primary)
                ?? throw new InvalidDataException("could not parse modpack primary modloader");
            string[] identifiers = primaryModLoader.Id.Split('-');

            if (identifiers.Length != 2) throw new InvalidDataException("could not parse modpack modloader");

            modLoaderInfo = new ModLoaderInfo
            (
                identifiers[0] switch
                {
                    "forge" => ModLoaderType.Forge,
                    "neoforge" => ModLoaderType.NeoForge,
                    "fabric" => ModLoaderType.Fabric,
                    "quilt" => ModLoaderType.Quilt,
                    _ => throw new NotSupportedException()
                },
                identifiers[1]
            );
        }

        return new ModpackInfo
        {
            Name = modpackManifest.Name,
            Version = modpackManifest.Version,
            Author = modpackManifest.Author,
            McVersion = modpackManifest.Minecraft.McVersion,
            ModpackType = ModpackType.CurseForge,
            ModLoader = modLoaderInfo
        };
    }

    public static ModpackInfo ParseModrinthModpack(ZipArchive packageArchive, out ModrinthModpackManifest modpackManifest)
    {
        modpackManifest = JsonSerializer.Deserialize(
            packageArchive.GetEntry("modrinth.index.json")?.ReadAsString() ?? throw new InvalidDataException(),
            ResourcesJsonSerializerContext.Default.ModrinthModpackManifest) ?? throw new InvalidDataException();

        if (!modpackManifest.Dependencies.TryGetValue("minecraft", out string? mcVersion))
            throw new InvalidDataException("could not parse modpack minecraft version");
        if (modpackManifest.Dependencies.Keys.Count > 2)
            throw new InvalidDataException("could not parse modpack modloader, count of modloader >= 2");

        ModLoaderInfo? modLoaderInfo = null;
        if (modpackManifest.Dependencies.Keys.Count == 2)
        {
            string loaderName = modpackManifest.Dependencies.Keys.First(k => k != "minecraft");
            modLoaderInfo = new ModLoaderInfo
            (
                loaderName switch
                {
                    "forge" => ModLoaderType.Forge,
                    "neoforge" => ModLoaderType.NeoForge,
                    "fabric-loader" => ModLoaderType.Fabric,
                    "quilt-loader" => ModLoaderType.Quilt,
                    _ => throw new NotSupportedException()
                },
                modpackManifest.Dependencies[loaderName]
            );
        }

        return new ModpackInfo
        {
            Name = modpackManifest.Name,
            Description = modpackManifest.Summary,
            Version = modpackManifest.VersionId,
            McVersion = mcVersion,
            ModpackType = ModpackType.Modrinth,
            ModLoader = modLoaderInfo
        };
    }
}