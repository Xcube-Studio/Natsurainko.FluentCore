using Nrk.FluentCore.Management.Parsing;
using Nrk.FluentCore.Resources;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Management.ModLoaders;

public class ForgeInstaller : ModLoaderInstallerBase
{
    public required string JavaPath { get; set; }

    public required string PackageFilePath { get; set; }

    private bool _isLegacyForgeVersion = false;

    private readonly Dictionary<string, List<string>> _outputs = new();
    private readonly Dictionary<string, List<string>> _errorOutputs = new();

    public override Task<InstallResult> ExecuteAsync() => Task.Run(() =>
    {
        ParsePackage(
            out ZipArchive packageArchive,
            out JsonNode installProfile,
            out string forgeVersion,
            out JsonNode versionInfoJson,
            out List<LibraryElement> libraries,
            out IReadOnlyList<HighVersionForgeProcessorData>? highVersionForgeProcessors
        );
        WriteFiles(packageArchive, installProfile, forgeVersion, versionInfoJson);
        DownloadLibraries(libraries);

        if (_isLegacyForgeVersion)
        {
            OnProgressChanged(1.0);
            return;
        }

        RunProcessors(highVersionForgeProcessors);
        OnProgressChanged(1.0);

    }).ContinueWith(task =>
    {
        if (task.IsFaulted || _errorOutputs.Count > 0)
            return new InstallResult
            {
                Success = false,
                Exception = task.Exception,
                //Log = _errorOutputs
            };

        return new InstallResult
        {
            Success = true,
            Exception = null,
            //Log = null
        };
    });

