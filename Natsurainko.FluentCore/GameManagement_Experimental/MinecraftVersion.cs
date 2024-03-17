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
/// <param name="VersionId">Version ID</param>
/// <param name="Type">Release type of the version</param>
public record struct MinecraftVersion(string VersionId, MinecraftVersionType Type)
{
    private static Regex _releaseRegex = new(@"^\d+\.\d+(\.\d+)?$");
    private static Regex _preReleaseRegex = new(@"^\d+\.\d+(\.\d+)?-pre\d+$");
    private static Regex _snapshotRegex = new(@"^\d{2}w\d{2}[a-z]$");

    /// <summary>
    /// Parse a version ID read from client.json into a <see cref="MinecraftVersion"/>
    /// </summary>
    /// <param name="id">ID in client.json (example: 1.19.3)
    /// </param>
    /// <returns>The <see cref="MinecraftVersion"/> parsed</returns>
    public static MinecraftVersion Parse(string id)
    {
        if (_releaseRegex.IsMatch(id))
            return new MinecraftVersion(id, MinecraftVersionType.Release);
        else if (_preReleaseRegex.IsMatch(id))
            return new MinecraftVersion(id, MinecraftVersionType.PreRelease);
        else if (_snapshotRegex.IsMatch(id))
            return new MinecraftVersion(id, MinecraftVersionType.Snapshot);
        else if (id.StartsWith("beta", StringComparison.OrdinalIgnoreCase))
            return new MinecraftVersion(id, MinecraftVersionType.OldBeta);
        else if (id.StartsWith("alpha", StringComparison.OrdinalIgnoreCase))
            return new MinecraftVersion(id, MinecraftVersionType.OldAlpha);
        else
            return new MinecraftVersion(id, MinecraftVersionType.Other);
    }
}
