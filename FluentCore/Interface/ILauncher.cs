using FluentCore.Model.Launch;
using FluentCore.Service.Local;
using System;

namespace FluentCore.Interface
{
    public interface ILauncher : IDisposable
    {
        ProcessContainer ProcessContainer { get; }

        LaunchConfig LaunchConfig { get; }

        ICoreLocator CoreLocator { get; set; }

        void Launch(string id);

        void Stop();
    }
}
