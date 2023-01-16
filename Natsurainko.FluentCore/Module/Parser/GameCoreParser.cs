using Natsurainko.FluentCore.Model.Download;
using Natsurainko.FluentCore.Model.Install;
using Natsurainko.FluentCore.Model.Launch;
using Natsurainko.FluentCore.Model.Parser;
using Natsurainko.FluentCore.Service;
using Natsurainko.Toolkits.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Natsurainko.FluentCore.Module.Parser;

public class GameCoreParser
{
    public DirectoryInfo Root { get; set; }

    public IEnumerable<VersionJsonEntity> JsonEntities { get; set; }

    public GameCoreParser(DirectoryInfo root, IEnumerable<VersionJsonEntity> jsonEntities)
    {
        Root = root;
        JsonEntities = jsonEntities;
    }

    public List<(string, Exception)> ErrorGameCores { get; private set; } = new();

    public virtual IEnumerable<GameCore> GetGameCores()
    {
        var cores = new List<GameCore>();

        foreach (var entity in JsonEntities)
        {
            try
            {
                var core = new GameCore
                {
                    Id = entity.Id,
                    Type = entity.Type,
                    MainClass = entity.MainClass,
                    InheritsFrom = entity.InheritsFrom,
                    JavaVersion = (int)(entity.JavaVersion?.MajorVersion),
                    LibraryResources = new LibraryParser(entity.Libraries, Root).GetLibraries().ToList(),
                    Root = Root
                };

                if (string.IsNullOrEmpty(entity.InheritsFrom) && entity.Downloads != null)
                    core.ClientFile = GetClientFile(entity);

                if (string.IsNullOrEmpty(entity.InheritsFrom) && entity.Logging != null && entity.Logging.Client != null)
                    core.LogConfigFile = GetLogConfigFile(entity);

                if (string.IsNullOrEmpty(entity.InheritsFrom) && entity.AssetIndex != null)
                    core.AssetIndexFile = GetAssetIndexFile(entity);

                if (entity.MinecraftArguments != null)
                    core.BehindArguments = HandleMinecraftArguments(entity.MinecraftArguments);

                if (entity.Arguments != null && entity.Arguments.Game != null)
                    core.BehindArguments = core.BehindArguments == null
                        ? HandleArgumentsGame(entity.Arguments)
                        : core.BehindArguments.Union(HandleArgumentsGame(entity.Arguments));

                if (entity.Arguments != null && entity.Arguments.Jvm != null)
                    core.FrontArguments = HandleArgumentsJvm(entity.Arguments);
                else core.FrontArguments = new string[]
                {
                    "-Djava.library.path=${natives_directory}",
                    "-Dminecraft.launcher.brand=${launcher_name}",
                    "-Dminecraft.launcher.version=${launcher_version}",
                    "-cp ${classpath}"
                };

                cores.Add(core);
            }
            catch (Exception ex) { ErrorGameCores.Add((entity.Id, ex)); }
        }

        foreach (var item in cores)
        {
            item.Source = GetSource(item);

            if (!string.IsNullOrEmpty(item.InheritsFrom))
            {
                GameCore inheritsFrom = default;

                foreach (var subitem in cores)
                    if (subitem.Id == item.InheritsFrom)
                        inheritsFrom = subitem;

                if (inheritsFrom != null)
                {
                    item.AssetIndexFile = inheritsFrom.AssetIndexFile;
                    item.ClientFile = inheritsFrom.ClientFile;
                    item.LogConfigFile = inheritsFrom.LogConfigFile;
                    item.JavaVersion = inheritsFrom.JavaVersion;
                    item.Type = inheritsFrom.Type;
                    item.LibraryResources = item.LibraryResources.Union(inheritsFrom.LibraryResources).ToList();
                    item.BehindArguments = inheritsFrom.BehindArguments.Union(item.BehindArguments).ToList();
                    item.FrontArguments = item.FrontArguments.Union(inheritsFrom.FrontArguments).ToList();
                }
                else continue;
            }

            item.ModLoaders = GetModLoaders(item);
            item.IsVanilla = GetIsVanilla(item);

            yield return item;
        }
    }

    protected FileResource GetClientFile(VersionJsonEntity entity)
    {
        var path = Path.Combine(Root.FullName, "versions", entity.Id, $"{entity.Id}.jar");

        return new FileResource
        {
            CheckSum = entity.Downloads["client"].Sha1,
            Size = entity.Downloads["client"].Size,
            Url = DownloadApiManager.Current != DownloadApiManager.Mojang ? entity.Downloads["client"].Url.Replace("https://launcher.mojang.com", DownloadApiManager.Current.Host) : entity.Downloads["client"].Url,
            Root = Root,
            FileInfo = new FileInfo(path),
            Name = Path.GetFileName(path)
        };
    }

    protected FileResource GetLogConfigFile(VersionJsonEntity entity)
    {
        var path = Path.Combine(Root.FullName, "versions", entity.Id, entity.Logging.Client.File.Id);

        return new FileResource
        {
            CheckSum = entity.Logging.Client.File.Sha1,
            Size = entity.Logging.Client.File.Size,
            Url = DownloadApiManager.Current != DownloadApiManager.Mojang ? entity.Logging.Client.File.Url.Replace("https://launcher.mojang.com", DownloadApiManager.Current.Host) : entity.Logging.Client.File.Url,
            Name = entity.Logging.Client.File.Id,
            FileInfo = new FileInfo(path),
            Root = Root,
        };
    }

