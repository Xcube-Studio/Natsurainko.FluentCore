using FluentCore.UWP.Model.Launch;
using FluentCore.UWP.Service.Local;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Interface
{
    public interface ILauncher : IDisposable
    {
        ProcessContainer ProcessContainer { get; }

        LaunchConfig LaunchConfig { get; }

        string Root { get; }

        void Launch(string id);

        void Stop();
    }
}
