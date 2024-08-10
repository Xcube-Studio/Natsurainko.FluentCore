using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Management.Parsing;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.ModLoaders.Fabric;

public class FabricInstaller : ModLoaderInstaller
{
    public required FabricInstallBuild FabricBuild { get; set; }

    private readonly IDownloader _downloader;

    public FabricInstaller(IDownloader downloader)
    {
        _downloader = downloader;
    }

    public override Task<InstallationResult> ExecuteAsync() => Task.Run(() =>
        {
            ParseBuild(out JsonNode versionInfoJson, out IEnumerable<LibraryElement> libraries);

            DownloadLibraries(libraries);

            WriteFiles(versionInfoJson);

            OnProgressChanged(1.0);
        })
        .ContinueWith(task =>
        {
            if (task.IsFaulted)
                return new InstallationResult
                {
                    Success = false,
                    Exception = task.Exception,
                    Log = null
                };

            return new InstallationResult
            {
                Success = true,
                Exception = null,
                Log = null
            };
        });

    void ParseBuild(out JsonNode versionInfoJson, out IEnumerable<LibraryElement> libraries)
    {
        var responseMessage = HttpUtils.HttpGet(
            $"https://meta.fabricmc.net/v2/versions/loader/{InheritedInstance.VersionFolderName}/{FabricBuild.BuildVersion}/profile/json"
        );
        versionInfoJson =
            JsonNode.Parse(responseMessage.Content.ReadAsString())
            ?? throw new InvalidDataException("Error in parsing version info json");

        var librariesJson = versionInfoJson["libraries"];
        if (librariesJson is null)
            throw new InvalidDataException("Error in parsing version info json");

        libraries = DefaultLibraryParser.EnumerateLibrariesFromJsonArray(
            librariesJson.AsArray(),
            InheritedInstance.MinecraftFolderPath
        );
    }

    void DownloadLibraries(IEnumerable<LibraryElement> libraries)
    {
        var libs = libraries.Select(lib => (lib.Url!, lib.AbsolutePath));
        _downloader.DownloadFilesAsync(libs).GetAwaiter().GetResult();
    }

    void WriteFiles(JsonNode versionInfoJson)
    {
        if (!string.IsNullOrEmpty(AbsoluteId))
            versionInfoJson["id"] = AbsoluteId;

        var id = versionInfoJson["id"];
        if (id is null)
            throw new InvalidDataException("Error in parsing version info json");

        var jsonFile = new FileInfo(
            Path.Combine(
                InheritedInstance.MinecraftFolderPath,
                "versions",
                id.GetValue<string>(),
                $"{id.GetValue<string>()}.json"
            )
        );

        if (jsonFile.Directory is not null && !jsonFile.Directory.Exists)
            jsonFile.Directory.Create();

        File.WriteAllText(jsonFile.FullName, versionInfoJson.ToString());
    }

    /*
    private void RunProcessor()
    {
        OnProgressChanged(0.35);

        var args = new List<string>()
        {
            "-jar", PackageFilePath.ToPathParameter(),
            "client",
            "-dir", InheritedFrom.MinecraftFolderPath.ToPathParameter(),
            "-mcversion", InheritedFrom.Id.ToPathParameter()
        };

        if (InheritedFrom.Type.Equals("snapshot"))
            args.Add("-snapshot");

        if (MirrorSource == DownloadMirrors.Mcbbs || MirrorSource == DownloadMirrors.Bmclapi)
        {
            args.Add("-mavenurl");
            args.Add(MirrorSource.LibrariesReplaceUrl["https://maven.fabricmc.net"] + "/");
        }

        using var process = Process.Start(new ProcessStartInfo(JavaPath)
        {
            UseShellExecute = false,
            WorkingDirectory = this.InheritedFrom.MinecraftFolderPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            ArgumentsJsonObject = string.Join(' ', args)
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

        OnProgressChanged(0.9);
    }
    */
}
