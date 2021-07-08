using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Model.Auth
{
    public class AuthDataModel
    {
        public Guid Uuid { get; set; }

        public string AccessToken { get; set; }

        public string UserName { get; set; }
    }
}
