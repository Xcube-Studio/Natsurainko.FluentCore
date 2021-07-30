using FluentCore.Model.Auth;
using System;

namespace FluentCore.Model.Launch
{
    public class LaunchConfig
    {
        public string JavaPath { get; set; }

        public string NativesFolder { get; set; }

        public int MaximumMemory { get; set; } = 1024;

        public int? MinimumMemory { get; set; } = 512;

        public string MoreFrontArgs { get; set; } = default;

        public string MoreBehindArgs { get; set; } = default;

        public string ClientToken { get; set; } = Guid.NewGuid().ToString("N");

        public AuthDataModel AuthDataModel { get; set; }
    }
}
