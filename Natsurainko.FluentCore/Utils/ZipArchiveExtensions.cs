using System.IO;
using System.IO.Compression;

namespace Nrk.FluentCore.Utils;

internal static class ZipArchiveExtensions
{
    public static string ReadAsString(this ZipArchiveEntry archiveEntry)
    {
        using var stream = archiveEntry.Open();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Extracts the ZipArchiveEntry to the specified destinationFile.
    /// </summary>
    /// <param name="zipArchiveEntry">A ZIP archive entry to be extracted</param>
    /// <param name="destinationFile">Path of destination file</param>
    public static void ExtractTo(this ZipArchiveEntry zipArchiveEntry, string destinationFile)
    {
        var file = new FileInfo(destinationFile);

        // file.Directory is null if destinationFile is a relative path, this will cause the file to be extracted to the directory of the executing assembly
        if (file.Directory is null)
            throw new DirectoryNotFoundException($"Directory of {destinationFile} not found");

        if (!file.Directory.Exists)
            file.Directory.Create();

        zipArchiveEntry.ExtractToFile(destinationFile, true);
    }
}
