using Nrk.FluentCore.Management.Parsing;
using Nrk.FluentCore.Resources;
using Nrk.FluentCore.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Management.ModLoaders;

public class ForgeInstallExecutor : BaseInstallExecutor
{
    public required string JavaPath { get; set; }

    public required string PackageFilePath { get; set; }

    private ZipArchive _packageArchive;
    private bool _isLegacyForgeVersion = false;
    private string _forgeVersion;

    private JsonNode _installProfile;
    private JsonNode _versionInfoJson;
    private List<LibraryElement> _libraries;

    private IReadOnlyList<HighVersionForgeProcessorData> _highVersionForgeProcessors;

    private readonly Dictionary<string, List<string>> _outputs = new();
    private readonly Dictionary<string, List<string>> _errorOutputs = new();

    public override Task<InstallResult> ExecuteAsync() => Task.Run(() =>
    {
        ParsePackage();

        WriteFiles();

        DownloadLibraries();

        if (_isLegacyForgeVersion)
        {
            OnProgressChanged(1.0);
            return;
        }

        RunProcessors();

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


    void ParsePackage()
    {
        OnProgressChanged(0.1);

        _packageArchive = ZipFile.OpenRead(PackageFilePath);
        _installProfile = JsonNode.Parse(_packageArchive.GetEntry("install_profile.json").ReadAsString());
        _isLegacyForgeVersion = _installProfile["install"] != null;
        _forgeVersion = _isLegacyForgeVersion
            ? _installProfile["install"]["version"].GetValue<string>().Replace("forge ", string.Empty)
            : _installProfile["version"].GetValue<string>().Replace("-forge-", "-");

        _versionInfoJson = _isLegacyForgeVersion
            ? _installProfile["versionInfo"]
            : JsonNode.Parse(_packageArchive.GetEntry("version.json").ReadAsString());

        _libraries = new List<LibraryElement>(
            DefaultLibraryParser.EnumerateLibrariesFromJsonArray(
                _versionInfoJson["libraries"].AsArray(),
                InheritedFrom.MinecraftFolderPath));

        foreach (var lib in _libraries.Where(x => string.IsNullOrEmpty(x.Url)))
            lib.Url = "https://bmclapi2.bangbang93.com/maven/" + lib.RelativePath.Replace("\\", "/");

        if (_isLegacyForgeVersion)
            return;

        _libraries.AddRange(DefaultLibraryParser.EnumerateLibrariesFromJsonArray(
            _installProfile["libraries"].AsArray(),
            InheritedFrom.MinecraftFolderPath));

        var _highVersionForgeDataDictionary = _installProfile["data"].Deserialize<Dictionary<string, Dictionary<string, string>>>();

        if (_highVersionForgeDataDictionary.Any())
        {
            _highVersionForgeDataDictionary["BINPATCH"]["client"] = $"[net.minecraftforge:forge:{_forgeVersion}:clientdata@lzma]";
            _highVersionForgeDataDictionary["BINPATCH"]["server"] = $"[net.minecraftforge:forge:{_forgeVersion}:serverdata@lzma]";
        }

        var replaceValues = new Dictionary<string, string>
        {
            { "{SIDE}", "client" },
            { "{MINECRAFT_JAR}", InheritedFrom.JarPath.ToPathParameter() },
            { "{MINECRAFT_VERSION}", _installProfile["minecraft"].GetValue<string>() },
            { "{ROOT}", InheritedFrom.MinecraftFolderPath.ToPathParameter() },
            { "{INSTALLER}", PackageFilePath.ToPathParameter() },
            { "{LIBRARY_DIR}", Path.Combine(InheritedFrom.MinecraftFolderPath, "libraries").ToPathParameter() }
        };

        var replaceProcessorArgs = _highVersionForgeDataDictionary.ToDictionary(
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

        _highVersionForgeProcessors = _installProfile["processors"]
            .Deserialize<IEnumerable<HighVersionForgeProcessorData>>()
            .Where(x => !(x.Sides.Count == 1 && x.Sides.Contains("server")))
            .ToList();

        foreach (var processor in _highVersionForgeProcessors)
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

        OnProgressChanged(0.25);
    }

    void DownloadLibraries()
    {
        OnProgressChanged(0.3);

        var resourcesDownloader = new DefaultResourcesDownloader(InheritedFrom);
        resourcesDownloader.SetLibraryElements(_libraries);
        resourcesDownloader.SetDownloadMirror(DownloadMirrors.Bmclapi);

        resourcesDownloader.Download();

        OnProgressChanged(0.5);
    }

    void WriteFiles()
    {
        OnProgressChanged(0.6);

        string forgeLibsFolder = Path.Combine(
            InheritedFrom.MinecraftFolderPath,
            "libraries\\net\\minecraftforge\\forge",
            _forgeVersion);

        if (_isLegacyForgeVersion)
        {
            var fileName = _installProfile["install"]["filePath"].GetValue<string>();
            _packageArchive.GetEntry(fileName).ExtractTo(Path.Combine(forgeLibsFolder, fileName));
        }

        _packageArchive.GetEntry($"maven/net/minecraftforge/forge/{_forgeVersion}/forge-{_forgeVersion}.jar")?
            .ExtractTo(Path.Combine(forgeLibsFolder, $"forge-{_forgeVersion}.jar"));
        _packageArchive.GetEntry($"maven/net/minecraftforge/forge/{_forgeVersion}/forge-{_forgeVersion}-universal.jar")?
            .ExtractTo(Path.Combine(forgeLibsFolder, $"forge-{_forgeVersion}-universal.jar"));
        _packageArchive.GetEntry("data/client.lzma")?
            .ExtractTo(Path.Combine(forgeLibsFolder, $"forge-{_forgeVersion}-clientdata.lzma"));

        if (!string.IsNullOrEmpty(AbsoluteId))
            _versionInfoJson["id"] = AbsoluteId;

        var jsonFile = new FileInfo(Path.Combine(
            InheritedFrom.MinecraftFolderPath,
            "versions",
            _versionInfoJson["id"].GetValue<string>(),
            $"{_versionInfoJson["id"].GetValue<string>()}.json"));

        if (!jsonFile.Directory.Exists)
            jsonFile.Directory.Create();

        File.WriteAllText(jsonFile.FullName, _versionInfoJson.ToString());

        OnProgressChanged(0.75);
    }

    void RunProcessors()
    {
        OnProgressChanged(0.8);

        int index = 0;

        foreach (var processor in _highVersionForgeProcessors)
        {
            var fileName = Path.Combine(
                InheritedFrom.MinecraftFolderPath,
                "libraries",
                StringExtensions.FormatLibraryNameToRelativePath(processor.Jar));

            using var fileArchive = ZipFile.OpenRead(fileName);
            string mainClass = fileArchive.GetEntry("META-INF/MANIFEST.MF")
                .ReadAsString()
                .Split("\r\n".ToCharArray())
                .First(x => x.Contains("Main-Class: "))
                .Replace("Main-Class: ", string.Empty);

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
            });

            var outputs = new List<string>();
            var errorOutputs = new List<string>();

            void AddOutput(string data, bool error = false)
            {
                if (string.IsNullOrEmpty(data))
                    return;

                outputs.Add(data);
                if (error) errorOutputs.Add(data);
            }

            process.OutputDataReceived += (_, args) => AddOutput(args.Data);
            process.ErrorDataReceived += (_, args) => AddOutput(args.Data, true);

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
