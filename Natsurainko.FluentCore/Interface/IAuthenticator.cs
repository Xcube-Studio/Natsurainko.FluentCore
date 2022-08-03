using Natsurainko.FluentCore.Class.Model.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Interface
{
    public interface IAuthenticator
    {
        Account Authenticate();

        Task<Account> AuthenticateAsync();
    }
}
