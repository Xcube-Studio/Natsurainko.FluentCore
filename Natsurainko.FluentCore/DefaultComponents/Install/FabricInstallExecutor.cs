using Nrk.FluentCore.Classes.Datas.Download;
using Nrk.FluentCore.Classes.Datas.Install;
using Nrk.FluentCore.Components.Install;
using Nrk.FluentCore.DefaultComponents.Download;
using Nrk.FluentCore.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Nrk.FluentCore.DefaultComponents.Install;

public class FabricInstallExecutor : BaseInstallExecutor
{
    public required string JavaPath { get; set; }

    public required string PackageFilePath { get; set; }

    public DownloadMirrorSource MirrorSource { get; set; }

    private readonly List<string> _outputs = new();
    private readonly List<string> _errorOutputs = new();

    public override Task<InstallResult> ExecuteAsync() => Task.Run(() =>
    {
        RunProcessor();

        OnProgressChanged(1.0);

    }).ContinueWith(task =>
    {
        if (task.IsFaulted || _errorOutputs.Count > 0)
            return new InstallResult
            {
                Success = false,
                Exception = task.Exception,
                Log = _errorOutputs
            };

        return new InstallResult
        {
            Success = true,
            Exception = null,
            Log = _outputs
        };
    });

    private void RunProcessor()
    {
        OnProgressChanged(0.35);

        var args = new List<string>()
        {
            "-jar", PackageFilePath.ToPathParameter(),
            "client",
            "-dir", InheritedFrom.MinecraftFolderPath.ToPathParameter(),
            "-mcversion", InheritedFrom.AbsoluteId.ToPathParameter()
        };

        if (InheritedFrom.Type.Equals("snapshot"))
            args.Add("-snapshot");

        if (MirrorSource == DownloadMirrors.Mcbbs || MirrorSource == DownloadMirrors.Bmclapi)
        {
            args.Add("-mavenurl");
            args.Add(MirrorSource.LibrariesReplaceUrl["https://maven.fabricmc.net"] + "/");
        }

        using var process = Process.Start(new ProcessStartInfo(JavaPath)
        {
            UseShellExecute = false,
            WorkingDirectory = this.InheritedFrom.MinecraftFolderPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            Arguments = string.Join(' ', args)
        });

        void AddOutput(string data, bool error = false)
        {
            if (string.IsNullOrEmpty(data))
                return;

            _outputs.Add(data);
            if (error) _errorOutputs.Add(data);
        }

        process.OutputDataReceived += (_, args) => AddOutput(args.Data);
        process.ErrorDataReceived += (_, args) => AddOutput(args.Data, true);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        OnProgressChanged(0.9);
    }
}
