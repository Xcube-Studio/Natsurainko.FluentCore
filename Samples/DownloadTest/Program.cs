using Nrk.FluentCore.Experimental.GameManagement.Downloader;

using var httpClient = new HttpClient();
//MultipartDownloader downloader = new(1048576, 8, httpClient);

const string url = "https://github.com/Xcube-Studio/Natsurainko.FluentLauncher/releases/download/v2.2.9.0/Natsurainko.FluentLauncher_2.2.9.0.msixbundle";
const string path = @"D:\Downloads\Natsurainko.FluentLauncher_2.2.9.0.msixbundle";

//const string url = "http://httpbin.org/stream-bytes/1024";
//const string path = @"D:\Downloads\test.bin";

// Test cancellation
using var cts = new CancellationTokenSource();
var delay = Task.Run(async () =>
{
    await Task.Delay(5000);
    //cts.Cancel();
});

var downloadTask = new MultipartDownloaderDownloadTask(url, path, 1048576, 64, httpClient, cts.Token);

// Progress report
using Timer timer = new((state) =>
{
    long? totalBytes = downloadTask.TotalBytes;
    if (totalBytes is null || totalBytes == -1)
        return;
    Console.WriteLine($"Downloaded {downloadTask.DownloadedBytes} / {totalBytes} bytes ({100.0d * downloadTask.DownloadedBytes / totalBytes:0.##}%).");
}, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

// Download operation
await downloadTask;
if (downloadTask.Status == DownloadStatus.Cancelled)
{
    Console.WriteLine("Download task cancelled.");
}
else if (downloadTask.Status == DownloadStatus.Failed)
{
    Console.WriteLine("Download task failed.\nError: " + downloadTask.Exception?.Message);
}
else
{
    Console.WriteLine("Download completed.");
}
await delay;