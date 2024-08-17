using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.ModLoaders.OptiFine;

public class OptiFineInstaller : ModLoaderInstaller
{
    public required string JavaPath { get; set; }

    public required string PackageFilePath { get; set; }

    private readonly List<string> _outputs = new();
    private readonly List<string> _errorOutputs = new();

    public override Task<InstallationResult> ExecuteAsync() =>
        Task.Run(() =>
            {
                ParsePackage(
                    out ZipArchive packageArchive,
                    out string launchwrapper,
                    out string packageMcVersion,
                    out string packagePatch
                );

                WriteFiles(
                    packageArchive,
                    launchwrapper,
                    packageMcVersion,
                    packagePatch,
                    out string optifineLibraryPath
                );

                RunProcessor(optifineLibraryPath);

                OnProgressChanged(1.0);
            })
            .ContinueWith(task =>
            {
                if (task.IsFaulted || _errorOutputs.Count > 0)
                    return new InstallationResult
                    {
                        Success = false,
                        Exception = task.Exception,
                        Log = _errorOutputs
                    };

                return new InstallationResult
                {
                    Success = true,
                    Exception = null,
                    Log = _outputs
                };
            });

    private void ParsePackage(
        out ZipArchive packageArchive,
        out string launchwrapper,
        out string packageMcVersion,
        out string packagePatch
    )
    {
        OnProgressChanged(0.1);

        packageArchive = ZipFile.OpenRead(PackageFilePath);
        launchwrapper = packageArchive.GetEntry("launchwrapper-of.txt")?.ReadAsString() ?? "1.12";

        var changelogLine = packageArchive.GetEntry("changelog.txt")?.ReadAsString().Split("\r\n")[0];
        if (changelogLine is null)
            throw new Exception("Invalid OptiFine package");

        var rawPatch = changelogLine[9..].Split('_');
        packageMcVersion = rawPatch[0];
        packagePatch = changelogLine[9..][(packageMcVersion.Length + 1)..];

        OnProgressChanged(0.2);
    }

    private void WriteFiles(
        ZipArchive packageArchive,
        string launchwrapper,
        string packageMcVersion,
        string packagePatch,
        out string optifineLibraryPath
    )
    {
        OnProgressChanged(0.4);

        var time = DateTime.Now.ToString("s");

        var jsonEntity = new
        {
            id = AbsoluteId ?? $"{packageMcVersion}-OptiFine-{packagePatch}",
            inheritsFrom = packageMcVersion,
            time,
            releaseTime = time,
            type = "release",
            libraries = new LibraryJsonNode[]
            {
                new() { Name = $"optifine:Optifine:{packageMcVersion}_{packagePatch}" },
                new()
                {
                    Name = launchwrapper.Equals("1.12")
                        ? "net.minecraft:launchwrapper:1.12"
                        : $"optifine:launchwrapper-of:{launchwrapper}"
                }
            },
            mainClass = "net.minecraft.launchwrapper.Launch",
            minecraftArguments = "  --tweakClass optifine.OptiFineTweaker"
        };

        var jsonFilePath = Path.Combine(
            InheritedInstance.MinecraftFolderPath,
            "versions",
            jsonEntity.id,
            $"{jsonEntity.id}.json"
        );
        var jarFilePath = Path.Combine(
            InheritedInstance.MinecraftFolderPath,
            "versions",
            jsonEntity.id,
            $"{jsonEntity.id}.jar"
        );
        var launchwrapperFile = Path.Combine(
            InheritedInstance.MinecraftFolderPath,
            "libraries",
            StringExtensions.FormatLibraryNameToRelativePath(jsonEntity.libraries[1].Name)
        );

        optifineLibraryPath = Path.Combine(
            InheritedInstance.MinecraftFolderPath,
            "libraries",
            StringExtensions.FormatLibraryNameToRelativePath(jsonEntity.libraries[0].Name)
        );

        foreach (var path in new string[] { jsonFilePath, launchwrapperFile, optifineLibraryPath })
        {
            var dir = Path.GetDirectoryName(path); // TODO: This may return null if path is a root directory
            if (dir is null)
                throw new Exception("Invalid path");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        File.WriteAllText(
            Path.Combine(jsonFilePath),
            JsonSerializer.Serialize(
                jsonEntity,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }
            )
        );

        var jar = packageArchive.GetEntry($"launchwrapper-of-{launchwrapper}.jar");
        if (jar is null)
            throw new Exception("Invalid OptiFine package");
        jar.ExtractToFile(launchwrapperFile, true);

        if (InheritedInstance.ClientJarPath is null || !File.Exists(InheritedInstance.ClientJarPath))
            throw new Exception("Invalid Minecraft jar");

        File.Copy(InheritedInstance.ClientJarPath, jarFilePath, true);

        OnProgressChanged(0.6);
    }

    private void RunProcessor(string optifineLibraryPath)
    {
        OnProgressChanged(0.65);
        if (InheritedInstance.ClientJarPath is null || !File.Exists(InheritedInstance.ClientJarPath))
            throw new Exception("Invalid Minecraft jar");

        using var process = Process.Start(
            new ProcessStartInfo(JavaPath)
            {
                UseShellExecute = false,
                WorkingDirectory = InheritedInstance.MinecraftFolderPath,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = string.Join(
                    " ",
                    new string[]
                    {
                        "-cp",
                        PackageFilePath.ToPathParameter(),
                        "optifine.Patcher",
                        InheritedInstance.ClientJarPath.ToPathParameter(),
                        PackageFilePath.ToPathParameter(),
                        optifineLibraryPath.ToPathParameter()
                    }
                )
            }
        );

        if (process is null)
            throw new Exception("Failed to install OptiFine");

        void AddOutput(string data, bool error = false)
        {
            if (string.IsNullOrEmpty(data))
                return;

            _outputs.Add(data);
            if (error)
                _errorOutputs.Add(data);
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

        OnProgressChanged(0.8);
    }
}

internal record LibraryJsonNode
{
    [JsonPropertyName("downloads")]
    public DownloadsJsonNode? Downloads { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("natives")]
    public Dictionary<string, string>? Natives { get; set; }
}

internal record DownloadsJsonNode
{
    [JsonPropertyName("artifact")]
    public FileJsonNode? Artifact { get; init; }

    [JsonPropertyName("classifiers")]
    public Dictionary<string, FileJsonNode>? Classifiers { get; init; }

}

internal record FileJsonNode
{
    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; init; }

    [JsonPropertyName("size")]
    public int? Size { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    //for client-x.xx.xml
    [JsonPropertyName("id")]
    public string? Id { get; init; }
}