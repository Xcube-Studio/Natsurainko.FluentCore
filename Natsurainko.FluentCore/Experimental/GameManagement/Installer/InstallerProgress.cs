using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Nrk.FluentCore.Utils.IProgressReporter.ProgressData;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer;

// Generic
public enum InstallerStageProgressType
{
    Starting,

    UpdateTotalTasks,
    UpdateFinishedTasks,
    IncrementFinishedTasks,

    Finished,
    Failed,
}

public readonly record struct InstallerStageProgress(
    InstallerStageProgressType Type,
    int? FinishedTasks,
    int? TotalTasks)
{
    internal static InstallerStageProgress Starting()
        => new(InstallerStageProgressType.Starting, null, null);
    internal static InstallerStageProgress UpdateTotalTasks(int totalTasks)
        => new(InstallerStageProgressType.UpdateTotalTasks, null, totalTasks);
    internal static InstallerStageProgress UpdateFinishedTasks(int finishedTasks)
        => new(InstallerStageProgressType.UpdateFinishedTasks, finishedTasks, null);
    internal static InstallerStageProgress IncrementFinishedTasks()
        => new(InstallerStageProgressType.IncrementFinishedTasks, null, null);
    internal static InstallerStageProgress Finished()
        => new(InstallerStageProgressType.Finished, null, null);
    internal static InstallerStageProgress Failed()
        => new(InstallerStageProgressType.Failed, null, null);
}

public readonly record struct InstallerProgress<TStage>(
    TStage Stage,
    InstallerStageProgress StageProgress)
    where TStage : notnull;

// Used in Launcher
public class InstallerProgressReporter<TStage> : IProgress<InstallerProgress<TStage>>
    where TStage : notnull
{
    public Dictionary<TStage, InstallerStageViewModel> Stages { get; } = new();

    public InstallerProgressReporter(IReadOnlyDictionary<TStage, string> stageNames)
    {
        // Init stage view models
        foreach (var (stage, name) in stageNames)
        {
            Stages.Add(stage, new InstallerStageViewModel { TaskName = name });
        }
    }

    public void Report(InstallerProgress<TStage> value)
    {
        var vm = Stages[value.Stage];
        vm.UpdateProgress(value.StageProgress);
    }
}

public class InstallerStageViewModel // INotifyPropertyChanged
{
    public required string TaskName { get; init; }

    public State TaskState { get; set; } = State.Prepared;

    public int TotalTasks { get; set; } = 1;

    public int FinishedTasks { get; set; }

    public void UpdateProgress(InstallerStageProgress payload)
    {
        switch (payload.Type)
        {
            case InstallerStageProgressType.Starting:
                break;

            case InstallerStageProgressType.UpdateTotalTasks:
                break;
            case InstallerStageProgressType.UpdateFinishedTasks:
                break;
            case InstallerStageProgressType.IncrementFinishedTasks:
                break;

            case InstallerStageProgressType.Finished:
                break;
            case InstallerStageProgressType.Failed:
                break;

            default:
                break;
        }
    }
}