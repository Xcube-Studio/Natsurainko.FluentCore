using FluentCore.Model.Auth;
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

        public int MaximumMemory { get; set; }

        public int? MinimumMemory { get; set; }

        public string MoreFrontArgs { get; set; }

        public string MoreBehindArgs { get; set; }

        public string ClientToken { get; set; } = Guid.NewGuid().ToString("N");

        public AuthDataModel AuthDataModel { get; set; }
    }
}
