using Nrk.FluentCore.GameManagement.Downloader;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Timer = System.Timers.Timer;

namespace Nrk.FluentCore.Utils;

#nullable disable
// TODO: refactor downloading system later

// TOOD: use internal
public static class HttpUtils
{
    public static readonly HttpClient HttpClient = new();
    public static readonly IDownloader Downloader = new MultipartDownloader(HttpClient, 1024 * 1024, 8, 64);
    public static readonly MemoryPool<byte> MemoryPool = MemoryPool<byte>.Shared;

    public static HttpResponseMessage HttpGet(
        string url,
        Tuple<string, string> authorization = default,
        HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

        if (authorization != null)
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                authorization.Item1,
                authorization.Item2
            );

        var responseMessage = HttpClient.Send(requestMessage, httpCompletionOption, CancellationToken.None);

        if (responseMessage.StatusCode.Equals(HttpStatusCode.Found))
        {
            string redirectUrl = responseMessage.Headers.Location.AbsoluteUri;

            responseMessage.Dispose();
            GC.Collect();

            return HttpGet(redirectUrl, authorization, httpCompletionOption);
        }

        return responseMessage;
    }

    public static string ReadAsString(this HttpContent content)
    {
        using var stream = content.ReadAsStream();
        using var streamReader = new StreamReader(stream);

        return streamReader.ReadToEnd();
    }
}
