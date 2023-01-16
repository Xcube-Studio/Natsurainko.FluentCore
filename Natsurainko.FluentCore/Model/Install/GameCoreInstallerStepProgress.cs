using System;

namespace Natsurainko.FluentCore.Model.Install;

public class GameCoreInstallerStepProgress
{
    public event EventHandler ProgressChanged;

    public string StepName { get; set; }

    public double Progress { get; set; } = -1;

    public bool IsIndeterminate { get; set; } = true;

    public int TotleTask { get; set; } = 1;

    public int CompletedTask { get; set; } = 0;

    internal void Report() => ProgressChanged?.Invoke(this, null);
}
