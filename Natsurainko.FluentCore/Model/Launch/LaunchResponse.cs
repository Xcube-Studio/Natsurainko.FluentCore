using Natsurainko.FluentCore.Event;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Model.Launch;

public class LaunchResponse : IDisposable
{
    public LaunchState State { get; private set; }

    public IEnumerable<string> Arguemnts { get; private set; }

    public List<GameProcessOutput> ProcessOutputs { get; private set; } = new();

    public Process Process { get; private set; }

    public Stopwatch RunTime { get; private set; }

    public Exception Exception { get; private set; }

    public event EventHandler<GameExitedArgs> GameExited;

    public event EventHandler<GameProcessOutputArgs> GameProcessOutput;

    public LaunchResponse(Process process, LaunchState state, IEnumerable<string> args, Stopwatch runTime)
    {
        Process = process;
        State = state;
        Arguemnts = args;
        RunTime = runTime;

        if (state == LaunchState.Succeess)
        {
            Process.OutputDataReceived += (_, e) => OnOutputDataReceived(e);
            Process.ErrorDataReceived += (_, e) => OnOutputDataReceived(e, true);

            Process.Exited += (_, _) =>
            {
                RunTime.Stop();

                GameExited?.Invoke(this, new GameExitedArgs
                {
                    Crashed = Process.ExitCode != 0,
                    ExitCode = Process.ExitCode,
                    RunTime = RunTime,
                    Outputs = ProcessOutputs
                });
            };

            Process.Start();

            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();
        }
    }

    public LaunchResponse(Process process, LaunchState state, IEnumerable<string> args, Exception exception)
    {
        Process = process;
        State = state;
        Arguemnts = args;
        Exception = exception;
    }

    private void OnOutputDataReceived(DataReceivedEventArgs e, bool isErrorDataReceived = false)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;

        var processOutput = Launch.GameProcessOutput.Parse(e.Data, isErrorDataReceived);

        GameProcessOutput?.Invoke(this, new GameProcessOutputArgs(processOutput, isErrorDataReceived));
        ProcessOutputs.Add(processOutput);
    }

    public void WaitForExit() => Process?.WaitForExit();

    public async Task WaitForExitAsync() => await Task.Run(() => Process?.WaitForExit());

    public void Stop() => Process?.Kill();

    #region Dispose
    private bool disposedValue;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {

            }

            Process?.Dispose();
            Arguemnts = null;
            ProcessOutputs = null;
            Exception = null;

            GameExited?.GetInvocationList().ToList().ForEach(x => GameExited -= (EventHandler<GameExitedArgs>)x);
            GameProcessOutput?.GetInvocationList().ToList().ForEach(x => GameProcessOutput -= (EventHandler<GameProcessOutputArgs>)x);

            disposedValue = true;
        }
    }
    #endregion
}

public enum LaunchState
{
    Succeess = 0,
    Failed = 1,
    Cancelled = 2
}