using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Auth;
using Natsurainko.Toolkits.Values;
using System;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Module.Authenticator;

public class OfflineAuthenticator : IAuthenticator
{
    public string Name { get; set; }

    public Guid Uuid { get; set; }

    public OfflineAuthenticator(string name, Guid uuid = default)
    {
        Name = name;
        Uuid = uuid;

        if (Uuid == default)
            Uuid = GuidHelper.FromString(Name);
    }

    public Account Authenticate()
        => new OfflineAccount
        {
            AccessToken = Guid.NewGuid().ToString("N"),
            ClientToken = Guid.NewGuid().ToString("N"),
            Name = Name,
            Uuid = Uuid
        };

    public Task<Account> AuthenticateAsync()
        => Task.Run(Authenticate);
}
