using Natsurainko.FluentCore.Model.Download;
using Natsurainko.FluentCore.Model.Install;
using System.Collections.Generic;
using System.IO;

namespace Natsurainko.FluentCore.Interface;

public interface IGameCore
{
    DirectoryInfo Root { get; set; }

    FileResource ClientFile { get; set; }

    FileResource AssetIndexFile { get; set; }

    FileResource LogConfigFile { get; set; }

    List<LibraryResource> LibraryResources { get; set; }

    string MainClass { get; set; }

    IEnumerable<string> FrontArguments { get; set; }

    IEnumerable<string> BehindArguments { get; set; }

    string Id { get; set; }

    string Type { get; set; }

    int JavaVersion { get; set; }

    string InheritsFrom { get; set; }

    string Source { get; set; }

    bool IsVanilla { get; set; }

    IEnumerable<ModLoaderInformation> ModLoaders { get; set; }

    int LibrariesCount { get; set; }

    int AssetsCount { get; set; }

    long TotalSize { get; set; }
}
