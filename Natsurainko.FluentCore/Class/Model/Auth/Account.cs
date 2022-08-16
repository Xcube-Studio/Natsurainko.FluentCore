using Natsurainko.Toolkits.Values;
using System;

namespace Natsurainko.FluentCore.Class.Model.Auth
{
    public class Account
    {
        public string YggdrasilServerUrl { get; set; }

        public string Name { get; set; }

        public Guid Uuid { get; set; }

        public string AccessToken { get; set; }

        public string ClientToken { get; set; }

        public AccountType AccountType { get; set; } = AccountType.Offline;

        public static Account Default => new()
        {
            Name = "Steve",
            Uuid = Guid.Parse("5627dd98e6be3c21b8a8e92344183641"),
            AccessToken = Guid.NewGuid().ToString("N"),
            ClientToken = string.Empty,
            AccountType = 0
        };

        public override int GetHashCode()
            => this.Name.GetHashCode() ^ this.Uuid.GetHashCode()
            ^ this.AccessToken.GetHashCode() ^ this.ClientToken.GetHashCode()
            ^ this.AccountType.GetHashCode() ^ this.YggdrasilServerUrl.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var item = (Account)obj;

            return this.Name == item.Name
                && this.Uuid == item.Uuid
                && this.AccessToken == item.AccessToken
                && this.ClientToken == item.ClientToken
                && this.AccountType == item.AccountType
                && this.YggdrasilServerUrl == item.YggdrasilServerUrl;
        }
    }
}
