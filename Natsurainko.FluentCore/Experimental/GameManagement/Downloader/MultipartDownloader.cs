using Nrk.FluentCore.Utils;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public class MultipartDownloader : IDownloader
{
    public long ChunkSize { get => _config.ChunkSize; }
    public int WorkersPerDownloadTask { get => _config.WorkersPerDownloadTask; }
    public int ConcurrentDownloadTasks { get => _config.ConcurrentDownloadTasks; }

    private HttpClient HttpClient { get => _config.HttpClient; }

    private const int DownloadBufferSize = 4096; // 4 KB
    private readonly DownloaderConfig _config;
    private readonly IDownloadMirror? _mirror;

    private readonly SemaphoreSlim _globalDownloadTasksSemaphore;

    public MultipartDownloader(HttpClient? httpClient, long chunkSize = 1048576 /* 1MB */, int workersPerDownloadTask = 16, int concurrentDownloadTasks = 5, IDownloadMirror? mirror = null)
    {
        httpClient ??= HttpUtils.HttpClient;
        _config = new DownloaderConfig(httpClient, chunkSize, workersPerDownloadTask, concurrentDownloadTasks);
        _mirror = mirror;
        _globalDownloadTasksSemaphore = new SemaphoreSlim(0, concurrentDownloadTasks);
    }

    public async Task<DownloadResult> DownloadFileAsync(DownloadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await DownloadFileDriverAsync(request, cancellationToken);
            return new DownloadResult(DownloadResultType.Successful);
        }
        catch (TaskCanceledException)
        {
            return new DownloadResult(DownloadResultType.Cancelled);
        }
        catch (Exception e)
        {
            return new DownloadResult(DownloadResultType.Failed)
            {
                Exception = e
            };
        }
    }

    public async Task DownloadFileDriverAsync(IDownloadRequest request, CancellationToken cancellationToken = default)
    {
        string url = request.Url;
        string localPath = request.LocalPath;

        if (_mirror is not null)
            url = _mirror.GetMirrorUrl(url);

        // Try to get the size of the file
        (var response, url) = await PrepareForDownloadAsync(url, cancellationToken);
        DownloadStates states = new()
        {
            Url = url,
            LocalPath = localPath,
            ChunkSize = _config.ChunkSize
        };

        // Use multi-part download if Content-Length is provided and the remote server supports range requests
        // Fall back to single part download if the remote server does not provide a Content-Length or does not support range requests
        bool useMultiPart = false;
        if (response.Content.Headers.ContentLength is long contentLength)
        {
            states.TotalBytes = contentLength;
            // Check support for range requests
            if (response.Headers.AcceptRanges.Contains("bytes")) // Check if the server supports range requests by checking the Accept-Ranges header
            {
                useMultiPart = true;
            }
            else // Check if the server supports range requests by sending a range request
            {
                var rangeRequest = new HttpRequestMessage(HttpMethod.Get, url);
                rangeRequest.Headers.Range = new RangeHeaderValue(0, 0); // Request first byte
                var rangeResponse = await HttpClient.SendAsync(rangeRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                useMultiPart = rangeResponse.StatusCode == HttpStatusCode.PartialContent;
            }
        }

        // Status changed
        request.OnFileSizeReceived(states.TotalBytes);

        // Ensure destination directory exists
        string? destinationDir = Path.GetDirectoryName(localPath);
        if (destinationDir is not null) // destinationDir is not root directory
            Directory.CreateDirectory(destinationDir); // Create the directory if it doesn't exist

        // Try multi-part download if Content-Length is provided and the file size is larger than a threshold
        if (useMultiPart)
        {
            await DownloadMultiPartAsync(states, request, cancellationToken);
        }
        else
        {
            await DownloadSinglePartAsync(states, request, cancellationToken);
        }
    }

    // Handle URL redirects and get header
    private async Task<(HttpResponseMessage Response, string RedirectedUrl)> PrepareForDownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        // Get header
        var request = new HttpRequestMessage(HttpMethod.Head, url);
        var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Found)
        {
            var redirectUrl = response.Headers.Location?.AbsoluteUri;
            if (redirectUrl is not null)
                return await PrepareForDownloadAsync(redirectUrl, cancellationToken); // Try the redirect URL if 302 is returned
        }
        response.EnsureSuccessStatusCode();

        return (response, url);
    }

    private async Task DownloadSinglePartAsync(DownloadStates states, IDownloadRequest request, CancellationToken cancellationToken = default)
    {
        // Send a GET request to start downloading the file
        using var response = await HttpClient.GetAsync(states.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Prepare streams and download buffer
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(states.LocalPath, FileMode.Create, FileAccess.Write);
        if (states.TotalBytes is long size)
            fileStream.SetLength(size);

        // Download the file
        byte[] downloadBufferArr = ArrayPool<byte>.Shared.Rent(DownloadBufferSize);
        Memory<byte> downloadBuffer = downloadBufferArr.AsMemory(0, DownloadBufferSize);
        await WriteStreamToFile(contentStream, fileStream, downloadBuffer, request, cancellationToken);
        ArrayPool<byte>.Shared.Return(downloadBufferArr);
    }

    private async Task WriteStreamToFile(Stream contentStream, FileStream fileStream, Memory<byte> buffer, IDownloadRequest request, CancellationToken cancellationToken = default)
    {
        int bytesRead = 0;
        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer[0..bytesRead], cancellationToken);
            request.OnBytesDownloaded(bytesRead);
        }
    }

    private async Task DownloadMultiPartAsync(DownloadStates states, IDownloadRequest request, CancellationToken cancellationToken = default)
    {
        long fileSize = (long)states.TotalBytes!; // Not null in multipart download

        long totalChunks = Math.DivRem(fileSize, ChunkSize, out long remainder);
        if (remainder > 0)
            totalChunks++;
        states.TotalChunks = totalChunks;

        // Pre-allocate the file with the desired size
        using var fileStream = new FileStream(states.LocalPath, FileMode.Create, FileAccess.Write, FileShare.Write);
        fileStream.SetLength(fileSize);

        // Initialize workers
        int numberOfWorkers = (int)Math.Min(WorkersPerDownloadTask, totalChunks);
        Task[] workers = new Task[numberOfWorkers];
        for (int i = 0; i < numberOfWorkers; i++)
        {
            workers[i] = MultipartDownloadWorker(states, request, cancellationToken);
        }
        await Task.WhenAll(workers);
    }

    private async Task MultipartDownloadWorker(DownloadStates states, IDownloadRequest downloadRequest, CancellationToken cancellationToken = default)
    {
        // Resources for this worker only
        using var fileStream = new FileStream(states.LocalPath, FileMode.Open, FileAccess.Write, FileShare.Write);

        // Download the file
        byte[] downloadBufferArr = ArrayPool<byte>.Shared.Rent(DownloadBufferSize);
        Memory<byte> downloadBuffer = downloadBufferArr.AsMemory(0, DownloadBufferSize);

        while (states.NextChunk() is (long start, long end))
        {
            // Start writing at the beginning of the chunk
            fileStream.Seek(start, SeekOrigin.Begin);

            // Send a range request to download the chunk of the file
            var request = new HttpRequestMessage(HttpMethod.Get, states.Url);
            request.Headers.Range = new RangeHeaderValue(start, end);
            var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Write to the file
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await WriteStreamToFile(contentStream, fileStream, downloadBuffer, downloadRequest, cancellationToken);
        }

        ArrayPool<byte>.Shared.Return(downloadBufferArr);
    }

    // Store the states of downloading a single file
    private class DownloadStates
    {
        public required string Url { get; init; }
        public required string LocalPath { get; init; }
        public long? TotalBytes { get; set; }

        // Used by chunk organization in multipart download
        public required long ChunkSize { get; init; }

        public long _chunkScheduled = 0;
        public long TotalChunks { get; set; } = 0;
        private readonly object _chunkOrganizerLock = new();

        // Returns the indices (inclusive) of bytes in the next chunk if there are more chunks to download
        public (long start, long end)? NextChunk()
        {
            long totalBytes = (long)TotalBytes!; // Not null in multipart download
            long start, end;
            lock (_chunkOrganizerLock)
            {
                if (_chunkScheduled == TotalChunks)
                    return null;
                start = _chunkScheduled * ChunkSize;
                _chunkScheduled++;
            }
            // Handle the last chunk
            end = Math.Min(start + ChunkSize, totalBytes) - 1;
            return (start, end);
        }
    }

    public async Task<GroupDownloadResult> DownloadFilesAsync(GroupDownloadRequest request, CancellationToken cancellationToken = default)
    {
        IGroupDownloadRequest groupReq = request;
        List<(DownloadRequest, DownloadResult)> failed = new();
        List<Task> downloadTasks = new();
        foreach (var req in request.Files)
        {
            string url = req.Url;
            string localPath = req.LocalPath;

            if (_mirror is not null)
                url = _mirror.GetMirrorUrl(url);

            Task downloadTask = DownloadFileDriverAsync(req, cancellationToken).ContinueWith((t) =>
            {
                if (t.IsCanceled)
                {
                    var result = new DownloadResult(DownloadResultType.Cancelled);
                    groupReq.OnSingleRequestCompleted(req, result);
                }
                else if (t.IsFaulted)
                {
                    var result = new DownloadResult(DownloadResultType.Failed)
                    {
                        Exception = t.Exception
                    };
                    failed.Add((req, result));
                    groupReq.OnSingleRequestCompleted(req, result);
                }
                else
                {
                    var result = new DownloadResult(DownloadResultType.Successful);
                    groupReq.OnSingleRequestCompleted(req, result);
                }
            });
            downloadTasks.Add(downloadTask);
        }

        await Task.WhenAll(downloadTasks);

        DownloadResultType type = DownloadResultType.Successful;
        if (cancellationToken.IsCancellationRequested)
            type = DownloadResultType.Cancelled;
        else if (failed.Count > 0)
            type = DownloadResultType.Failed;

        return new GroupDownloadResult
        {
            Failed = failed,
            Type = type
        };
    }

    private record class DownloaderConfig(HttpClient HttpClient, long ChunkSize, int WorkersPerDownloadTask, int ConcurrentDownloadTasks);
}
