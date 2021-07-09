using FluentCore.Service.Local;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Service.Network
{
    public class HttpHelper
    {
        public static int BufferSize { get; set; } = 4096;

        public static readonly HttpClient HttpClient;

        static HttpHelper() => HttpClient = new HttpClient();

        public static async Task<HttpResponseMessage> HttpGetAsync(string url, Tuple<string, string> authorization = default, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead)
        {
            HttpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue() { NoCache = true, NoStore = true };

            if (authorization != null)
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authorization.Item1, authorization.Item2);

            var responseMessage = await HttpClient.GetAsync(url, httpCompletionOption);
            HttpClient.DefaultRequestHeaders.Authorization = null;

            return responseMessage;
        }

        public static async Task<HttpResponseMessage> HttpPostAsync(string url, Stream content, string contentType = "application/json")
        {
            using (var httpContent = new StreamContent(content))
            {
                httpContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                return await HttpClient.PostAsync(url, httpContent);
            }
        }

        public static async Task<HttpResponseMessage> HttpPostAsync(string url, string content, string contentType = "application/json")
        {
            using(var stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                    writer.Write(content);

                return await HttpPostAsync(url, stream, contentType);
            }
        }

        public static async Task<FileInfo> HttpDownloadAsync(string url,string folder)
        {
            using var responseMessage = await HttpGetAsync(url, default, HttpCompletionOption.ResponseHeadersRead);

            var fileInfo = new FileInfo(Path.Combine(folder, Path.GetFileName(responseMessage.RequestMessage.RequestUri.AbsoluteUri)));

            using var fileStream = File.Create(fileInfo.FullName);
            using var stream = await responseMessage.Content.ReadAsStreamAsync();

            byte[] bytes = new byte[BufferSize];
            int read = await stream.ReadAsync(bytes.AsMemory(0, bytes.Length));
            while (read > 0)
            {
                await fileStream.WriteAsync(bytes.AsMemory(0, read));
                read = await stream.ReadAsync(bytes.AsMemory(0, bytes.Length));
            }

            fileStream.Flush();
            fileStream.Close();
            stream.Close();

            return fileInfo;
        }
    }
}
