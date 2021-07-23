using FluentCore.Interface;
using FluentCore.Model.Auth;
using FluentCore.Model.Auth.Yggdrasil;
using FluentCore.Service.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Service.Component.Authenticator
{
    public class YggdrasilAuthenticator : IAuthenticator
    {
        public string YggdrasilServerUrl { get; set; } = "https://authserver.mojang.com";

        public string ClientToken { get; set; } = Guid.NewGuid().ToString("N");

        public string Email { get; set; }

        public string Password { get; set; }

        public YggdrasilAuthenticator(string email, string password, string yggdrasilServerUrl = default)
        {

        }

        public YggdrasilAuthenticator(string yggdrasilServerUrl = default)
        {

        }

        public Tuple<ResponseModel, AuthResponseTypeModel> Authenticate() => AuthenticateAsync().GetAwaiter().GetResult();

        public async Task<Tuple<ResponseModel, AuthResponseTypeModel>> AuthenticateAsync()
        {
            string content = JsonConvert.SerializeObject(
                new LoginRequestModel
                {
                    ClientToken = this.ClientToken,
                    UserName = this.Email,
                    Password = this.Password
                }
            );

            using var res = await HttpHelper.HttpPostAsync($"{YggdrasilServerUrl}/authenticate", content);

            if (res.IsSuccessStatusCode)
                return new Tuple<ResponseModel, AuthResponseTypeModel>
                    (JsonConvert.DeserializeObject<StandardResponseModel>(await res.Content.ReadAsStringAsync()), AuthResponseTypeModel.Succeeded);
            else return new Tuple<ResponseModel, AuthResponseTypeModel>
                    (JsonConvert.DeserializeObject<ErrorResponseModel>(await res.Content.ReadAsStringAsync()), AuthResponseTypeModel.Failed);
        }

        public Tuple<ResponseModel, AuthResponseTypeModel> Refresh(string accessToken, ProfileModel profile = null) => RefreshAsync(accessToken, profile).GetAwaiter().GetResult();

        public async Task<Tuple<ResponseModel, AuthResponseTypeModel>> RefreshAsync(string accessToken, ProfileModel profile = null)
        {
            string content = JsonConvert.SerializeObject(
                new
                {
                    clientToken = this.ClientToken,
                    accessToken,
                    requestUser = true
                }
            );

            if (profile != null)
                content = JsonConvert.SerializeObject(
                new
                {
                    clientToken = this.ClientToken,
                    accessToken,
                    requestUser = true,
                    selectedProfile = profile
                }
            );

            using var res = await HttpHelper.HttpPostAsync($"{YggdrasilServerUrl}/refresh", content);

            if (res.IsSuccessStatusCode)
                return new Tuple<ResponseModel, AuthResponseTypeModel>
                    (JsonConvert.DeserializeObject<StandardResponseModel>(await res.Content.ReadAsStringAsync()), AuthResponseTypeModel.Succeeded);
            else return new Tuple<ResponseModel, AuthResponseTypeModel>
                    (JsonConvert.DeserializeObject<ErrorResponseModel>(await res.Content.ReadAsStringAsync()), AuthResponseTypeModel.Failed);
        }

        public Tuple<ResponseModel, AuthResponseTypeModel> Validate(string accessToken) => ValidateAsync(accessToken).GetAwaiter().GetResult();

        public async Task<Tuple<ResponseModel, AuthResponseTypeModel>> ValidateAsync(string accessToken)
        {
            string content = JsonConvert.SerializeObject(
                new StandardRequestModel
                {
                    ClientToken = this.ClientToken,
                    AccessToken = accessToken
                }
            );

            using var res = await HttpHelper.HttpPostAsync($"{YggdrasilServerUrl}/validate", content);

            if (res.IsSuccessStatusCode)
                return new Tuple<ResponseModel, AuthResponseTypeModel>
                    (null, AuthResponseTypeModel.Succeeded);
            else return new Tuple<ResponseModel, AuthResponseTypeModel>
                    (JsonConvert.DeserializeObject<ErrorResponseModel>(await res.Content.ReadAsStringAsync()), AuthResponseTypeModel.Failed);
        }

        public Tuple<ResponseModel, AuthResponseTypeModel> Signout() => SignoutAsync().GetAwaiter().GetResult();

        public async Task<Tuple<ResponseModel, AuthResponseTypeModel>> SignoutAsync()
        {
            string content = JsonConvert.SerializeObject(
                new
                {
                    username = this.Email,
                    password = this.Password
                }
            );

            using var res = await HttpHelper.HttpPostAsync($"{YggdrasilServerUrl}/signout", content);

            if (res.IsSuccessStatusCode)
                return new Tuple<ResponseModel, AuthResponseTypeModel>
                    (null, AuthResponseTypeModel.Succeeded);
            else return new Tuple<ResponseModel, AuthResponseTypeModel>
                    (JsonConvert.DeserializeObject<ErrorResponseModel>(await res.Content.ReadAsStringAsync()), AuthResponseTypeModel.Failed);
        }

        public Tuple<ResponseModel, AuthResponseTypeModel> Invalidate(string accessToken) => InvalidateAsync(accessToken).GetAwaiter().GetResult();

        public async Task<Tuple<ResponseModel, AuthResponseTypeModel>> InvalidateAsync(string accessToken)
        {
            string content = JsonConvert.SerializeObject(
                new StandardRequestModel
                {
                    ClientToken = this.ClientToken,
                    AccessToken = accessToken
                }
            );

            using var res = await HttpHelper.HttpPostAsync($"{YggdrasilServerUrl}/invalidate", content);

            if (res.IsSuccessStatusCode)
                return new Tuple<ResponseModel, AuthResponseTypeModel>
                    (null, AuthResponseTypeModel.Succeeded);
            else return new Tuple<ResponseModel, AuthResponseTypeModel>
                    (JsonConvert.DeserializeObject<ErrorResponseModel>(await res.Content.ReadAsStringAsync()), AuthResponseTypeModel.Failed);
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