    void ParsePackage(
        out ZipArchive packageArchive,
        out JsonNode installProfile,
        out string forgeVersion,
        out JsonNode versionInfoJson,
        out List<LibraryElement> libraries,
        out IReadOnlyList<HighVersionForgeProcessorData>? highVersionForgeProcessors)
    {
        highVersionForgeProcessors = null; // default output
        OnProgressChanged(0.1);

        packageArchive = ZipFile.OpenRead(PackageFilePath);
        installProfile = packageArchive
            .GetEntry("install_profile.json")?
            .ReadAsString()
            .ToJsonNode()
            ?? throw new Exception("Failed to parse install_profile.json");

        _isLegacyForgeVersion = installProfile["install"] != null;
        if (_isLegacyForgeVersion)
        {
            forgeVersion = installProfile["install"]?["version"]?
                .GetValue<string>()
                .Replace("forge ", string.Empty)
                ?? throw new Exception("Failed to parse forge version");
        }
        else
        {
            forgeVersion = installProfile["version"]?
                .GetValue<string>()
                .Replace("-forge-", "-")
                ?? throw new Exception("Failed to parse forge version");
        }

        if (_isLegacyForgeVersion)
        {
            versionInfoJson = installProfile["versionInfo"]
                ?? throw new Exception("Failed to parse forge version");
        }
        else
        {
            string versionJson = packageArchive.GetEntry("version.json")?.ReadAsString()
                ?? throw new Exception("Failed to read version.json");
            versionInfoJson = JsonNode.Parse(versionJson)
                ?? throw new Exception("Failed to parse version.json");
        }

        libraries = new List<LibraryElement>();
        if (versionInfoJson["libraries"]?.AsArray() is JsonArray arr)
        {
            var libs = DefaultLibraryParser.EnumerateLibrariesFromJsonArray(arr, InheritedFrom.MinecraftFolderPath);
            libraries.AddRange(libs);
        }

        foreach (var lib in libraries.Where(x => string.IsNullOrEmpty(x.Url)))
        {
            if (lib.RelativePath is null)
                throw new Exception("Library relative path is null");
            lib.Url = "https://bmclapi2.bangbang93.com/maven/" + lib.RelativePath.Replace("\\", "/");
        }

        if (_isLegacyForgeVersion)
            return;

        if (installProfile["libraries"]?.AsArray() is JsonArray profileLibArr)
        {
            var libs = DefaultLibraryParser.EnumerateLibrariesFromJsonArray(
                profileLibArr,
                InheritedFrom.MinecraftFolderPath
            );
            libraries.AddRange(libs);
        }

        var highVersionForgeDataDictionary = installProfile["data"]
            .Deserialize<Dictionary<string, Dictionary<string, string>>>()
            ?? throw new Exception("Failed to parse install profile data");

        if (highVersionForgeDataDictionary.Any())
        {
            highVersionForgeDataDictionary["BINPATCH"]["client"] = $"[net.minecraftforge:forge:{forgeVersion}:clientdata@lzma]";
            highVersionForgeDataDictionary["BINPATCH"]["server"] = $"[net.minecraftforge:forge:{forgeVersion}:serverdata@lzma]";
        }

        var mcVer = installProfile["minecraft"]?
            .GetValue<string>()
            ?? throw new Exception("Failed to parse Minecraft version");

        var jarPath = InheritedFrom.JarPath?.ToPathParameter()
            ?? throw new Exception("Failed to parse Minecraft jar path");

        var replaceValues = new Dictionary<string, string>
        {
            { "{SIDE}", "client" },
            { "{MINECRAFT_JAR}", jarPath },
            { "{MINECRAFT_VERSION}", mcVer },
            { "{ROOT}", InheritedFrom.MinecraftFolderPath.ToPathParameter() },
            { "{INSTALLER}", PackageFilePath.ToPathParameter() },
            { "{LIBRARY_DIR}", Path.Combine(InheritedFrom.MinecraftFolderPath, "libraries").ToPathParameter() }
        };

        var replaceProcessorArgs = highVersionForgeDataDictionary.ToDictionary(
            kvp => $"{{{kvp.Key}}}", kvp =>
            {
                var value = kvp.Value["client"];
                if (!value.StartsWith('[')) return value;

                return Path.Combine(
                    InheritedFrom.MinecraftFolderPath,
                    "libraries",
                    StringExtensions.FormatLibraryNameToRelativePath(value.TrimStart('[').TrimEnd(']')))
                    .ToPathParameter();
            });

        highVersionForgeProcessors = installProfile["processors"]?
            .Deserialize<IEnumerable<HighVersionForgeProcessorData>>()?
            .Where(x => !(x.Sides.Count == 1 && x.Sides.Contains("server")))
            .ToList();

        if (highVersionForgeProcessors is not null)
        {
            foreach (var processor in highVersionForgeProcessors)
            {
                processor.Args = processor.Args.Select(x =>
                {
                    if (x.StartsWith("["))
                        return Path.Combine(
                            InheritedFrom.MinecraftFolderPath,
                            "libraries",
                            StringExtensions.FormatLibraryNameToRelativePath(x.TrimStart('[').TrimEnd(']')))
                            .ToPathParameter();

                    return x.ReplaceFromDictionary(replaceProcessorArgs)
                        .ReplaceFromDictionary(replaceValues);
                });

                processor.Outputs = processor.Outputs.ToDictionary(
                    kvp => kvp.Key.ReplaceFromDictionary(replaceProcessorArgs),
                    kvp => kvp.Value.ReplaceFromDictionary(replaceProcessorArgs));
            }
        }

        OnProgressChanged(0.25);
    }

    void DownloadLibraries(List<LibraryElement> _libraries)
    {
        OnProgressChanged(0.3);

        var resourcesDownloader = new DefaultResourcesDownloader(InheritedFrom);
        resourcesDownloader.SetLibraryElements(_libraries);
        resourcesDownloader.SetDownloadMirror(DownloadMirrors.Bmclapi);

        resourcesDownloader.Download();

        OnProgressChanged(0.5);
    }

