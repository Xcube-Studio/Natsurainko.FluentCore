using Newtonsoft.Json;

namespace FluentCore.Model
{
    public class FileModel
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("sha1")]
        public string Sha1 { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        //for client-x.xx.xml
        [JsonProperty("id")]
        public string Id { get; set; }
    }

}
