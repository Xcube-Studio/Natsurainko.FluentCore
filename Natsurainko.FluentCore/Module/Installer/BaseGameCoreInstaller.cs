using Natsurainko.FluentCore.Event;
using Natsurainko.FluentCore.Interface;
using Natsurainko.FluentCore.Model.Install;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Module.Installer;

public abstract class BaseGameCoreInstaller : IGameCoreInstaller
{
    public event EventHandler<GameCoreInstallerProgressChangedEventArgs> ProgressChanged;

    public string CustomId { get; private set; }

    public string McVersion { get; private set; }

    protected virtual Dictionary<string, GameCoreInstallerStepProgress> StepsProgress { get; set; }

    public IGameCoreLocator<IGameCore> GameCoreLocator { get; private set; }

    public BaseGameCoreInstaller(
        IGameCoreLocator<IGameCore> coreLocator,
        string mcVersion,
        string customId = default)
    {
        GameCoreLocator = coreLocator;
        McVersion = mcVersion;
        CustomId = customId;
    }

    public GameCoreInstallerResponse Install() => InstallAsync().GetAwaiter().GetResult();

    public virtual Task<GameCoreInstallerResponse> InstallAsync() => throw new NotImplementedException();

    protected virtual async Task CheckInheritedCore()
    {
        OnProgressChanged("Check Inherited Core", 0);

        if (GameCoreLocator.GetGameCore(McVersion) == null)
        {
            var installer = new MinecraftVanlliaInstaller(GameCoreLocator, McVersion);
            installer.ProgressChanged += (sender, e) 
                => OnProgressChanged(
                    $"Check Inherited Core", 
                    e.TotleProgress, 
                    e.StepsProgress.Values.Sum(x => x.TotleTask), 
                    e.StepsProgress.Values.Sum(x => x.CompletedTask));


            var installerResponse = await installer.InstallAsync();
        }

        OnProgressChanged("Check Inherited Core", 1);
    }

    protected virtual void OnProgressChanged(string stepName, double progress, int totol = default, int completed = default) 
    {
        int sumProcesses = 0;
        int completedProcesses = 0;

        StepsProgress[stepName].Progress = progress;

        if (!totol.Equals(default))
            StepsProgress[stepName].TotleTask = totol;

        if (!completed.Equals(default))
            StepsProgress[stepName].CompletedTask = completed;

        if (progress.Equals(1))
        {
            StepsProgress[stepName].CompletedTask = StepsProgress[stepName].TotleTask;
            StepsProgress[stepName].IsIndeterminate = false;
        }

        foreach (var kvp in StepsProgress)
        {
            sumProcesses += kvp.Value.TotleTask;
            completedProcesses += kvp.Value.CompletedTask;
        }

        StepsProgress[stepName].Report();

        ProgressChanged?.Invoke(this, new()
        {
            StepsProgress = StepsProgress,
            TotleProgress = (double)completedProcesses / sumProcesses
        });
    }
}
