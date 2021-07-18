using FluentCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Interface
{
    public interface IDependence
    {
        HttpDownloadRequest GetDownloadRequest(string root);

        string GetRelativePath();
    }
}
