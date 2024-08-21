using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Dependencies;

public class DependencyResolver
{
    public IReadOnlyList<MinecraftDependency> Dependencies { get; init; }

    public event EventHandler<(DownloadRequest, DownloadResult)>? DependencyDownloaded;
    public event EventHandler<IEnumerable<MinecraftDependency>>? InvalidDependenciesDetermined;

    public static DependencyResolverBuilder CreateBuilder() => new();

    public DependencyResolver(IEnumerable<MinecraftDependency> deps)
    {
        Dependencies = new List<MinecraftDependency>(deps);
    }

    public async Task<GroupDownloadResult> VerifyAndDownloadDependenciesAsync(IDownloader? downloader = null, int fileVerificationParallelism = 10, CancellationToken cancellationToken = default)
    {
        if (fileVerificationParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(fileVerificationParallelism));

        downloader ??= HttpUtils.Downloader;

        // Verify
        // This is IO bound operation, using TPL is inefficient
        // TODO: Test performance of this implementation
        SemaphoreSlim semaphore = new(fileVerificationParallelism, fileVerificationParallelism);
        ConcurrentBag<MinecraftDependency> invalidDeps = new();

        var tasks = Dependencies.Select(async dep =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!await VerifyDependencyAsync(dep, cancellationToken))
                {
                    lock (invalidDeps)
                    {
                        invalidDeps.Add(dep);
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks);
        InvalidDependenciesDetermined?.Invoke(this, invalidDeps);

        // Download
        var downloadItems = invalidDeps.Select(dep => new DownloadRequest(dep.Url, dep.FullPath));
        var groupRequest = new GroupDownloadRequest(downloadItems);
        groupRequest.SingleRequestCompleted += (request, result) =>
        {
            DependencyDownloaded?.Invoke(this, (request, result));
        };
        return await downloader.DownloadFilesAsync(groupRequest, cancellationToken);
    }

    // TODO: change to private
    public static async Task<bool> VerifyDependencyAsync(MinecraftDependency dep, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(dep.FullPath))
            return false;

        using var fileStream = File.OpenRead(dep.FullPath);
        byte[] sha1Bytes = await SHA1.HashDataAsync(fileStream, cancellationToken);
        string sha1Str = BitConverter.ToString(sha1Bytes).Replace("-", string.Empty).ToLower();

        return sha1Str == dep.Sha1;
    }
}

public class DependencyResolverBuilder
{
    private readonly List<MinecraftDependency> _deps = new();

    public DependencyResolverBuilder AddDependencies(IEnumerable<MinecraftDependency> deps)
    {
        _deps.AddRange(deps);
        return this;
    }

    public DependencyResolver Build()
    {
        return new DependencyResolver(_deps);
    }
}