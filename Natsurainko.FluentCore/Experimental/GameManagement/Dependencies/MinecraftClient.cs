using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public class MinecraftClient : MinecraftDependency
{
    /// <inheritdoc/>
    public override string FilePath => Path.Combine("versions", ClientId, $"{ClientId}.jar");

    /// <inheritdoc/>
    public override string Url => _url;

    private readonly string _url;

    public required string ClientId { get; init; }

    public MinecraftClient(string url)
    {
        _url = url;
    }
}
