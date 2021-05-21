using FluentCore.Interface;
using FluentCore.Model.Launch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Service.Component.Launch
{
    public class CoreLocator : ICoreLocator
    {
        public CoreLocator(string root)
        {
            if (string.IsNullOrEmpty(root))
                throw new ArgumentException("无效的参数");

            this.Root = root;
        }

        public string Root { get; set; }

        public IEnumerable<GameCore> GetAllGameCores()
        {
            throw new NotImplementedException();
        }

        public GameCore GetGameCore(string id)
        {
            throw new NotImplementedException();
        }
    }
}
