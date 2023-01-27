using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Interface;

public interface IAuthenticator
{
    IAccount Authenticate();

    Task<IAccount> AuthenticateAsync();
}
