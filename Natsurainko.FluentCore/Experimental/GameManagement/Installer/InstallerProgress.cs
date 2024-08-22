using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Nrk.FluentCore.Utils.IProgressReporter.ProgressData;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer;

// Installer specific
public enum VanillaInstallationStage
{
    DownloadVersionJson,
    DownloadAssetIndexJson,
    DownloadMinecraftDependencies
}

// public enum ForgeInstallationStage

// Generic
public enum ProgressType
{
    Finished,
    Failed,
    Running,
    UpdateTotalTasks,
    UpdateFinishedTasks,
    UpdateAllTasks,
    IncrementFinishedTasks
}

public record struct InstallerProgressPayload(ProgressType Type, int? FinishedTasks, int? TotalTasks);

public struct InstallerProgress<TStage> where TStage : notnull
{
    public TStage Stage { get; init; }

    public InstallerProgressPayload Payload { get; init; }
}

// Used in Launcher
public class InstallerProgressReporter<TStage> : IProgress<InstallerProgress<TStage>>
    where TStage : notnull
{
    public Dictionary<TStage, InstallerStageViewModel> Stages { get; } = new();

    public void Report(InstallerProgress<TStage> value)
    {
        var vm = Stages[value.Stage];
        vm.UpdateProgress(value.Payload);
    }
}

public class InstallerStageViewModel // INotifyPropertyChanged
{
    public string TaskName { get; set; } = "";

    public State TaskState { get; set; } = State.Prepared;

    public int TotalTasks { get; set; } = 1;

    public int FinishedTasks { get; set; }

    public void UpdateProgress(InstallerProgressPayload payload)
    {
        switch (payload.Type)
        {
            case ProgressType.Finished:
                break;
            case ProgressType.Failed:
                break;
            case ProgressType.Running:
                break;
            case ProgressType.UpdateTotalTasks:
                break;
            case ProgressType.UpdateFinishedTasks:
                break;
            case ProgressType.UpdateAllTasks:
                break;
            case ProgressType.IncrementFinishedTasks:
                break;
            default:
                break;
        }
    }
}