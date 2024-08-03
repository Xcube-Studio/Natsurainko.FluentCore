using Nrk.FluentCore.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public class MultipartDownloader
{
    private readonly HttpClient _httpClient;
    private readonly int _chunkSize;
    private readonly int _maxConcurrentThreads;

    const int DownloadBufferSize = 4096; // 4 KB

    public MultipartDownloader(int chunkSize = 104857600 /* 1MB */, int maxConcurrentThreads = 4, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? HttpUtils.HttpClient;
        _chunkSize = chunkSize;
        _maxConcurrentThreads = maxConcurrentThreads;
    }

    public async Task DownloadFileAsync(string url, string destinationPath, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        // Try to get the size of the file
        (var response, url) = await PrepareForDownloadAsync(url, cancellationToken);

        // Use multi-part download if the file size is larger than a threshold and Content-Length is provided
        bool useMultiPart = false;
        long fileSize = -1;
        if (response.Content.Headers.ContentLength is long contentLength)
        {
            fileSize = contentLength;
            if (contentLength > _chunkSize)
            {
                // Check support for range requests
                if (response.Headers.AcceptRanges.Contains("bytes")) // Check if the server supports range requests by checking the Accept-Ranges header
                {
                    useMultiPart = true;
                }
                else // Check if the server supports range requests by sending a range request
                {
                    var rangeRequest = new HttpRequestMessage(HttpMethod.Get, url);
                    rangeRequest.Headers.Range = new RangeHeaderValue(0, 0); // Request first byte
                    var rangeResponse = await _httpClient.SendAsync(rangeRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    useMultiPart = rangeResponse.StatusCode == HttpStatusCode.PartialContent;
                }
                // Fall back to single part download if the remote server does not provide a Content-Length or does not support range requests
            }
        }

        // Ensure destination directory exists
        string? destinationDir = Path.GetDirectoryName(destinationPath);
        if (destinationDir is not null) // destinationDir is not root directory
            Directory.CreateDirectory(destinationDir); // Create the directory if it doesn't exist

        // Try multi-part download if Content-Length is provided and the file size is larger than a threshold
        if (useMultiPart)
        {
            await DownloadMultiPartAsync(url, destinationPath, fileSize, progress, cancellationToken);
        }
        else
        {
            await DownloadSinglePartAsync(url, destinationPath, fileSize == -1 ? null : fileSize, progress, cancellationToken);
        }
    }

    // Handle URL redirects and get header
    private async Task<(HttpResponseMessage Response, string RedirectedUrl)> PrepareForDownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        // Get header
        var request = new HttpRequestMessage(HttpMethod.Head, url);
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Found)
        {
            var redirectUrl = response.Headers.Location?.AbsoluteUri;
            if (redirectUrl is not null)
                return await PrepareForDownloadAsync(redirectUrl, cancellationToken); // Try the redirect URL if 302 is returned
        }
        response.EnsureSuccessStatusCode();

        return (response, url);
    }

    private async Task DownloadSinglePartAsync(string url, string destinationPath, long? fileSize, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        // Send a GET request to start downloading the file
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Prepare streams and download buffer
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
        if (fileSize is long size)
            fileStream.SetLength(size);

        // Download the file
        byte[] downloadBufferArr = ArrayPool<byte>.Shared.Rent(DownloadBufferSize);
        Memory<byte> downloadBuffer = downloadBufferArr.AsMemory(0, DownloadBufferSize);
        await WriteStreamToFile(contentStream, fileStream, downloadBuffer, progress, cancellationToken);
        ArrayPool<byte>.Shared.Return(downloadBufferArr);
    }

    private async Task WriteStreamToFile(Stream contentStream, FileStream fileStream, Memory<byte> buffer, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        int bytesRead = 0;
        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer[0..bytesRead], cancellationToken);
            progress?.Report(bytesRead);
        }
    }

    private class MultipartDownloadStates
    {
        public required long TotalBytes { get; init; }
        public required long TotalChunks { get; init; }
        public required long ChunkSize { get; init; }
        public long ChunkScheduled { get; private set; } = 0;

        private object _lock = new();

        // Returns the indices (inclusive) of bytes in the next chunk if there are more chunks to download
        public (long start, long end)? NextChunk()
        {
            long start, end;
            lock (_lock)
            {
                if (ChunkScheduled == TotalChunks)
                    return null;
                start = ChunkScheduled * ChunkSize;
                ChunkScheduled++;
            }
            end = Math.Min(start + ChunkSize, TotalBytes) - 1; // Handle the last chunk
            return (start, start + ChunkSize - 1);
        }
    }

    private async Task DownloadMultiPartAsync(string url, string destinationPath, long fileSize, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        long totalNumberOfParts = Math.DivRem(fileSize, _chunkSize, out long remainder);
        if (remainder > 0)
            totalNumberOfParts++;

        // Pre-allocate the file with the desired size
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.Write);
        fileStream.SetLength(fileSize);

        // Initialize workers
        int numberOfWorkers = (int)Math.Min(_maxConcurrentThreads, totalNumberOfParts);
        Task[] workers = new Task[numberOfWorkers];
        MultipartDownloadStates states = new()
        {
            TotalBytes = fileSize,
            TotalChunks = totalNumberOfParts,
            ChunkSize = _chunkSize
        };
        for (int i = 0; i < numberOfWorkers; i++)
        {
            workers[i] = MultipartDownloadWorker(url, destinationPath, states, progress, cancellationToken);
        }

        await Task.WhenAll(workers);
    }

    private async Task MultipartDownloadWorker(string url, string destinationPath, MultipartDownloadStates states, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        // Resources for this worker only
        using var fileStream = new FileStream(destinationPath, FileMode.Open, FileAccess.Write, FileShare.Write);

        // Download the file
        byte[] downloadBufferArr = ArrayPool<byte>.Shared.Rent(DownloadBufferSize);
        Memory<byte> downloadBuffer = downloadBufferArr.AsMemory(0, DownloadBufferSize);

        while (states.NextChunk() is (long start, long end))
        {
            // Start writing at the beginning of the chunk
            fileStream.Seek(start, SeekOrigin.Begin);

            // Send a range request to download the chunk of the file
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(start, end);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Write to the file
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await WriteStreamToFile(contentStream, fileStream, downloadBuffer, progress, cancellationToken);
        }

        ArrayPool<byte>.Shared.Return(downloadBufferArr);
    }
}
