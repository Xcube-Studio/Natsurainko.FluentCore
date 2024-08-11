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

    public async Task<DownloadResult> DownloadFileAsync(
        string url, string localPath,
        Action<long?>? fileSizeReceivedCallback = null,
        Action<long>? bytesDownloadedCallback = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await DownloadFileDriverAsync(url, localPath, fileSizeReceivedCallback, bytesDownloadedCallback, cancellationToken);
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

    public async Task DownloadFileDriverAsync(
        string url, string localPath,
        Action<long?>? fileSizeReceivedCallback = null,
        Action<long>? bytesDownloadedCallback = null,
        CancellationToken cancellationToken = default)
    {
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
        fileSizeReceivedCallback?.Invoke(states.TotalBytes);

        // Ensure destination directory exists
        string? destinationDir = Path.GetDirectoryName(localPath);
        if (destinationDir is not null) // destinationDir is not root directory
            Directory.CreateDirectory(destinationDir); // Create the directory if it doesn't exist

        // Try multi-part download if Content-Length is provided and the file size is larger than a threshold
        if (useMultiPart)
        {
            await DownloadMultiPartAsync(states, bytesDownloadedCallback, cancellationToken);
        }
        else
        {
            await DownloadSinglePartAsync(states, bytesDownloadedCallback, cancellationToken);
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

    private async Task DownloadSinglePartAsync(DownloadStates states, Action<long>? bytesDownloadedCallback = null, CancellationToken cancellationToken = default)
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
        await WriteStreamToFile(contentStream, fileStream, downloadBuffer, bytesDownloadedCallback, cancellationToken);
        ArrayPool<byte>.Shared.Return(downloadBufferArr);
    }

    private async Task WriteStreamToFile(Stream contentStream, FileStream fileStream, Memory<byte> buffer, Action<long>? bytesDownloadedCallback = null, CancellationToken cancellationToken = default)
    {
        int bytesRead = 0;
        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer[0..bytesRead], cancellationToken);
            bytesDownloadedCallback?.Invoke(bytesRead);
        }
    }


    private async Task DownloadMultiPartAsync(DownloadStates states, Action<long>? bytesDownloadedCallback = null, CancellationToken cancellationToken = default)
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
            workers[i] = MultipartDownloadWorker(states, bytesDownloadedCallback, cancellationToken);
        }
        await Task.WhenAll(workers);
    }

    private async Task MultipartDownloadWorker(DownloadStates states, Action<long>? bytesDownloadedCallback = null, CancellationToken cancellationToken = default)
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
            await WriteStreamToFile(contentStream, fileStream, downloadBuffer, bytesDownloadedCallback, cancellationToken);
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

    public async Task<GroupDownloadResult> DownloadFilesAsync(
        IEnumerable<(string url, string localPath)> files,
        Action<(string url, string localPath), DownloadResult>? downloadTaskFinishedCallback = null,
        CancellationToken cancellationToken = default)
    {
        List<((string url, string localPath), DownloadResult)> failed = new();
        List<((string url, string localPath), DownloadResult)> cancelled = new();
        List<Task> downloadTasks = new();
        foreach ((string url, string localPath) in files)
        {
            string mirrorUrl = url;
            if (_mirror is not null)
                mirrorUrl = _mirror.GetMirrorUrl(url);

            Task downloadTask = DownloadFileDriverAsync(url, localPath, null, null, cancellationToken).ContinueWith((t) =>
            {
                if (t.IsCanceled)
                {
                    var result = new DownloadResult(DownloadResultType.Cancelled);
                    cancelled.Add(((url, localPath), result));
                }
                else if (t.IsFaulted)
                {
                    var result = new DownloadResult(DownloadResultType.Failed)
                    {
                        Exception = t.Exception
                    };
                    failed.Add(((url, localPath), result));
                    downloadTaskFinishedCallback?.Invoke((url, localPath), result);
                }
                else
                {
                    var result = new DownloadResult(DownloadResultType.Successful);
                    downloadTaskFinishedCallback?.Invoke((url, localPath), result);
                }
            });
            downloadTasks.Add(downloadTask);
        }

        await Task.WhenAll(downloadTasks);

        DownloadResultType type = DownloadResultType.Successful;
        if (cancelled.Count > 0)
            type = DownloadResultType.Cancelled;
        else if (failed.Count > 0)
            type = DownloadResultType.Failed;
        return new GroupDownloadResult
        {
            Cancelled = cancelled,
            Failed = failed,
            Type = type
        };
    }

    public Task DownloadFilesDriverAsync(
        IEnumerable<(string url, string localPath)> files,
        Action<(string url, string localPath)>? downloadTaskFinishedCallback = null,
        CancellationToken cancellationToken = default)
    {
        List<Task> tasks = new();
        foreach ((string url, string localPath) in files)
        {
            string mirrorUrl = url;
            if (_mirror is not null)
                mirrorUrl = _mirror.GetMirrorUrl(url);
            Task downloadTask = DownloadFileDriverAsync(url, localPath, null, null, cancellationToken).ContinueWith((_) =>
            {
                downloadTaskFinishedCallback?.Invoke((url, localPath));
            });
            tasks.Add(downloadTask);
        }
        return Task.WhenAll(tasks);
    }

    private record class DownloaderConfig(HttpClient HttpClient, long ChunkSize, int WorkersPerDownloadTask, int ConcurrentDownloadTasks);
}
