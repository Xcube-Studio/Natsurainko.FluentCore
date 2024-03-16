using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement;

public enum MinecraftVersionType
{
    Release,
    PreRelease,
    Snapshot,
    OldBeta,
    OldAlpha,
    Other // Special versions or unknown versions
}

/// <summary>
/// Version of vanilla Minecraft
/// </summary>
/// <param name="Version">Release type of the version</param>
/// <param name="Type">Version ID</param>
public record struct MinecraftVersion(string Version, MinecraftVersionType Type)
{
    private static Regex _releaseRegex = new(@"^\d+\.\d+(\.\d+)?");
    private static Regex _preReleaseRegex = new(@"^\d+\.\d+(\.\d+)?-pre\d+");
    private static Regex _snapshotRegex = new(@"^\d{2}w\d{2}[a-z]");

    /// <summary>
    /// Parse a version ID read from client.json into a <see cref="MinecraftVersion"/>
    /// </summary>
    /// <param name="id">ID in client.json (example: 1.19.3)
    /// <para>Only matches the beginning, so modified versions such as 1.19.3-Fabric 0.14.12 are also supported</para>
    /// </param>
    /// <returns>The <see cref="MinecraftVersion"/> parsed</returns>
    public static MinecraftVersion Parse(string id)
    {
        // Math release versions
        var releaseMatch = _releaseRegex.Match(id);
        if (releaseMatch.Success)
            return new MinecraftVersion(releaseMatch.Value, MinecraftVersionType.Release);

        // Match pre-release versions
        var preReleaseMatch = _preReleaseRegex.Match(id);
        if (preReleaseMatch.Success)
            return new MinecraftVersion(preReleaseMatch.Value, MinecraftVersionType.PreRelease);

        // Match snapshot versions
        var snapshotMatch = _snapshotRegex.Match(id);
        if (snapshotMatch.Success)
            return new MinecraftVersion(snapshotMatch.Value, MinecraftVersionType.Snapshot);

        // Match old beta versions
        if (id.StartsWith("beta", StringComparison.OrdinalIgnoreCase))
            return new MinecraftVersion(id, MinecraftVersionType.OldBeta);

        // Match old alpha versions
        if (id.StartsWith("alpha", StringComparison.OrdinalIgnoreCase))
            return new MinecraftVersion(id, MinecraftVersionType.OldAlpha);

        // Special versions or unknown versions
        return new MinecraftVersion(id, MinecraftVersionType.Other);
    }
}
