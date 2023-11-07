namespace Nrk.FluentCore.GameResources.ThirdPartySources;

public interface IDownloadElement
{
    public string AbsolutePath { get; set; }

    public string Url { get; set; }

    public string Checksum { get; set; }
}
