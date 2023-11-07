namespace Nrk.FluentCore.GameResources.ThirdPartySources;

public record ModrinthFile
{
    public string McVersion { get; set; }

    public string FileName { get; set; }

    public string Url { get; set; }

    public string Loaders { get; set; }

    public string DisplayDescription => $"{Loaders}, {McVersion}";
}
