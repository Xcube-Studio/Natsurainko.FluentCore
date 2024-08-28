namespace Nrk.FluentCore.GameManagement.Dependencies;

public interface IVerifiableDependency
{
    /// <summary>
    /// Expected size of the file in bytes
    /// </summary>
    long? Size { get; }

    /// <summary>
    /// Expected SHA1 of the file
    /// </summary>
    string? Sha1 { get; }
}
