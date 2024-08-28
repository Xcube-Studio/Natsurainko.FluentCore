using Nrk.FluentCore.GameManagement.Downloader;

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
    await Task.Delay(1000);
    //cts.Cancel();
});

var downloader = new MultipartDownloader(httpClient, 1048576, 16);

// Download operation
var downloadTask = downloader.CreateDownloadTask(url, path);

// Progress report
using Timer timer = new((state) =>
{
    if (downloadTask.TotalBytes is null) return;

    Console.WriteLine($"Downloaded {downloadTask.DownloadedBytes} / {downloadTask.TotalBytes} bytes ({100.0d * downloadTask.DownloadedBytes / downloadTask.TotalBytes:0.##}%).");
}, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

var result = await downloadTask.StartAsync(cts.Token);

if (result.Type == DownloadResultType.Cancelled)
{
    Console.WriteLine("Download task cancelled.");
}
else if (result.Type == DownloadResultType.Failed)
{
    Console.WriteLine("Download task failed.\nError: " + result.Exception!.Message);
}
else
{
    Console.WriteLine("Download completed.");
}
await delay;