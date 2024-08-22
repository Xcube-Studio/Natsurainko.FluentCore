using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Experimental.GameManagement.Instances;
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
    public event EventHandler<(DownloadRequest, DownloadResult)>? DependencyDownloaded;
    public event EventHandler<IEnumerable<MinecraftDependency>>? InvalidDependenciesDetermined;

    private readonly List<MinecraftDependency> _dependencies = new();
    private readonly MinecraftInstance _instance;

    public DependencyResolver(MinecraftInstance instance, IEnumerable<MinecraftDependency>? extraDependencies = null)
    {
        if (extraDependencies is not null)
            _dependencies.AddRange(extraDependencies);

        _instance = instance;
    }

    public async Task<GroupDownloadResult> VerifyAndDownloadDependenciesAsync(IDownloader? downloader = null, int fileVerificationParallelism = 10, CancellationToken cancellationToken = default)
    {
        if (fileVerificationParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(fileVerificationParallelism));

        downloader ??= HttpUtils.Downloader;

        // TODO: 特殊情况处理
        // 1. Forge端URL无法下载，需要替换
        // 2. OptiFine部分依赖需要从安装器提取

        // 1. Find all dependencies

        // 1.1 Libraries
        var (libs, nativeLibs) = _instance.GetRequiredLibraries();
        _dependencies.AddRange(libs);
        _dependencies.AddRange(nativeLibs);

        if (_instance is ModifiedMinecraftInstance modInstance)
        {
            if (modInstance.HasInheritance)
            {
                (libs, nativeLibs) = modInstance.InheritedMinecraftInstance.GetRequiredLibraries();
                _dependencies.AddRange(libs);
                _dependencies.AddRange(nativeLibs);
            }
        }

        // 1.2 Asset index
        var assetIndex = _instance.GetAssetIndex();
        bool isAssetIndexValid = await VerifyDependencyAsync(assetIndex, cancellationToken);
        if (!isAssetIndexValid)
        {
            var result = await downloader.CreateDownloadTask(assetIndex.Url, assetIndex.FullPath).StartAsync();

            if (result.Type == DownloadResultType.Failed)
                throw new Exception("依赖材质索引文件获取失败");
        }

        // 1.3 Assets
        var assets = _instance.GetRequiredAssets();
        _dependencies.AddRange(assets);

        // 1.3 Client Jar
        var jar = _instance.GetJarElement();
        if (jar != null)
            _dependencies.Add(jar);

        // 2. Verify dependencies

        // This is IO bound operation, using TPL is inefficient
        // TODO: Test performance of this implementation
        SemaphoreSlim semaphore = new(fileVerificationParallelism, fileVerificationParallelism);
        ConcurrentBag<MinecraftDependency> invalidDeps = new();

        var tasks = _dependencies.Select(async dep =>
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

        // 3. Download invalid dependencies
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
