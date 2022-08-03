using Newtonsoft.Json;

namespace Natsurainko.FluentCore.Class.Model.Auth.Yggdrasil
{
    /// <summary>
    /// Yggdrasil登录请求模型
    /// </summary>
    public class LoginRequestModel : BaseYggdrasilRequestModel
    {
        [JsonProperty("agent")]
        public Agent Agent { get; set; } = new Agent();

        /// <summary>
        /// mojang帐号名
        /// </summary>
        [JsonProperty("username")]
        public string UserName { get; set; }

        /// <summary>
        /// mojang帐号密码
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        /// <summary>
        /// 若为true则将user对象加入到响应中
        /// </summary>
        [JsonProperty("requestUser")]
        public bool RequestUser { get; set; } = true;
    }

    /// <summary>
    /// Yggdrasil标准请求模型
    /// </summary>
    public class YggdrasilRequestModel : BaseYggdrasilRequestModel
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }
    }

    public class Agent
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "Minecraft";

        [JsonProperty("version")]
        public int Version { get; set; } = 1;
    }

    public class BaseYggdrasilRequestModel
    {
        /// <summary>
        /// 客户端标识符
        /// </summary>
        [JsonProperty("clientToken")]
        public string ClientToken { get; set; }
    }
}