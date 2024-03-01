namespace Nrk.FluentCore.Resources;

public record ModrinthFile
{
    public required string McVersion { get; set; }

    public required string FileName { get; set; }

    public required string Url { get; set; }

    public required string Loaders { get; set; }

    public string DisplayDescription => $"{Loaders}, {McVersion}";
}
