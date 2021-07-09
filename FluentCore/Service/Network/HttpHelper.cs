using FluentCore.Service.Local;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

            if (authorization != null)
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue(authorization.Item1, authorization.Item2);

            var responseMessage = await HttpClient.SendAsync(requestMessage, httpCompletionOption);

            if (responseMessage.StatusCode.Equals(HttpStatusCode.Found))
            {
                string redirectUrl = responseMessage.Headers.Location.AbsoluteUri;
                responseMessage.Dispose();
                return await HttpGetAsync(redirectUrl, authorization, httpCompletionOption);
            }

            return responseMessage;
        }

        public static async Task<HttpResponseMessage> HttpPostAsync(string url, Stream content, string contentType = "application/json")
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            using var httpContent = new StreamContent(content);

            httpContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            requestMessage.Content = httpContent;

            return await HttpClient.SendAsync(requestMessage);
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
            FileInfo fileInfo = default;

            if (responseMessage.Content.Headers != null && responseMessage.Content.Headers.ContentDisposition != null)
                fileInfo = new FileInfo(Path.Combine(folder, responseMessage.Content.Headers.ContentDisposition.FileName.Trim('\"')));
            else fileInfo = new FileInfo(Path.Combine(folder, Path.GetFileName(responseMessage.RequestMessage.RequestUri.AbsoluteUri)));

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
