using System;

namespace FluentCore.Exceptions.Launcher
{
    public class GameCoreNotFoundException : Exception
    {
        public string Id { get; set; }
    }
}
