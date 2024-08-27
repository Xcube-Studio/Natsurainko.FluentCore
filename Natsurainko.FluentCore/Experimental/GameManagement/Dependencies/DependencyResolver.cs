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
    private readonly List<MinecraftDependency> _dependencies = [];
    private readonly MinecraftInstance _instance;

    public event EventHandler<(DownloadRequest, DownloadResult)>? DependencyDownloaded;
    public event EventHandler<IEnumerable<MinecraftDependency>>? InvalidDependenciesDetermined;

    /// <summary>
    /// 允许检查继承的依赖
    /// </summary>
    public bool AllowInheritedDependencies { get; init; } = true;

    /// <summary>
    /// 允许检查 Assets (包含 AssetIndex 文件的检查与下载)
    /// </summary>
    public bool AllowVerifyAssets { get; init; } = true;

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

        #region 1.1 Libraries & Inherited Libraries

        var (libs, nativeLibs) = _instance.GetRequiredLibraries();
        _dependencies.AddRange(libs);
        _dependencies.AddRange(nativeLibs);

        if (AllowInheritedDependencies 
            && _instance is ModifiedMinecraftInstance modInstance 
            && modInstance.HasInheritance)
        {
            (libs, nativeLibs) = modInstance.InheritedMinecraftInstance.GetRequiredLibraries();
            _dependencies.AddRange(libs);
            _dependencies.AddRange(nativeLibs);
        }

        #endregion

        #region 1.2 Client.jar

        var jar = _instance.GetJarElement();
        if (jar != null)
        {
            _dependencies.Add(jar);
        }

        #endregion

        #region 1.3 AssetIndex & Assets

        if (AllowVerifyAssets)
        {
            var assetIndex = _instance.GetAssetIndex();

            if (!await VerifyDependencyAsync(assetIndex, cancellationToken))
            {
                var result = await downloader.CreateDownloadTask(assetIndex.Url, assetIndex.FullPath).StartAsync(cancellationToken);

                if (result.Type == DownloadResultType.Failed)
                {
                    throw new Exception("Failed to obtain the dependent material index file");
                }
            }

            _dependencies.AddRange(_instance.GetRequiredAssets());
        }

        #endregion

        // 2. Verify dependencies

        // This is IO bound operation, using TPL is inefficient
        // TODO: Test performance of this implementation
        SemaphoreSlim semaphore = new(fileVerificationParallelism, fileVerificationParallelism);
        ConcurrentBag<MinecraftDependency> invalidDeps = [];

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
        var downloadItems = invalidDeps.Where(dep => dep is IDownloadableDependency)
            .OfType<IDownloadableDependency>()
            .Select(dep => new DownloadRequest(dep.Url, dep.FullPath));

        var groupRequest = new GroupDownloadRequest(downloadItems);
        groupRequest.SingleRequestCompleted += (request, result) => DependencyDownloaded?.Invoke(this, (request, result));

        return await downloader.DownloadFilesAsync(groupRequest, cancellationToken);
    }

    // TODO: change to private
    public static async Task<bool> VerifyDependencyAsync(MinecraftDependency dep, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(dep.FullPath))
            return false;

        if (dep is not IVerifiableDependency verifiableDependency)
            return true;

        if (verifiableDependency.Sha1 != null)
        {
            using var fileStream = File.OpenRead(dep.FullPath);
            byte[] sha1Bytes = await SHA1.HashDataAsync(fileStream, cancellationToken);
            string sha1Str = BitConverter.ToString(sha1Bytes).Replace("-", string.Empty).ToLower();

            return sha1Str == verifiableDependency.Sha1;
        }

        if (verifiableDependency.Size != null)
        {
            var file = new FileInfo(dep.FullPath);
            return verifiableDependency.Size == file.Length;
        }

        return true;
    }
}
