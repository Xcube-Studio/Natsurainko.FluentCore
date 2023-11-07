using System;

namespace Nrk.FluentCore.Launch;

public class DefaultLaunchProcess : BaseLaunchProcess
{
    public DefaultLaunchProcess() : base()
    {

    }

    public override void KillProcess()
    {
        throw new NotImplementedException();
    }

    public override void RunLaunch()
    {
        if (!InspectAction())
        {
            State = LaunchState.Faulted;
            return;
        }
    }
}
