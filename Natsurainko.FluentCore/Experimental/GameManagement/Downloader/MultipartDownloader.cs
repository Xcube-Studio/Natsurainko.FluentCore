using Nrk.FluentCore.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Downloader;

public class MultipartDownloader
{
    public long ChunkSize { get; init; }
    public int NumberOfConcurrentTasks { get; init; }

    private readonly HttpClient _httpClient;

    public MultipartDownloader(HttpClient? httpClient, long chunkSize = 1048576 /* 1MB */, int numConcurrentTasks = 4)
    {
        _httpClient = httpClient ?? HttpUtils.HttpClient;
        ChunkSize = chunkSize;
        NumberOfConcurrentTasks = numConcurrentTasks;
    }

    public IDownloadTask DownloadFileAsync(string url, string localPath, CancellationToken cancellationToken)
        => new DownloadTask(url, localPath, _httpClient, ChunkSize, NumberOfConcurrentTasks, cancellationToken);

    private class DownloadTask : IDownloadTask
    {
        // Download item info
        public string Url { get; init; }
        public string LocalPath { get; init; }

        #region Download task states

        private string _redirectedUrl;
        private long _chunkScheduled = 0;
        private long _totalChunks = 0;
        private object _chunkOrganizerLock = new();
        private long _totalBytes = -1; // Set when Status is Downloading | Completed | Failed | Cancelled
        private long _downloadedBytes = 0; // ref long required by Interlock.Add

        public long? TotalBytes { get => _totalChunks == -1 ? null : _totalBytes; }

        public long DownloadedBytes { get => _downloadedBytes; }

        // Task status
        public Task Task { get; init; }
        public DownloadStatus Status { get; private set; } = DownloadStatus.Preparing; // Replace with a type union in future C# versions
        public Exception? Exception { get; private set; } = null; // Set when Status is Failed

        #endregion

        // Events
        public event EventHandler<long?>? FileSizeReceived;
        public event EventHandler<int>? BytesDownloaded;

        // Downloader config
        private readonly HttpClient _httpClient;
        private readonly long _chunkSize;
        private readonly int _numConcurrentTasks;

        const int DownloadBufferSize = 4096; // 4 KB

        public DownloadTask(
            string url, string localPath,
            HttpClient? httpClient = null, long chunkSize = 1048576 /* 1MB */, int numConcurrentTasks = 4,
            CancellationToken cancellationToken = default)
        {
            Url = url;
            LocalPath = localPath;
            _redirectedUrl = url;

            _httpClient = httpClient ?? HttpUtils.HttpClient;
            _chunkSize = chunkSize;
            _numConcurrentTasks = numConcurrentTasks;

            Task = DownloadFileDriverAsync(cancellationToken);
        }

