using System;

namespace FluentCore.Model.Auth
{
    public class AuthDataModel
    {
        public Guid Uuid { get; set; }

        public string AccessToken { get; set; }

        public string UserName { get; set; }
    }
}
