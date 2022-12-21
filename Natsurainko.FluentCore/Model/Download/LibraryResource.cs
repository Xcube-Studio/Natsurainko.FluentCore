using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Service;
using Natsurainko.Toolkits.Network;
using Natsurainko.Toolkits.Network.Downloader;
using Natsurainko.Toolkits.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Natsurainko.FluentCore.Model.Download;

public class LibraryResource : IResource
{
    public DirectoryInfo Root { get; set; }

    public string Name { get; set; }

    public bool IsEnable { get; set; }

    public bool IsNatives { get; set; }

    public int Size { get; set; }

    public string CheckSum { get; set; }

    public string Url { get; set; }

    public DownloadRequest ToDownloadRequest()
    {
        var root = DownloadApiManager.Current.Libraries;

        foreach (var item in FormatName(Name))
            root = UrlExtension.Combine(root, item);

        if (!string.IsNullOrEmpty(Url))
        {
            if (!DownloadApiManager.Current.Host.Equals(DownloadApiManager.Mojang.Host))
                root = Url
                    .Replace(DownloadApiManager.Mojang.Libraries, DownloadApiManager.Current.Libraries)
                    .Replace(DownloadApiManager.ForgeLibraryUrlReplace)
                    .Replace(DownloadApiManager.FabricLibraryUrlReplace);
            else root = Url;
        }

        return new()
        {
            Directory = ToFileInfo().Directory,
            FileName = ToFileInfo().Name,
            FileSize = Size,
            Url = root
        };
    }

    public FileInfo ToFileInfo()
    {
        var root = Path.Combine(Root.FullName, "libraries");

        foreach (var item in FormatName(Name))
            root = Path.Combine(root, item);

        return new FileInfo(root);
    }

    public static IEnumerable<string> FormatName(string Name)
    {
        var extension = Name.Contains("@") ? Name.Split('@') : Array.Empty<string>();
        var subString = extension.Any()
            ? Name.Replace($"@{extension[1]}", string.Empty).Split(':')
            : Name.Split(':');

        foreach (string item in subString[0].Split('.'))
            yield return item;

        yield return subString[1];
        yield return subString[2];

        if (!extension.Any())
            yield return $"{subString[1]}-{subString[2]}{(subString.Length > 3 ? $"-{subString[3]}" : string.Empty)}.jar";
        else yield return $"{subString[1]}-{subString[2]}{(subString.Length > 3 ? $"-{subString[3]}" : string.Empty)}.jar".Replace("jar", extension[1]);
    }
}
