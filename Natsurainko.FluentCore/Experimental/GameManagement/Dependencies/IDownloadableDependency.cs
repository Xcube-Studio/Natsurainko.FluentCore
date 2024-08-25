using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public interface IDownloadableDependency
{
    /// <summary>
    /// URL to download the file
    /// </summary>
    string Url { get; }
}
