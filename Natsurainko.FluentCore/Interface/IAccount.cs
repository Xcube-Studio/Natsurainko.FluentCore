using Natsurainko.FluentCore.Model.Auth;
using Newtonsoft.Json;
using System;

namespace Natsurainko.FluentCore.Interface;

[JsonConverter(typeof(AccountJsonConverter))]
public interface IAccount
{
    string Name { get; set; }

    Guid Uuid { get; set; }

    string AccessToken { get; set; }

    string ClientToken { get; set; }

    AccountType Type { get; }
}
