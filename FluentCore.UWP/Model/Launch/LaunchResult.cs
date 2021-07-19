using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Model.Launch
{
    public class LaunchResult
    {
        public IEnumerable<string> Logs { get; set; }

        public string Args { get; set; }

        public IEnumerable<string> Errors { get; set; }
    }
}
