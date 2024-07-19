using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement;

public class MinecraftProfileBuilder
{
    private readonly string _minecraftFolderPath;

    public MinecraftProfileBuilder(string minecraftFolderPath)
    {
        _minecraftFolderPath = minecraftFolderPath;
    }

    public MinecraftProfile Build()
    {
        return new MinecraftProfile(_minecraftFolderPath);
    }
}