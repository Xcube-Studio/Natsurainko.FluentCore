using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Nrk.FluentCore.Launch;

/// <summary>
/// State of a Minecraft process
/// </summary>
/// 
/// State transitions:
/// Object initialized -> Created
/// Created -> Running: Start() called
/// Running -> Exited: Minecraft exited (normally, killed, or other error crashes the game)
public enum MinecraftProcessState
{
    /// <summary>
    /// Process is created but not started
    /// </summary>
    Created,
    /// <summary>
    /// Process is running
    /// </summary>
    Running,
    /// <summary>
    /// Process has exited
    /// </summary>
    Exited
}

public class MinecraftProcessExitedEventArgs : EventArgs
{
    public int ExitCode { get; }

    public MinecraftProcessExitedEventArgs(int exitCode)
    {
        ExitCode = exitCode;
    }
}

public class MinecraftProcess : IDisposable
{
    /// <summary>
    /// Java path to use for running Minecraft
    /// </summary>
    public string JavaPath { get; }

    /// <summary>
    /// State of this launch session
    /// </summary>
    public MinecraftProcessState State { get; private set; }

    /// <summary>
    /// Arguments passed when the Minecraft process is started. 
    /// Can be updated before calling <see cref="Start"/>
    /// </summary>
    public IEnumerable<string> ArgumentList { get; init; }

    internal Process _process;

    #region Events for _process

    /// <summary>
    /// Raised when Minecraft exits normally, crashes, or is killed
    /// </summary>
    public event EventHandler<MinecraftProcessExitedEventArgs>? Exited;

    /// <summary>
    /// Raised when Minecraft is started
    /// </summary>
    public event EventHandler? Started;

    // Forwarded from _process
    public event DataReceivedEventHandler? OutputDataReceived
    {
        add => _process.OutputDataReceived += value;
        remove => _process.OutputDataReceived -= value;
    }

    // Forwarded from _process
    public event DataReceivedEventHandler? ErrorDataReceived
    {
        add => _process.ErrorDataReceived += value;
        remove => _process.ErrorDataReceived -= value;
    }

    #endregion

    /// <summary>
    /// Create a new Minecraft process
    /// </summary>
    /// <param name="javaPath">Java path to use for running Minecraft</param>
    /// <param name="workingDir"></param>
    /// <param name="launchArgs">Launch arguments to pass to the Minecraft process</param>
    public MinecraftProcess(string javaPath, string workingDir, IEnumerable<string> launchArgs)
    {
        JavaPath = javaPath;
        ArgumentList = launchArgs;

        _process = new Process
        {
            StartInfo = new ProcessStartInfo(javaPath)
            {
                WorkingDirectory = workingDir,
                Arguments = string.Join(' ', launchArgs),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
            EnableRaisingEvents = true,
        };
        State = MinecraftProcessState.Created;

        _process.Exited += MCProcess_Exited;
    }

    private void MCProcess_Exited(object? sender, EventArgs e)
    {
        State = MinecraftProcessState.Exited;
        Exited?.Invoke(this, new MinecraftProcessExitedEventArgs(_process.ExitCode));
    }

    public void Start()
    {
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        State = MinecraftProcessState.Running;
        Started?.Invoke(this, EventArgs.Empty);
    }

    public void Kill()
    {
        _process.Kill(); // Will raise Exited event in the handler MCProcess_Exited
        State = MinecraftProcessState.Exited;
    }

    public void Dispose() => _process.Dispose();
}
