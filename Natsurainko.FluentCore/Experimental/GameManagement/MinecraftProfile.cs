using Nrk.FluentCore.GameManagement;
using Nrk.FluentCore.Management.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement;

public class MinecraftProfile
{
    public string MinecraftFolderPath { get; init; }

    // Vanilla items
    // TODO: Consider lazy loading
    public IList<MinecraftInstance> Instances { get; init; } = [];

    public IList<object> Assets { get; init; } = [];

    public IList<object> Libraries { get; init; } = [];

    public IList<object> DataPacks => throw new NotImplementedException();

    // Non-vanilla items
    public IList<object>? Mods => throw new NotImplementedException();

    public IList<object>? ShaderPacks => throw new NotImplementedException();


    // Functions for loading and refreshing items
    public void LoadInstances() => throw new NotImplementedException();

    public void LoadAssets() => throw new NotImplementedException();

    public void LoadLibraries() => throw new NotImplementedException();

    public void LoadDataPacks() => throw new NotImplementedException();

    public void LoadMods() => throw new NotImplementedException();

    public void LoadShaderPacks() => throw new NotImplementedException();

    #region Constructor and Builder

    internal MinecraftProfile(string minecraftFolderPath)
    {
        MinecraftFolderPath = minecraftFolderPath;
    }

    public static MinecraftProfileBuilder CreateBuilder(string minecraftFolderPath)
    {
        return new MinecraftProfileBuilder(minecraftFolderPath);
    }

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

    #endregion
}
