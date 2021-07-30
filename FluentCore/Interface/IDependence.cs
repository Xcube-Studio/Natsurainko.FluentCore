using FluentCore.Model;

namespace FluentCore.Interface
{
    public interface IDependence
    {
        HttpDownloadRequest GetDownloadRequest(string root);

        string GetRelativePath();
    }
}
