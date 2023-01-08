using System.Collections.Generic;
using System.IO;

namespace Natsurainko.FluentCore.Interface;

public interface IGameCoreLocator<out TCore> 
    where TCore : IGameCore
{
    DirectoryInfo Root { get; }

    IEnumerable<TCore> GetGameCores();

    TCore GetGameCore(string id);
}

