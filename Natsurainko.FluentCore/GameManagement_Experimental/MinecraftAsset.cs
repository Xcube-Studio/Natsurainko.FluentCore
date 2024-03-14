using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement;

public class MinecraftAsset
{
    public required string Name { get; init; }

    public required string Hash { get; init; }

    public required int Size { get; init; }
}