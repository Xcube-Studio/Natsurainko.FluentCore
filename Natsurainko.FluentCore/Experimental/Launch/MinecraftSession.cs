using Nrk.FluentCore.Authentication;
using Nrk.FluentCore.Experimental.GameManagement.Dependencies;
using Nrk.FluentCore.Experimental.GameManagement.Instances;
using Nrk.FluentCore.Experimental.Launch;
using Nrk.FluentCore.Launch;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nrk.FluentCore.Experimental.GameManagement.Launch;

/// <summary>
/// Encapsulates a launch session, holds a McProcess instance
/// </summary>
public class MinecraftSession
{
    #region Downloader Events

    /// <summary>
    /// Raised when single file downloaded completely
    /// </summary>
    public event EventHandler? SingleFileDownloaded;

    /// <summary>
    /// Raised when all download task posted into downloader
    /// </summary>
    public event EventHandler<int>? DownloadElementsPosted;

    #endregion

    #region Process Events

    /// <summary>
    /// Raised when <see cref="Process"/> started
    /// </summary>
    public event EventHandler? ProcessStarted;

    /// <summary>
    /// Raised when <see cref="Process"/> exited
    /// </summary>
    public event EventHandler<MinecraftProcessExitedEventArgs>? ProcessExited;

    /// <summary>
    /// Raised when <see cref="Process.OutputDataReceived"/>
    /// </summary>
    public event DataReceivedEventHandler? OutputDataReceived;

    /// <summary>
    /// Raised when <see cref="Process.ErrorDataReceived"/>
    /// </summary>
    public event DataReceivedEventHandler? ErrorDataReceived;

    #endregion

    /// <summary>
    /// Raised when <see cref="State"/> changes
    /// </summary>
    public event EventHandler<MinecraftSessionStateChagnedEventArgs>? StateChanged;

    #region Properties

    public IEnumerable<string>? ExtraVmParameters { get; set; }
    public IEnumerable<string>? ExtraGameParameters { get; set; }
    public bool UseDemoUser { get; set; }

    public required string JavaPath { get; set; }
    public required string GameDirectory { get; set; }
    public required int MaxMemory { get; set; }
    public int MinMemory { get; set; }
    public required MinecraftInstance MinecraftInstance { get; set; }
    public required Account Account { get; set; }

    private IEnumerable<MinecraftLibrary>? _enabledLibraries;
    private IEnumerable<MinecraftLibrary>? _enabledNativesLibraries;

    #endregion

    private MinecraftProcess? _mcProcess; // TODO: Create on init, so it can be non-nullable, update argument list when needed before the process is started
    // QUESTION: Should this be public? MinecraftSession is designed to hide the underlying process, maybe should forward events instead?

    // Sets to true when a Kill() is called, used for the Exited event handler to deterine state
    private bool _killRequested = false;

    private MinecraftSessionState _state = MinecraftSessionState.Created;
    public MinecraftSessionState State
    {
        get { return _state; }
        private set
        {
            var old = _state;
            _state = value;
            StateChanged?.Invoke(this, new MinecraftSessionStateChagnedEventArgs(old, value));
        }
    }

    /// <summary>
    /// Start the Minecraft game (guarantees <see cref="_mcProcess"/> is not null when finished)
    /// </summary>
    public async Task StartAsync()
    {
        if (State != MinecraftSessionState.Created)
            throw new InvalidOperationException("this session has been launched, please create new one");

        try
        {
            State = MinecraftSessionState.Inspecting;
            CheckEnvironment();

            if (RefreshAccountTask != null)
            {
                State = MinecraftSessionState.Authenticating;

                RefreshAccountTask.Start();
                Account = await RefreshAccountTask;
            }

            var (enabledLibraries, enabledNativesLibraries) = MinecraftInstance.GetRequiredLibraries();

            _enabledLibraries = enabledLibraries;
            _enabledNativesLibraries = enabledNativesLibraries;

            State = MinecraftSessionState.CompletingResources;

            CheckAndCompleteDependencies();

            State = MinecraftSessionState.BuildingArguments;
            _mcProcess = CreateMinecraftProcess();

            // Forward events from _mcProcess
            _mcProcess.OutputDataReceived += OutputDataReceived;
            _mcProcess.ErrorDataReceived += ErrorDataReceived;
            _mcProcess.Started += ProcessStarted;
            _mcProcess.Exited += ProcessExited;

            // Updates session state when the game process exits
            _mcProcess.Exited += (_, e) =>
            {
                State = e.ExitCode == 0
                    ? MinecraftSessionState.GameExited
                    : _killRequested
                        ? MinecraftSessionState.Killed
                        : MinecraftSessionState.GameCrashed;

                _mcProcess.Dispose(); // Release resources used by the Minecraft process when it exits. Exit code is reflected by the MinecraftSessionState.
            };

            State = MinecraftSessionState.LaunchingProcess;
            _mcProcess.Start();
            State = MinecraftSessionState.GameRunning;
        }
        catch (Exception)
        {
            State = MinecraftSessionState.Faulted;
            // QUESTION: Invoke an event here? Maybe just use a general state changed event?
            throw; // rethrow for caller to handle
        }
    }

