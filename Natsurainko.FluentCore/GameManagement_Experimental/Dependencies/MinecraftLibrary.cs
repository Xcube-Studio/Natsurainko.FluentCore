using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement;

public class MinecraftLibrary
{
    public required string Domain { get; init; } = "";

    public required string Name { get; init; } = "";

    public required string Version { get; init; } = "";

    public string? Classifier { get; init; }


    public MinecraftLibrary() { }

    /// <summary>
    /// Parse a library from the full name of a Java library
    /// </summary>
    /// <remarks>If <paramref name="packageName"/> is not a Java library name, then it is set for <see cref="Name"/> and other fields are <see cref="string.Empty"/></remarks>
    /// <param name="packageName">Full library name in the format of DOMAIN:NAME:VER:CLASSIFIER</param>
    public MinecraftLibrary(string packageName)
    {
        Regex regex = new(@"^(?<domain>[^:]+):(?<name>[^:]+):(?<version>[^:]+)(?::(?<classifier>[^:]+))?");
        Match match = regex.Match(packageName);

        if (!match.Success)
        {
            Name = packageName;
            return;
        }

        Domain = match.Groups["domain"].Value;
        Name = match.Groups["name"].Value;
        Version = match.Groups["version"].Value;
        if (match.Groups["classifier"].Success)
            Classifier = match.Groups["classifier"].Value;
    }
}
