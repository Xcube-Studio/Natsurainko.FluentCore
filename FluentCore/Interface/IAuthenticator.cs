using FluentCore.Model.Auth;
using FluentCore.Model.Auth.Yggdrasil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Interface
{
    public interface IAuthenticator : IDisposable
    {
        Tuple<ResponseModel, AuthResponseTypeModel> Authenticate();

        Task<Tuple<ResponseModel, AuthResponseTypeModel>> AuthenticateAsync();
    }
}
