using FluentCore.Model.Game;
using FluentCore.Model.Launch;
using System.Collections.Generic;

namespace FluentCore.Interface
{
    public interface ICoreLocator
    {
        string Root { get; set; }

        IEnumerable<GameCore> GetAllGameCores();

        IEnumerable<CoreModel> GetAllCoreModels();

        GameCore GetGameCoreFromId(string id);

        CoreModel GetCoreModelFromId(string id);
    }
}
