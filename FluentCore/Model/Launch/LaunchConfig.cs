using FluentCore.Interface;
using FluentCore.Model.Auth;
using FluentCore.Service.Network.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
