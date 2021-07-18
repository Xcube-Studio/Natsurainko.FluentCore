using FluentCore.UWP.Interface;
using FluentCore.UWP.Model.Auth;
using FluentCore.UWP.Model.Auth.Mojang;
using FluentCore.UWP.Model.Game;
using FluentCore.UWP.Service.Local;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Service.Component.Authenticator
{
    public class OfflineAuthenticator : IAuthenticator
    {
        public OfflineAuthenticator(string userName, Guid uuid = default)
        {
            this.UserName = userName;
            this.Uuid = uuid;
        }

        public string UserName { get; private set; }

        public Guid Uuid { get; private set; }

        public Tuple<StandardResponseModel, AuthResponseTypeModel> Authenticate()
        {
            Uuid = Uuid.Equals(null) ? UuidHelper.FromString(UserName) : Guid.NewGuid();

            var model = new StandardResponseModel
            {
                AccessToken = Guid.NewGuid().ToString("N"),
                ClientToken = Guid.NewGuid().ToString("N"),
                SelectedProfile = new ProfileModel
                {
                    Id = Uuid.ToString("N"),
                    Name = UserName
                },
                User = new User()
                {
                    Id = Uuid.ToString("N"),
                    Properties = new List<PropertyModel>()
                    {
                        new PropertyModel
                        {
                            Name = "preferredLanguage",
                            Value = "zh-cn"
                        }
                    }
                }
            };

            return new Tuple<StandardResponseModel, AuthResponseTypeModel>(model, AuthResponseTypeModel.Succeeded);
        }

        public Task<Tuple<StandardResponseModel, AuthResponseTypeModel>> AuthenticateAsync() => Task.Run(Authenticate);

        public void Dispose() => GC.SuppressFinalize(this);

    }
}
