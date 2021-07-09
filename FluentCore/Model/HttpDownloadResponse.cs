using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Model
{
    public class HttpDownloadResponse
    {
        public string Message { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public FileInfo FileInfo { get; set; }
    }
}
