using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Model.Install.Forge
{
    public class ForgeInstallerResult
    {
        public bool IsSuccessful { get; set; }

        public IEnumerable<string> ProcessOutput { get; set; }

        public IEnumerable<string> ProcessErrorOutput { get; set; }

        public string Message { get; set; }
    }
}
