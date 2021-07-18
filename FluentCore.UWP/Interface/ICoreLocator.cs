using FluentCore.UWP.Model.Game;
using FluentCore.UWP.Model.Launch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Interface
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
