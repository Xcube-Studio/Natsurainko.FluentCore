using Nrk.FluentCore.Experimental.GameManagement.Downloader;
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
    public IReadOnlyList<GameDependency> Dependencies { get; init; }

    public event EventHandler<IDownloadTask>? DependencyDownloaded;
    public event EventHandler<IEnumerable<GameDependency>>? InvalidDependenciesDetermined;


    public static DependencyResolverBuilder CreateBuilder() => new();

    public DependencyResolver(IEnumerable<GameDependency> deps)
    {
        Dependencies = new List<GameDependency>(deps);
    }

    public async Task VerifyAndDownloadDependenciesAsync(IDownloader downloader, int fileVerificationParallelism, CancellationToken cancellationToken = default)
    {
        if (fileVerificationParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(fileVerificationParallelism));

        // Verify
        // This is IO bound operation, using TPL is inefficient
        // TODO: Test performance of this implementation
        SemaphoreSlim semaphore = new(0, fileVerificationParallelism);
        ConcurrentBag<GameDependency> invalidDeps = new();

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
        var downloadItems = invalidDeps.Select(dep => (dep.Url, dep.FullPath));
        IDownloadTaskGroup taskGroup = downloader.DownloadFilesAsync(downloadItems, cancellationToken);
        // FIXME: a task may have already completed before the event handler is attached
        taskGroup.DownloadTaskCompleted += (_, task) => DependencyDownloaded?.Invoke(this, task);
        await taskGroup;
    }

    private static async Task<bool> VerifyDependencyAsync(GameDependency dep, CancellationToken cancellationToken = default)
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
    private readonly List<GameDependency> _deps = new();

    public DependencyResolverBuilder AddDependencies(IEnumerable<GameDependency> deps)
    {
        _deps.AddRange(deps);
        return this;
    }

    public DependencyResolver Build()
    {
        return new DependencyResolver(_deps);
    }
}