    protected FileResource GetAssetIndexFile(VersionJsonEntity entity)
    {
        var path = Path.Combine(Root.FullName, "assets", "indexes", $"{entity.AssetIndex.Id}.json");

        return new FileResource
        {
            CheckSum = entity.AssetIndex.Sha1,
            Size = entity.AssetIndex.Size,
            Url = DownloadApiManager.Current != DownloadApiManager.Mojang ? entity.AssetIndex.Url.Replace("https://launchermeta.mojang.com", DownloadApiManager.Current.Host).Replace("https://piston-meta.mojang.com", DownloadApiManager.Current.Host) : entity.AssetIndex.Url,
            Name = $"{entity.AssetIndex.Id}.json",
            FileInfo = new FileInfo(path),
            Root = Root,
        };
    }

    protected static string GetSource(GameCore core)
    {
        try
        {
            if (core.InheritsFrom != null)
                return core.InheritsFrom;

            var json = Path.Combine(core.Root.FullName, "versions", core.Id, $"{core.Id}.json");

            if (File.Exists(json))
            {
                var entity = JObject.Parse(File.ReadAllText(json));

                if (entity.ContainsKey("patches"))
                    return ((JArray)entity["patches"])[0]["version"].ToString();

                if (entity.ContainsKey("clientVersion"))
                    return entity["clientVersion"].ToString();
            }
        }
        catch //(Exception ex)
        {
            //throw;
        }

        return core.Id;
    }

    protected static bool GetIsVanilla(GameCore core)
    {
        if (core.ModLoaders.Any())
            return false;

        foreach (var arg in core.BehindArguments)
            switch (arg)
            {
                case "--tweakClass optifine.OptiFineTweaker":
                case "--tweakClass net.minecraftforge.fml.common.launcher.FMLTweaker":
                case "--fml.forgeGroup net.minecraftforge":
                    return false;
            }

        foreach (var arg in core.FrontArguments)
            if (arg.Contains("-DFabricMcEmu= net.minecraft.client.main.Main"))
                return false;

        return core.MainClass switch
        {
            "net.minecraft.client.main.Main" or "net.minecraft.launchwrapper.Launch" or "com.mojang.rubydung.RubyDung" => true,
            _ => false,
        };
    }

    protected static IEnumerable<ModLoaderInformation> GetModLoaders(GameCore core)
    {
        var libFind = core.LibraryResources.Where(lib => HandleModLoaderDictionary.Where(kvp => lib.Name.ToLower().Contains(kvp.Key)).Any());

        foreach (var lib in libFind)
        {
            var lowerName = lib.Name.ToLower();
            var id = lib.Name.Split(':')[2];

            var handle = HandleModLoaderDictionary.Where(kvp => lowerName.Contains(kvp.Key)).First();
            yield return new() { LoaderType = handle.Value.Item1, Version = handle.Value.Item2(id) };
        }
    }

    protected static IEnumerable<string> HandleMinecraftArguments(string minecraftArguments) => ArgumnetsGroup(minecraftArguments.Replace("  ", " ").Split(' '));

    protected static IEnumerable<string> HandleArgumentsGame(ArgumentsJsonEntity entity) => ArgumnetsGroup(entity.Game.Where(x => x.Type == JTokenType.String).Select(x => x.ToString().ToPath()));

    protected static IEnumerable<string> HandleArgumentsJvm(ArgumentsJsonEntity entity) => ArgumnetsGroup(entity.Jvm.Where(x => x.Type == JTokenType.String).Select(x => x.ToString().ToPath()));

    protected static IEnumerable<string> ArgumnetsGroup(IEnumerable<string> vs)
    {
        var cache = new List<string>();

        foreach (var item in vs)
        {
            if (cache.Any() && cache[0].StartsWith("-") && item.StartsWith("-"))
            {
                yield return cache[0].Trim(' ');

                cache = new List<string> { item };
            }
            else if (vs.Last() == item && !cache.Any())
                yield return item.Trim(' ');
            else cache.Add(item);

            if (cache.Count == 2)
            {
                yield return string.Join(" ", cache).Trim(' ');
                cache = new List<string>();
            }
        }
    }

    public static Dictionary<string, (ModLoaderType, Func<string, string>)> HandleModLoaderDictionary { get; } = new() 
    {
        { "net.minecraftforge:forge:", (ModLoaderType.Forge, libVersion => libVersion.Split('-')[1]) },
        { "net.minecraftforge:fmlloader:", (ModLoaderType.Forge, libVersion => libVersion.Split('-')[1]) },
        { "optifine:optifine", (ModLoaderType.OptiFine, libVersion => libVersion.Substring(libVersion.IndexOf('_') + 1)) },
        { "net.fabricmc:fabric-loader", (ModLoaderType.Fabric, libVersion => libVersion) },
        { "com.mumfrey:liteloader:", (ModLoaderType.LiteLoader, libVersion => libVersion) },
        { "org.quiltmc:quilt-loader:", (ModLoaderType.Quilt, libVersion => libVersion) },
    };
}
