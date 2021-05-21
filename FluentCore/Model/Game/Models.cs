using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FluentCore.Model.Game
{
    public class Downloads
    {
        [JsonProperty("artifact")]
        public FileModel Artifact { get; set; }

        [JsonProperty("classifiers")]
        public Dictionary<string, FileModel> Classifiers { get; set; }
    }

    public class Extract
    {
        [JsonProperty("exclude")] 
        public List<string> Exclude { get; set; }
    }

    public class RuleModel
    {
        [JsonProperty("action")]
        public string Action { get; set; }


        [JsonProperty("os")]
        public Dictionary<string, string> System { get; set; }

    }

    public class AssetIndex : FileModel
    {
        [JsonProperty("totalSize")]
        public int TotalSize { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Client
    {
        [JsonProperty("argument")]
        public string Argument { get; set; }

        [JsonProperty("file")]
        public FileModel File { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
    
    public class Logging
    {
        [JsonProperty("client")]
        public Client Client { get; set; }
    }

    public class Arguments
    {
        [JsonProperty("game")] 
        public List<object> Game { get; set; }

        [JsonProperty("jvm")] 
        public List<object> Jvm { get; set; }
    }
}
