namespace Nrk.FluentCore.GameManagement.Dependencies;

public interface IDownloadableDependency
{
    /// <summary>
    /// URL to download the file
    /// </summary>
    string Url { get; }

    string FullPath { get; }
}
