using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Natsurainko.FluentCore.Class.Model.Auth
{
    public class Account
    {
        public string Name { get; set; }

        public Guid Uuid { get; set; }

        public string AccessToken { get; set; }

        public string ClientToken { get; set; }

        public AccountType AccountType { get; set; } = AccountType.Offline;
    }
}
