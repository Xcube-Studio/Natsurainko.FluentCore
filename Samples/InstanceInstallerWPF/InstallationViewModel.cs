using CommunityToolkit.Mvvm.ComponentModel;
using Nrk.FluentCore.Experimental.GameManagement.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstanceInstallerWPF;

// Used in Launcher
class InstallationViewModel<TStage> : IProgress<InstallerProgress<TStage>>
    where TStage : notnull
{
    public Dictionary<TStage, InstallationStageViewModel> Stages { get; } = new();

    public InstallationViewModel(IReadOnlyDictionary<TStage, string> stageNames)
    {
        // Init stage view models
        foreach (var (stage, name) in stageNames)
        {
            Stages.Add(stage, new InstallationStageViewModel { TaskName = name });
        }
    }

    public void Report(InstallerProgress<TStage> value)
    {
        var vm = Stages[value.Stage];
        vm.UpdateProgress(value.StageProgress);
    }
}

public enum State
{
    Prepared,
    Running,
    Finished,
    Failed
}

partial class InstallationStageViewModel : ObservableObject
{
    [ObservableProperty]
    private string taskName = "";

    [ObservableProperty]
    private State taskState = State.Prepared;

    [ObservableProperty]
    private int totalTasks = 1;

    public int FinishedTasks
    {
        get => _finishedTasks;
        set
        {
            _finishedTasks = value;
            OnPropertyChanged(nameof(FinishedTasks));
        }
    }

    private int _finishedTasks = 0;

    public void UpdateProgress(InstallerStageProgress payload)
    {
        switch (payload.Type)
        {
            case InstallerStageProgressType.Starting:
                TaskState = State.Running;
                break;

            case InstallerStageProgressType.UpdateTotalTasks:
                TotalTasks = (int)payload.TotalTasks!;
                break;
            case InstallerStageProgressType.UpdateFinishedTasks:
                FinishedTasks = (int)payload.FinishedTasks!;
                break;
            case InstallerStageProgressType.IncrementFinishedTasks:
                Interlocked.Increment(ref _finishedTasks);
                OnPropertyChanged(nameof(FinishedTasks));
                break;

            case InstallerStageProgressType.Finished:
                TaskState = State.Finished;
                FinishedTasks = TotalTasks;
                break;
            case InstallerStageProgressType.Failed:
                TaskState = State.Failed;
                break;

            default:
                break;
        }
    }
}