using Nrk.FluentCore.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.GameManagement.Downloader;

public class MultipartDownloader : IDownloader
{
    private readonly SemaphoreSlim _globalDownloadTasksSemaphore;
    private readonly HttpClient _httpClient;
    private const int DownloadBufferSize = 4096; // 4 KB

    public HttpClient HttpClient => _httpClient;

    public long ChunkSize { get; init; }

    public int WorkersPerDownloadTask { get; init; }

    public int ConcurrentDownloadTasks { get; init; }

    public int MaxRetryCount { get; init; }

    public IDownloadMirror? DownloadMirror { get; init; }

    public bool EnableMultiPartDownload { get; init; }

    public MultipartDownloader(
        HttpClient? httpClient,
        long chunkSize = 1048576 /* 1MB */,
        int workersPerDownloadTask = 16,
        int concurrentDownloadTasks = 5,
        IDownloadMirror? mirror = null,
        bool enableMultiPartDownload = true,
        int maxRetryCount = 8)
    {
        _httpClient = httpClient ?? HttpUtils.HttpClient;

        ChunkSize = chunkSize;
        WorkersPerDownloadTask = workersPerDownloadTask;
        ConcurrentDownloadTasks = concurrentDownloadTasks;
        MaxRetryCount = maxRetryCount;
        DownloadMirror = mirror;
        EnableMultiPartDownload = enableMultiPartDownload;

        _globalDownloadTasksSemaphore = new SemaphoreSlim(concurrentDownloadTasks, concurrentDownloadTasks);
    }

    public async Task<DownloadResult> DownloadFileAsync(DownloadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Limits the number of concurrent download tasks
            await _globalDownloadTasksSemaphore.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new DownloadResult(DownloadResultType.Cancelled);
        }

        try
        {
            while (true)
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
                    if (request.AttemptCount >= MaxRetryCount)
                        return new DownloadResult(DownloadResultType.Failed) { Exception = e };
                }
                finally
                {
                    request.AttemptCount++;
                }
            }
        }
        finally
        {
            _globalDownloadTasksSemaphore.Release();
        }
    }

    public async Task DownloadFileDriverAsync(DownloadRequest request, CancellationToken cancellationToken = default)
    {
        string url = request.Url;
        string localPath = request.LocalPath;

        if (DownloadMirror is not null)
            url = DownloadMirror.GetMirrorUrl(url);

        // Try to get the size of the file
        (var response, url) = await PrepareForDownloadAsync(url, cancellationToken);
        DownloadStates states = new()
        {
            Url = url,
            LocalPath = localPath,
            ChunkSize = ChunkSize
        };

        // Use multi-part download if Content-Length is provided and the remote server supports range requests
        // Fall back to single part download if the remote server does not provide a Content-Length or does not support range requests
        bool useMultiPart = false;
        if (EnableMultiPartDownload && response.Content.Headers.ContentLength is long contentLength)
        {
            states.TotalBytes = contentLength;
            // Commented: some servers return AcceptRange="bytes" while they return 404 for range requests
            // Check support for range requests
            //if (response.Headers.AcceptRanges.Contains("bytes")) // Check if the server supports range requests by checking the Accept-Ranges header
            //{
            //    useMultiPart = true;
            //}
            //else // Check if the server supports range requests by sending a range request
            //{
            using var rangeRequest = new HttpRequestMessage(HttpMethod.Get, url);
            rangeRequest.Headers.Range = new RangeHeaderValue(0, 0); // Request first byte
            using var rangeResponse = await _httpClient.SendAsync(rangeRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            useMultiPart = rangeResponse.StatusCode == HttpStatusCode.PartialContent;
            //}
        }

        // Status changed
        request.FileSizeReceived?.Invoke(states.TotalBytes);

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
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
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

    private async Task DownloadSinglePartAsync(DownloadStates states, DownloadRequest request, CancellationToken cancellationToken = default)
    {
        // Send a GET request to start downloading the file
        using var response = await _httpClient.GetAsync(states.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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

    private async Task WriteStreamToFile(Stream contentStream, FileStream fileStream, Memory<byte> buffer, DownloadRequest request, CancellationToken cancellationToken = default)
    {
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer[0..bytesRead], cancellationToken);
            request.BytesDownloaded?.Invoke(bytesRead);
        }
    }

    private async Task DownloadMultiPartAsync(DownloadStates states, DownloadRequest request, CancellationToken cancellationToken = default)
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

    private async Task MultipartDownloadWorker(DownloadStates states, DownloadRequest downloadRequest, CancellationToken cancellationToken = default)
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
            using var request = new HttpRequestMessage(HttpMethod.Get, states.Url);
            request.Headers.Range = new RangeHeaderValue(start, end);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Write to the file
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await WriteStreamToFile(contentStream, fileStream, downloadBuffer, downloadRequest, cancellationToken);
        }

        ArrayPool<byte>.Shared.Return(downloadBufferArr);
    }

    private async Task DownloadFileInGroupAsync(DownloadRequest request, GroupDownloadRequest groupRequest, List<(DownloadRequest, DownloadResult)> failed, CancellationToken cancellationToken)
    {
        DownloadResult result = await DownloadFileAsync(request, cancellationToken);
        if (result.Type == DownloadResultType.Failed)
            failed.Add((request, result));
        groupRequest.SingleRequestCompleted?.Invoke(request, result);
    }

    public async Task<GroupDownloadResult> DownloadFilesAsync(GroupDownloadRequest request, CancellationToken cancellationToken = default)
    {
        List<(DownloadRequest, DownloadResult)> failed = [];
        List<Task> downloadTasks = [];

        foreach (var req in request.Files)
        {
            if (DownloadMirror is not null)
                req.Url = DownloadMirror.GetMirrorUrl(req.Url);

            downloadTasks.Add(DownloadFileInGroupAsync(req, request, failed, cancellationToken));
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

    //private record class DownloaderConfig(
    //    HttpClient HttpClient,
    //    long ChunkSize,
    //    int WorkersPerDownloadTask,
    //    int ConcurrentDownloadTasks,
    //    IDownloadMirror? Mirror,
    //    bool EnableMultiPartDownload = true,
    //    int MaxRetryCount = 8);
}
