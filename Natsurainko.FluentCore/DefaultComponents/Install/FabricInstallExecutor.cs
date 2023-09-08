using Nrk.FluentCore.Classes.Datas.Install;
using Nrk.FluentCore.Classes.Datas.Parse;
using Nrk.FluentCore.Components.Install;
using Nrk.FluentCore.DefaultComponents.Download;
using Nrk.FluentCore.DefaultComponents.Parse;
using Nrk.FluentCore.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.DefaultComponents.Install;

public class FabricInstallExecutor : BaseInstallExecutor
{
    public required FabricInstallBuild FabricBuild { get; set; }

    private JsonNode _versionInfoJson;
    private IEnumerable<LibraryElement> _libraries;

    public override Task<InstallResult> ExecuteAsync() => Task.Run(() =>
    {
        ParseBuild();

        DownloadLibraries();

        WriteFiles();

        OnProgressChanged(1.0);

    }).ContinueWith(task =>
    {
        if (task.IsFaulted)
            return new InstallResult
            {
                Success = false,
                Exception = task.Exception,
                Log = null
            };

        return new InstallResult
        {
            Success = true,
            Exception = null,
            Log = null
        };
    });

    void ParseBuild()
    {
        var responseMessage = HttpUtils.HttpGet($"https://meta.fabricmc.net/v2/versions/loader/{InheritedFrom.AbsoluteId}/{QuiltBuild.BuildVersion}/profile/json");
        _versionInfoJson = JsonNode.Parse(responseMessage.Content.ReadAsString());

        _libraries = DefaultLibraryParser.EnumerateLibrariesFromJsonArray(_versionInfoJson["libraries"].AsArray(), InheritedFrom.MinecraftFolderPath);
    }

    void DownloadLibraries()
    {
        var resourcesDownloader = new DefaultResourcesDownloader(InheritedFrom);
        resourcesDownloader.SetLibraryElements(_libraries);

        resourcesDownloader.Download();
    }

    void WriteFiles()
    {
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
            "-mcversion", InheritedFrom.AbsoluteId.ToPathParameter()
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
            Arguments = string.Join(' ', args)
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
