using Nrk.FluentCore.Classes.Datas.Install;
using Nrk.FluentCore.Classes.Datas.Parse;
using Nrk.FluentCore.Components.Install;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nrk.FluentCore.DefaultComponents.Install;

public class OptiFineInstallExecutor : BaseInstallExecutor
{
    public required string JavaPath { get; set; }

    public required string PackageFilePath { get; set; }

    private ZipArchive _packageArchive;
    private string _launchwrapper;
    private string _packageMcVersion;
    private string _packagePatch;
    private string _optifineLibraryPath;

    private readonly List<string> _outputs = new();
    private readonly List<string> _errorOutputs = new();

    public override Task<InstallResult> ExecuteAsync() => Task.Run(() =>
    {
        ParsePackage();

        WriteFiles();

        RunProcessor();

        OnProgressChanged(1.0);

    }).ContinueWith(task =>
    {
        if (task.IsFaulted || _errorOutputs.Count > 0)
            return new InstallResult
            {
                Success = false,
                Exception = task.Exception,
                Log = _errorOutputs
            };

        return new InstallResult
        {
            Success = true,
            Exception = null,
            Log = _outputs
        };
    });

    private void ParsePackage()
    {
        OnProgressChanged(0.1);

        _packageArchive = ZipFile.OpenRead(this.PackageFilePath);
        _launchwrapper = _packageArchive.GetEntry("launchwrapper-of.txt")?.ReadAsString() ?? "1.12";

        var changelogLine = _packageArchive.GetEntry("changelog.txt")?.ReadAsString().Split("\r\n")[0];
        var rawPatch = changelogLine[9..].Split('_');

        _packageMcVersion = rawPatch[0];
        _packagePatch = changelogLine[9..][(_packageMcVersion.Length + 1)..];

        OnProgressChanged(0.2);
    }

    private void WriteFiles()
    {
        OnProgressChanged(0.4);

        var time = DateTime.Now.ToString("s");

        var jsonEntity = new
        {
            id = AbsoluteId ?? $"{_packageMcVersion}-OptiFine-{_packagePatch}",
            inheritsFrom = _packageMcVersion,
            time,
            releaseTime = time,
            type = "release",
            libraries = new LibraryJsonNode[]
            {
                new () { Name = $"optifine:Optifine:{_packageMcVersion}_{_packagePatch}" },
                new () { Name = _launchwrapper.Equals("1.12") ? "net.minecraft:launchwrapper:1.12" : $"optifine:launchwrapper-of:{_launchwrapper}" }
            },
            mainClass = "net.minecraft.launchwrapper.Launch",
            minecraftArguments = "  --tweakClass optifine.OptiFineTweaker"
        };

        var jsonFilePath = Path.Combine(InheritedFrom.MinecraftFolderPath, "versions", jsonEntity.id, $"{jsonEntity.id}.json");
        var jarFilePath = Path.Combine(InheritedFrom.MinecraftFolderPath, "versions", jsonEntity.id, $"{jsonEntity.id}.jar");
        var launchwrapperFile = Path.Combine(
                InheritedFrom.MinecraftFolderPath,
                "libraries",
                StringExtensions.FormatLibraryNameToRelativePath(jsonEntity.libraries[1].Name));

        _optifineLibraryPath = Path.Combine(
                InheritedFrom.MinecraftFolderPath,
                "libraries",
                StringExtensions.FormatLibraryNameToRelativePath(jsonEntity.libraries[0].Name));

        foreach (var path in new string[] { jsonFilePath, launchwrapperFile, _optifineLibraryPath })
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        File.WriteAllText(Path.Combine(jsonFilePath),
            JsonSerializer.Serialize(jsonEntity, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }));

        _packageArchive.GetEntry($"launchwrapper-of-{_launchwrapper}.jar").ExtractToFile(launchwrapperFile, true);

        File.Copy(InheritedFrom.JarPath, jarFilePath, true);

        OnProgressChanged(0.6);
    }

    private void RunProcessor()
    {
        OnProgressChanged(0.65);

        using var process = Process.Start(new ProcessStartInfo(JavaPath)
        {
            UseShellExecute = false,
            WorkingDirectory = this.InheritedFrom.MinecraftFolderPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            Arguments = string.Join(" ", new string[]
            {
                "-cp",
                PackageFilePath.ToPathParameter(),
                "optifine.Patcher",
                this.InheritedFrom.JarPath.ToPathParameter(),
                PackageFilePath.ToPathParameter(),
                _optifineLibraryPath.ToPathParameter()
            })
        });

        void AddOutput(string data, bool error = false)
        {
            if (string.IsNullOrEmpty(data))
                return;

            _outputs.Add(data);
            if (error) _errorOutputs.Add(data);
        }

        process.OutputDataReceived += (_, args) => AddOutput(args.Data);
        process.ErrorDataReceived += (_, args) => AddOutput(args.Data, true);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        OnProgressChanged(0.8);
    }
}
