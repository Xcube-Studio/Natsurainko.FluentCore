using System.Collections.Generic;

namespace Nrk.FluentCore.GameManagement.Downloader;

public static class DownloadMirrors
{
    public static readonly IDownloadMirror BmclApi = new BmclApiMirror();
}

internal class BmclApiMirror : IDownloadMirror
{
    private static readonly Dictionary<string, string> _replacementMap = new()
    {
        { "https://resources.download.minecraft.net", "https://bmclapi2.bangbang93.com/assets" },
        { "https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com" },
        { "https://launchermeta.mojang.com", "https://bmclapi2.bangbang93.com" },
        { "https://launcher.mojang.com" , "https://bmclapi2.bangbang93.com" },
        { "https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/maven" },
        { "https://maven.minecraftforge.net", "https://bmclapi2.bangbang93.com/maven" },
        { "https://files.minecraftforge.net/maven", "https://bmclapi2.bangbang93.com/maven" },
        { "https://maven.fabricmc.net", "https://bmclapi2.bangbang93.com/maven" },
        { "https://meta.fabricmc.net", "https://bmclapi2.bangbang93.com/fabric-meta" },
        { "https://maven.neoforged.net/releases", "https://bmclapi2.bangbang93.com/maven" }
    };

    public string GetMirrorUrl(string sourceUrl)
    {
        foreach (var (src, mirror) in _replacementMap)
        {
            if (sourceUrl.StartsWith(src))
                return sourceUrl.Replace(src, mirror);
        }

        return sourceUrl;
    }
}