    /// <summary>
    /// Kills a running Minecraft game. The operation is invalid if the game is not running.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Kill()
    {
        // TODO: Cancel the launch sequence when an async version is implemented
        if (_mcProcess is null)
            throw new InvalidOperationException("Process has not been created");

        _killRequested = true;
        _mcProcess.Kill();
        // State change handeled by _mcProcess.Exited event
    }

    public nint GetProcessMainWindowHandle()
    {
        if (_mcProcess is null)
            throw new InvalidOperationException();

        _mcProcess._process.Refresh();
        return _mcProcess._process.MainWindowHandle;
    }

    #region Step Methods

    private void CheckEnvironment()
    {
        if (!File.Exists(JavaPath)) throw new FileNotFoundException(JavaPath);

        // TODO: check memory?
    }

    public Task<Account>? RefreshAccountTask { get; set; }

    public Func<IEnumerable<MinecraftLibrary>, DependencyResolver>? CreateDependencyResolver { get; set; }

    void CheckAndCompleteDependencies()
    {
        if (_enabledNativesLibraries == null)
            throw new ArgumentNullException(nameof(_enabledNativesLibraries));

        if (_enabledLibraries == null)
            throw new ArgumentNullException(nameof(_enabledLibraries));

        if (CreateDependencyResolver != null)
        {
            var resourcesDownloader = CreateDependencyResolver(_enabledLibraries.Union(_enabledNativesLibraries));
            resourcesDownloader.DependencyDownloaded += (_, e) =>
                SingleFileDownloaded?.Invoke(resourcesDownloader, new EventArgs());
            resourcesDownloader.InvalidDependenciesDetermined += (_, deps) =>
                DownloadElementsPosted?.Invoke(resourcesDownloader, deps.Count());

            var downloadResult = resourcesDownloader.VerifyAndDownloadDependenciesAsync().GetAwaiter().GetResult();
            if (downloadResult.Failed.Count > 0)
                throw new Exception("ResourcesDownloader.ErrorDownload.Count > 0");
        }

        UnzipUtils.BatchUnzip(
            Path.Combine(MinecraftInstance.MinecraftFolderPath, "versions", MinecraftInstance.InstanceId, "natives"),
            _enabledNativesLibraries.Select(x => x.FullPath));
    }

    private MinecraftProcess CreateMinecraftProcess()
    {
        if (_enabledLibraries == null)
            throw new ArgumentNullException(nameof(_enabledLibraries));

        var builder = new MinecraftProcessBuilder(MinecraftInstance)
            .SetLibraries(_enabledLibraries)
            .SetAccountSettings(Account, UseDemoUser)
            .SetJavaSettings(JavaPath, MaxMemory, MinMemory)
            .SetGameDirectory(GameDirectory);

        if (ExtraVmParameters != null)
            builder.AddVmArguments(ExtraVmParameters);

        if (ExtraGameParameters != null)
            builder.AddGameArguments(ExtraGameParameters);

        return builder.Build();
    }

    #endregion
}

public enum MinecraftSessionState
{
    // Launch sequence
    Created = 0,
    Inspecting = 1,
    Authenticating = 2,
    CompletingResources = 3,
    BuildingArguments = 4,
    LaunchingProcess = 5,
    GameRunning = 6,

    GameExited = 7, // Game exited normally (exit code == 0)
    Faulted = 8, // Failure before game started
    Killed = 9, // Game killed by user
    GameCrashed = 10 // Game crashed (exit code != 0)
}

public class MinecraftSessionStateChagnedEventArgs(MinecraftSessionState oldState, MinecraftSessionState newState) : EventArgs
{
    public MinecraftSessionState OldState { get; } = oldState;

    public MinecraftSessionState NewState { get; } = newState;
}