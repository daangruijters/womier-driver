using System.IO;
using System.Windows.Threading;
using Velopack;
using Velopack.Sources;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Profiles;

namespace WomierDriver.App.Services;

public enum UpdateStatus
{
    /// <summary>Not an installed copy (dotnet run, a plain publish): updates are not handled by the app.</summary>
    Unsupported,
    Idle,
    Checking,
    Downloading,
    UpToDate,
    Failed,

    /// <summary>A newer release is downloaded and is installed by <see cref="UpdateService.RestartToUpdate"/>.</summary>
    Ready,
}

/// <summary>
/// Keeps an installed copy up to date from the GitHub releases (Velopack). Checks a minute after start and then
/// every six hours while <see cref="AppSettings.CheckForUpdates"/> is on, downloads a newer release in the
/// background and installs it only when the user asks for the restart, so a restart never cuts into a write to
/// the keyboard. A downloaded update that is not installed by then is applied on the next start.
/// All members are used on the UI thread.
/// </summary>
public sealed class UpdateService
{
    private const string RepositoryUrl = "https://github.com/daangruijters/womier-driver";

    /// <summary>A folder (or URL) with packed releases instead of GitHub; for testing the update flow.</summary>
    private const string SourceOverrideVariable = "WOMIER_DRIVER_UPDATE_SOURCE";

    private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    private readonly AppSettings _settings;
    private readonly ILogger _log;
    private readonly UpdateManager? _manager;
    private readonly DispatcherTimer _timer;
    private VelopackAsset? _pending;

    public UpdateService(AppSettings settings, ILogger log, Dispatcher dispatcher)
    {
        _settings = settings;
        _log = log;
        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher) { Interval = FirstCheckDelay };
        _timer.Tick += async (_, _) =>
        {
            _timer.Interval = CheckInterval;
            if (_settings.CheckForUpdates)
            {
                await CheckAsync();
            }
        };

        try
        {
            var manager = new UpdateManager(CreateSource());
            if (manager.IsInstalled)
            {
                _manager = manager;
                _pending = manager.UpdatePendingRestart;
                NewVersion = _pending?.Version.ToString();
                Status = _pending is null ? UpdateStatus.Idle : UpdateStatus.Ready;
                _timer.Start();
            }
        }
        catch (Exception ex)
        {
            _log.Warn($"Updates are unavailable: {ex.Message}");
        }
    }

    /// <summary>Raised on the UI thread whenever <see cref="Status"/> changes.</summary>
    public event EventHandler? StatusChanged;

    /// <summary>Raised when the updater waits for the driver to exit; the app shuts down cleanly in response.</summary>
    public event Action? ExitRequested;

    public UpdateStatus Status { get; private set; } = UpdateStatus.Unsupported;

    /// <summary>The version that <see cref="RestartToUpdate"/> installs, or the one being downloaded.</summary>
    public string? NewVersion { get; private set; }

    public string? LastError { get; private set; }

    public bool IsBusy => Status is UpdateStatus.Checking or UpdateStatus.Downloading;

    /// <summary>Looks for a newer release and downloads it. Does nothing while busy or once an update is ready.</summary>
    public async Task CheckAsync()
    {
        if (_manager is null || IsBusy || Status == UpdateStatus.Ready)
        {
            return;
        }

        try
        {
            SetStatus(UpdateStatus.Checking);
            var update = await _manager.CheckForUpdatesAsync();
            if (update is null)
            {
                SetStatus(UpdateStatus.UpToDate);
                return;
            }

            var target = update.TargetFullRelease;
            NewVersion = target.Version.ToString();
            _log.Info($"Downloading update {NewVersion}.");
            SetStatus(UpdateStatus.Downloading);
            await _manager.DownloadUpdatesAsync(update);
            _pending = target;
            _log.Info($"Update {NewVersion} is ready.");
            SetStatus(UpdateStatus.Ready);
        }
        catch (Exception ex)
        {
            _log.Warn($"Update check failed: {ex.Message}");
            LastError = ex.Message;
            SetStatus(UpdateStatus.Failed);
        }
    }

    /// <summary>
    /// Hands the downloaded update to the Velopack updater, which waits for this process to exit, installs it
    /// and starts the new version; then asks the app to shut down.
    /// </summary>
    public void RestartToUpdate()
    {
        if (_manager is null || _pending is null)
        {
            return;
        }

        _log.Info($"Restarting to install update {_pending.Version}.");
        _manager.WaitExitThenApplyUpdates(_pending, silent: true, restart: true);
        ExitRequested?.Invoke();
    }

    private void SetStatus(UpdateStatus status)
    {
        if (status != UpdateStatus.Failed)
        {
            LastError = null;
        }

        Status = status;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private static IUpdateSource CreateSource()
    {
        var overrideSource = Environment.GetEnvironmentVariable(SourceOverrideVariable);
        if (string.IsNullOrWhiteSpace(overrideSource))
        {
            return new GithubSource(RepositoryUrl, accessToken: null, prerelease: false);
        }

        return Directory.Exists(overrideSource)
            ? new SimpleFileSource(new DirectoryInfo(overrideSource))
            : new SimpleWebSource(overrideSource);
    }
}
