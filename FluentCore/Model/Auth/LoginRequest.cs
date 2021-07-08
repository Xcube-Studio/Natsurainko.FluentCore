using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Model.Auth
{
    public class LoginRequestModel
    {
        [JsonProperty("agent")]
        public Agent Agent { get; set; }
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
        /// 客户端标识符
        /// </summary>
        [JsonProperty("clientToken")]
        public string ClientToken { get; set; }

        [JsonProperty("requestUser")]
        public bool RequestUser { get; set; }
    }

    public class Agent
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }
    }
}
