using FluentCore.Model.Launch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Interface
{
    public interface ICoreLocator
    {
        string Root { get; set; }

        IEnumerable<GameCore> GetAllGameCores();

        GameCore GetGameCore(string id);
    }
}
