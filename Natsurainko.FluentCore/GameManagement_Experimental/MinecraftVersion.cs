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

public record struct MinecraftVersion
{
    public MinecraftVersionType Type { get; init; }
    public string Version { get; init; }

    private static Regex _releaseRegex = new(@"^\d+\.\d+(\.\d+)?$");
    private static Regex _preReleaseRegex = new(@"^\d+\.\d+(\.\d+)?-pre\d+$");
    private static Regex _snapshotRegex = new(@"^\d{2}w\d{2}[a-z]$");

    public MinecraftVersion(string id)
    {
        Version = id;
        if (_releaseRegex.IsMatch(id))
            Type = MinecraftVersionType.Release;
        else if (_preReleaseRegex.IsMatch(id))
            Type = MinecraftVersionType.PreRelease;
        else if (_snapshotRegex.IsMatch(id))
            Type = MinecraftVersionType.Snapshot;
        else if (id.StartsWith("beta", StringComparison.OrdinalIgnoreCase))
            Type = MinecraftVersionType.OldBeta;
        else if (id.StartsWith("alpha", StringComparison.OrdinalIgnoreCase))
            Type = MinecraftVersionType.OldAlpha;
        else
            Type = MinecraftVersionType.Other;
    }

    public MinecraftVersion(string version, MinecraftVersionType type)
    {
        Type = type;
        Version = version;
    }
}
