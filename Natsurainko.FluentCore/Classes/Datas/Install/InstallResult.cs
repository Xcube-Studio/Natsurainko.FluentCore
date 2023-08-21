using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Classes.Datas.Install;

public class InstallResult
{
    public bool Success { get; set; }

    public Exception Exception { get; set; }

    public IEnumerable<string> Log { get; set; }
}
