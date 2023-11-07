using Nrk.FluentCore.Classes.Datas.Install;
using Nrk.FluentCore.DefaultComponents.Download;
using Nrk.FluentCore.GameResources.Parsing;
using Nrk.FluentCore.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameResources.ModLoaders;

public class QuiltInstallExecutor : BaseInstallExecutor
{
    public required QuiltInstallBuild QuiltBuild { get; set; }

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
        var responseMessage = HttpUtils.HttpGet($"https://meta.quiltmc.org/v3/versions/loader/{InheritedFrom.AbsoluteId}/{QuiltBuild.BuildVersion}/profile/json");
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
}
