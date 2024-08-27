using Nrk.FluentCore.Experimental.GameManagement.Dependencies;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;

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
    /// ArgumentsJsonObject passed when the Minecraft process is started. 
    /// Can be updated before calling <see cref="Start"/>
    /// </summary>
    public IEnumerable<string> ArgumentList { get; init; }

    public Process Process { get; private set; }

    public IReadOnlyList<MinecraftLibrary> Natives { get; private set; }

    #region Events for Process

    /// <summary>
    /// Raised when Minecraft exits normally, crashes, or is killed
    /// </summary>
    public event EventHandler<MinecraftProcessExitedEventArgs>? Exited;

    /// <summary>
    /// Raised when Minecraft is started
    /// </summary>
    public event EventHandler? Started;

    // Forwarded from Process
    public event DataReceivedEventHandler? OutputDataReceived
    {
        add => Process.OutputDataReceived += value;
        remove => Process.OutputDataReceived -= value;
    }

    // Forwarded from Process
    public event DataReceivedEventHandler? ErrorDataReceived
    {
        add => Process.ErrorDataReceived += value;
        remove => Process.ErrorDataReceived -= value;
    }

    #endregion

    /// <summary>
    /// Create a new Minecraft process
    /// </summary>
    /// <param name="javaPath">Java path to use for running Minecraft</param>
    /// <param name="workingDir"></param>
    /// <param name="launchArgs">Launch arguments to pass to the Minecraft process</param>
    public MinecraftProcess(string javaPath, string workingDir, IEnumerable<string> launchArgs, IReadOnlyList<MinecraftLibrary> natives)
    {
        Natives = natives;
        JavaPath = javaPath;
        ArgumentList = launchArgs;

        Process = new Process
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

        Process.Exited += MCProcess_Exited;
    }

    /// <summary>
    /// Create a new Minecraft process
    /// </summary>
    /// <param name="javaPath">Java path to use for running Minecraft</param>
    /// <param name="workingDir"></param>
    /// <param name="launchArgs">Launch arguments to pass to the Minecraft process</param>
    [SupportedOSPlatform("windows")]
    public MinecraftProcess(string javaPath, string workingDir, IEnumerable<string> launchArgs, IReadOnlyList<MinecraftLibrary> natives, bool isCmdMode)
    {
        Natives = natives;
        JavaPath = javaPath;
        ArgumentList = launchArgs;

        Process = new Process
        {
            StartInfo = new ProcessStartInfo(isCmdMode ? "cmd.exe" : javaPath)
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

        Process.Exited += MCProcess_Exited;
    }

    private void MCProcess_Exited(object? sender, EventArgs e)
    {
        State = MinecraftProcessState.Exited;
        Exited?.Invoke(this, new MinecraftProcessExitedEventArgs(Process.ExitCode));
    }

    public void Start()
    {
        Process.Start();
        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();
        State = MinecraftProcessState.Running;
        Started?.Invoke(this, EventArgs.Empty);
    }

    public void Kill()
    {
        Process.Kill(); // Will raise Exited event in the handler MCProcess_Exited
        State = MinecraftProcessState.Exited;
    }

    public void Dispose() => Process.Dispose();
}
