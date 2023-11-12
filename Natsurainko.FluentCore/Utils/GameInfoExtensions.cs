using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nrk.FluentCore.Management.Parsing;
using Nrk.FluentCore.Management.ModLoaders;
using Nrk.FluentCore.Launch;

namespace Nrk.FluentCore.Utils;

// TOOD: use internal
public static class GameInfoExtensions
{
    public static LibraryElement? GetJarElement(this GameInfo gameInfo)
    {
        var jsonClient = JsonNode.Parse(File.ReadAllText(gameInfo.IsInheritedFrom ? gameInfo.InheritsFrom.VersionJsonPath : gameInfo.VersionJsonPath))
            ?["downloads"]?["client"];

        if (jsonClient is null)
            return null;

        string? absolutePath = gameInfo.IsInheritedFrom ? gameInfo.InheritsFrom.JarPath : gameInfo.JarPath;
        if (absolutePath is null)
            return null;

        return new LibraryElement
        {
            AbsolutePath = absolutePath,
            Checksum = jsonClient["sha1"]?.GetValue<string>(),
            Url = jsonClient["url"]?.GetValue<string>()
        };
    }

    public static string GetSuitableJavaVersion(this GameInfo gameInfo)
    {
        if (gameInfo.IsInheritedFrom)
            return gameInfo.InheritsFrom.GetSuitableJavaVersion();

        JsonNode jsonNode = JsonNodeUtils.ParseFile(gameInfo.VersionJsonPath);

        var jsonMajorVersion = jsonNode["javaVersion"]?["majorVersion"];
        if (jsonMajorVersion != null) return jsonMajorVersion.GetValue<int>().ToString();

        return "8";
    }

    public static IEnumerable<ModLoaderInfo> GetModLoaders(this GameInfo gameInfo)
    {
        var handle = new Dictionary<string, (ModLoaderType, Func<string, string>)>()
        {
            { "net.minecraftforge:forge:", (ModLoaderType.Forge, libVersion => libVersion.Split('-')[1]) },
            { "net.minecraftforge:fmlloader:", (ModLoaderType.Forge, libVersion => libVersion.Split('-')[1]) },
            { "net.neoforged.fancymodloader:loader:", (ModLoaderType.NeoForge, libVersion => libVersion) },
            { "optifine:optifine", (ModLoaderType.OptiFine, libVersion => libVersion[(libVersion.IndexOf('_') + 1)..].ToUpper()) },
            { "net.fabricmc:fabric-loader", (ModLoaderType.Fabric, libVersion => libVersion) },
            { "com.mumfrey:liteloader:", (ModLoaderType.LiteLoader, libVersion => libVersion) },
            { "org.quiltmc:quilt-loader:", (ModLoaderType.Quilt, libVersion => libVersion) },
        };

        JsonNode jsonNode = JsonNodeUtils.ParseFile(gameInfo.VersionJsonPath);
        var libraryJsonNodes = jsonNode["libraries"].Deserialize<IEnumerable<LibraryJsonNode>>();
        if (libraryJsonNodes is null)
            yield break;

        var enumed = new List<ModLoaderInfo>();
        foreach (var library in libraryJsonNodes)
        {
            var loweredName = library.Name.ToLower();

            foreach (var key in handle.Keys)
            {
                if (!loweredName.Contains(key))
                    continue;

                var id = loweredName.Split(':')[2];
                var loader = new ModLoaderInfo { LoaderType = handle[key].Item1, Version = handle[key].Item2(id) };

                if (enumed.Contains(loader)) break;

                yield return loader;
                enumed.Add(loader);

                break;
            }
        }
    }

    public static GameStatisticInfo GetStatisticInfo(this GameInfo gameInfo)
    {
        var libraryParser = new DefaultLibraryParser(gameInfo);
        libraryParser.EnumerateLibraries(out var enabledLibraries, out var enabledNativesLibraries);

        var assetParser = new DefaultAssetParser(gameInfo);

        long length = 0;
        int assets = 0;

        foreach (var library in enabledLibraries)
        {
            if (File.Exists(library.AbsolutePath))
                length += new FileInfo(library.AbsolutePath).Length;
        }

        foreach (var library in enabledNativesLibraries)
        {
            if (File.Exists(library.AbsolutePath))
                length += new FileInfo(library.AbsolutePath).Length;
        }

        if (File.Exists(gameInfo.AssetsIndexJsonPath))
        {
            foreach (var asset in assetParser.EnumerateAssets())
            {
                assets++;

                if (File.Exists(asset.AbsolutePath))
                    length += new FileInfo(asset.AbsolutePath).Length;
            }

            length += new FileInfo(gameInfo.AssetsIndexJsonPath).Length;
        }

        if (File.Exists(gameInfo.JarPath))
            length += new FileInfo(gameInfo.JarPath).Length;

        length += new FileInfo(gameInfo.VersionJsonPath).Length;

        return new GameStatisticInfo
        {
            AssetsCount = assets,
            LibrariesCount = enabledLibraries.Count,
            TotalSize = length,
            ModLoaders = gameInfo.GetModLoaders()
        };
    }

    public static void Delete(this GameInfo gameInfo)
    {
        var directory = new DirectoryInfo(Path.Combine(gameInfo.MinecraftFolderPath, "versions", gameInfo.AbsoluteId));

        if (directory.Exists)
            directory.DeleteAllFiles();

        directory.Delete();
    }
}
