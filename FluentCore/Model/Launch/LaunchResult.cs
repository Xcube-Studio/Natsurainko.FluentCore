using System.Collections.Generic;

namespace FluentCore.Model.Launch
{
    public class LaunchResult
    {
        public IEnumerable<string> Logs { get; set; }

        public IEnumerable<string> Errors { get; set; }

        public string Args { get; set; }
    }
}