    void WriteFiles(ZipArchive _packageArchive, JsonNode _installProfile, string _forgeVersion, JsonNode _versionInfoJson)
    {
        OnProgressChanged(0.6);

        string forgeLibsFolder = Path.Combine(
            InheritedFrom.MinecraftFolderPath,
            "libraries\\net\\minecraftforge\\forge",
            _forgeVersion);

        if (_isLegacyForgeVersion)
        {
            var fileName = _installProfile["install"]?["filePath"]?.GetValue<string>()
                ?? throw new Exception("Failed to parse filename");
            var entry = _packageArchive.GetEntry(fileName)
                ?? throw new Exception("Failed to extract file");
            entry.ExtractTo(Path.Combine(forgeLibsFolder, fileName));
        }

        _packageArchive.GetEntry($"maven/net/minecraftforge/forge/{_forgeVersion}/forge-{_forgeVersion}.jar")?
            .ExtractTo(Path.Combine(forgeLibsFolder, $"forge-{_forgeVersion}.jar"));
        _packageArchive.GetEntry($"maven/net/minecraftforge/forge/{_forgeVersion}/forge-{_forgeVersion}-universal.jar")?
            .ExtractTo(Path.Combine(forgeLibsFolder, $"forge-{_forgeVersion}-universal.jar"));
        _packageArchive.GetEntry("data/client.lzma")?
            .ExtractTo(Path.Combine(forgeLibsFolder, $"forge-{_forgeVersion}-clientdata.lzma"));

        if (!string.IsNullOrEmpty(AbsoluteId))
            _versionInfoJson["id"] = AbsoluteId;

        string id = _versionInfoJson["id"]?.GetValue<string>()
            ?? throw new Exception("Failed to parse id");

        var jsonFile = new FileInfo(Path.Combine(
            InheritedFrom.MinecraftFolderPath,
            "versions",
            id,
            $"{id}.json"));

        if (jsonFile.Directory is null)
            throw new Exception("Failed to get json file directory");

        if (!jsonFile.Directory.Exists)
            jsonFile.Directory.Create();

        File.WriteAllText(jsonFile.FullName, _versionInfoJson.ToString());

        OnProgressChanged(0.75);
    }

    void RunProcessors(IReadOnlyList<HighVersionForgeProcessorData>? _highVersionForgeProcessors)
    {
        OnProgressChanged(0.8);

        if (_highVersionForgeProcessors is null)
            return;

        int index = 0;

        foreach (var processor in _highVersionForgeProcessors)
        {
            var fileName = Path.Combine(
                InheritedFrom.MinecraftFolderPath,
                "libraries",
                StringExtensions.FormatLibraryNameToRelativePath(processor.Jar));

            using var fileArchive = ZipFile.OpenRead(fileName);
            string? mainClass = fileArchive.GetEntry("META-INF/MANIFEST.MF")?
                .ReadAsString()
                .Split("\r\n".ToCharArray())
                .First(x => x.Contains("Main-Class: "))
                .Replace("Main-Class: ", string.Empty);
            if (mainClass is null)
                continue;

            string classPath = string.Join(Path.PathSeparator.ToString(), new List<string>() { fileName }
                .Concat(processor.Classpath.Select(x => Path.Combine(
                    InheritedFrom.MinecraftFolderPath,
                    "libraries",
                    StringExtensions.FormatLibraryNameToRelativePath(x)))));

            var args = new List<string>
            {
                "-cp",
                classPath.ToPathParameter(),
                mainClass
            };

            args.AddRange(processor.Args);

            using var process = Process.Start(new ProcessStartInfo(JavaPath)
            {
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                WorkingDirectory = InheritedFrom.MinecraftFolderPath,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }) ?? throw new Exception("Failed to start Java");

            var outputs = new List<string>();
            var errorOutputs = new List<string>();

            void AddOutput(string data, bool error = false)
            {
                if (string.IsNullOrEmpty(data))
                    return;

                outputs.Add(data);
                if (error) errorOutputs.Add(data);
            }

            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    AddOutput(args.Data);
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    AddOutput(args.Data, true);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            _outputs.Add($"{fileName}-{index}", outputs);

            if (errorOutputs.Any()) _errorOutputs.Add($"{fileName}-{index}", errorOutputs);

            index++;

            OnProgressChanged(0.8 + 0.2 * (index / (double)_highVersionForgeProcessors.Count));
        }
    }
}
