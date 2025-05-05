using Nrk.FluentCore.GameManagement.Downloader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nrk.FluentCore.Exceptions;

/// <summary>
/// 依赖补全不完整错误
/// </summary>
public class IncompleteDependenciesException(
    IReadOnlyList<(DownloadRequest, DownloadResult)> failed, 
    string message) : Exception(GenerateMessage(failed, message))
{
    public IReadOnlyList<(DownloadRequest, DownloadResult)> Failed { get; init; } = failed;

    private static string GenerateMessage(IReadOnlyList<(DownloadRequest, DownloadResult)> failed, string message)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine(message);
        stringBuilder.AppendLine(string.Empty);

        foreach (var (request, result) in failed)
            stringBuilder.AppendLine($"<{Path.GetFileName(request.LocalPath)}>[{request.Url}]: {result.Exception?.Message}");

        return stringBuilder.ToString();
    }
}
