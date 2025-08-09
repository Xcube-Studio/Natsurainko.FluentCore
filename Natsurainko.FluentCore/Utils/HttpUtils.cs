using Nrk.FluentCore.GameManagement.Downloader;
using System.Buffers;
using System.IO;
using System.Net.Http;

namespace Nrk.FluentCore.Utils;

internal static class HttpUtils
{
    public static readonly HttpClient HttpClient = new();
    public static readonly IDownloader Downloader = new MultipartDownloader(HttpClient, 1024 * 1024, 8, 64);
    public static readonly MemoryPool<byte> MemoryPool = MemoryPool<byte>.Shared;

    public static string ReadAsString(this HttpContent content)
    {
        using var stream = content.ReadAsStream();
        using var streamReader = new StreamReader(stream);

        return streamReader.ReadToEnd();
    }
}
