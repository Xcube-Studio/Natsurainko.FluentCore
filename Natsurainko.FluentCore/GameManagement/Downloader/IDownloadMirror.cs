namespace Nrk.FluentCore.GameManagement.Downloader;

public interface IDownloadMirror
{
    public string GetMirrorUrl(string sourceUrl);
}