        private async Task DownloadFileDriverAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await DownloadFileAsync(cancellationToken);
                Status = DownloadStatus.Completed;
            }
            catch (TaskCanceledException)
            {
                Status = DownloadStatus.Cancelled;
            }
            catch (Exception e)
            {
                Status = DownloadStatus.Failed;
                Exception = e.InnerException ?? e;
            }
        }

        public TaskAwaiter GetAwaiter() => Task.GetAwaiter();

        private async Task DownloadFileAsync(CancellationToken cancellationToken = default)
        {
            // Try to get the size of the file
            (var response, _redirectedUrl) = await PrepareForDownloadAsync(Url, cancellationToken);

            // Use multi-part download if Content-Length is provided and the remote server supports range requests
            // Fall back to single part download if the remote server does not provide a Content-Length or does not support range requests
            bool useMultiPart = false;
            if (response.Content.Headers.ContentLength is long contentLength)
            {
                _totalBytes = contentLength;
                // Check support for range requests
                if (response.Headers.AcceptRanges.Contains("bytes")) // Check if the server supports range requests by checking the Accept-Ranges header
                {
                    useMultiPart = true;
                }
                else // Check if the server supports range requests by sending a range request
                {
                    var rangeRequest = new HttpRequestMessage(HttpMethod.Get, _redirectedUrl);
                    rangeRequest.Headers.Range = new RangeHeaderValue(0, 0); // Request first byte
                    var rangeResponse = await _httpClient.SendAsync(rangeRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    useMultiPart = rangeResponse.StatusCode == HttpStatusCode.PartialContent;
                }
            }

            // Status changed
            Status = DownloadStatus.Downloading;
            FileSizeReceived?.Invoke(this, TotalBytes);

            // Ensure destination directory exists
            string? destinationDir = Path.GetDirectoryName(LocalPath);
            if (destinationDir is not null) // destinationDir is not root directory
                Directory.CreateDirectory(destinationDir); // Create the directory if it doesn't exist

            // Try multi-part download if Content-Length is provided and the file size is larger than a threshold
            if (useMultiPart)
            {
                await DownloadMultiPartAsync(_totalBytes, cancellationToken);
            }
            else
            {
                await DownloadSinglePartAsync(cancellationToken);
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

        private async Task DownloadSinglePartAsync(CancellationToken cancellationToken = default)
        {
            // Send a GET request to start downloading the file
            using var response = await _httpClient.GetAsync(_redirectedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Prepare streams and download buffer
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(LocalPath, FileMode.Create, FileAccess.Write);
            if (TotalBytes is long size)
                fileStream.SetLength(size);

            // Download the file
            byte[] downloadBufferArr = ArrayPool<byte>.Shared.Rent(DownloadBufferSize);
            Memory<byte> downloadBuffer = downloadBufferArr.AsMemory(0, DownloadBufferSize);
            await WriteStreamToFile(contentStream, fileStream, downloadBuffer, cancellationToken);
            ArrayPool<byte>.Shared.Return(downloadBufferArr);
        }

        private async Task WriteStreamToFile(Stream contentStream, FileStream fileStream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int bytesRead = 0;
            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer[0..bytesRead], cancellationToken);
                Interlocked.Add(ref _downloadedBytes, bytesRead);
                BytesDownloaded?.Invoke(this, bytesRead);
            }
        }

        // Returns the indices (inclusive) of bytes in the next chunk if there are more chunks to download
        private (long start, long end)? NextChunk()
        {
            long start, end;
            lock (_chunkOrganizerLock)
            {
                if (_chunkScheduled == _totalChunks)
                    return null;
                start = _chunkScheduled * _chunkSize;
                _chunkScheduled++;
            }
            // Handle the last chunk
            end = Math.Min(start + _chunkSize, _totalBytes) - 1;
            return (start, end);
        }

        private async Task DownloadMultiPartAsync(long fileSize, CancellationToken cancellationToken = default)
        {
            long totalChunks = Math.DivRem(fileSize, _chunkSize, out long remainder);
            if (remainder > 0)
                totalChunks++;
            _totalChunks = totalChunks;

            // Pre-allocate the file with the desired size
            using var fileStream = new FileStream(LocalPath, FileMode.Create, FileAccess.Write, FileShare.Write);
            fileStream.SetLength(fileSize);

            // Initialize workers
            int numberOfWorkers = (int)Math.Min(_numConcurrentTasks, totalChunks);
            Task[] workers = new Task[numberOfWorkers];
            for (int i = 0; i < numberOfWorkers; i++)
            {
                workers[i] = MultipartDownloadWorker(cancellationToken);
            }
            await Task.WhenAll(workers);
        }

        private async Task MultipartDownloadWorker(CancellationToken cancellationToken)
        {
            // Resources for this worker only
            using var fileStream = new FileStream(LocalPath, FileMode.Open, FileAccess.Write, FileShare.Write);

            // Download the file
            byte[] downloadBufferArr = ArrayPool<byte>.Shared.Rent(DownloadBufferSize);
            Memory<byte> downloadBuffer = downloadBufferArr.AsMemory(0, DownloadBufferSize);

            while (NextChunk() is (long start, long end))
            {
                // Start writing at the beginning of the chunk
                fileStream.Seek(start, SeekOrigin.Begin);

                // Send a range request to download the chunk of the file
                var request = new HttpRequestMessage(HttpMethod.Get, _redirectedUrl);
                request.Headers.Range = new RangeHeaderValue(start, end);
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Write to the file
                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await WriteStreamToFile(contentStream, fileStream, downloadBuffer, cancellationToken);
            }

            ArrayPool<byte>.Shared.Return(downloadBufferArr);
        }
    }

}

