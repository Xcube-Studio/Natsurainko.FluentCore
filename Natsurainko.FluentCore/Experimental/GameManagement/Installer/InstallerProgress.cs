using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Installer;

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
