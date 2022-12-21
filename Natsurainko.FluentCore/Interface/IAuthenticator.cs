using Natsurainko.FluentCore.Model.Auth;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Interface;

public interface IAuthenticator
{
    Account Authenticate();

    Task<Account> AuthenticateAsync();
}
