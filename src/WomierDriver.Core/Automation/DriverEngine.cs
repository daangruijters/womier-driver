using WomierDriver.Core.Device;
using WomierDriver.Core.Hid;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Profiles;

namespace WomierDriver.Core.Automation;

public sealed record ApplyStatus(DateTimeOffset When, bool Success, string Message, string ProfileName);

public sealed class ProfileAppliedEventArgs(Profile profile, ActivationReason reason, string? trigger, ApplyResult result, bool profileChanged) : EventArgs
{
    public Profile Profile { get; } = profile;

    public ActivationReason Reason { get; } = reason;

    public string? Trigger { get; } = trigger;

    public ApplyResult Result { get; } = result;

    /// <summary>True when a different profile became active (as opposed to re-applying edits of the same profile).</summary>
    public bool ProfileChanged { get; } = profileChanged;
}

/// <summary>
/// The always-on part of the driver: keeps a connection to the keyboard (with hot-plug), watches running
/// processes, decides which profile should be active and writes it to the keyboard when that changes.
/// </summary>
/// <remarks>
/// Thread-safety: the profile library is only mutated through <see cref="MutateLibrary"/>, which takes the
/// engine's state lock; the engine snapshots everything it needs under that same lock before touching the
/// device. Events are raised on thread-pool threads.
/// </remarks>
public sealed class DriverEngine : IAsyncDisposable
{
    private readonly ProfileLibrary _library;
    private readonly AppSettings _settings;
    private readonly ILogger _log;
    private readonly ProcessWatcher _watcher;
    private readonly SemaphoreSlim _deviceGate = new(1, 1);
    private readonly object _stateLock = new();
    private readonly CancellationTokenSource _lifetime = new();

    private IDisposable? _hotplugSubscription;
    private CancellationTokenSource? _hotplugDebounce;
    private Sk75Keyboard? _keyboard;
    private Guid? _manualOverride;
    private Guid? _lastAutoProfileId;
    private string? _lastAutoTrigger;
    private Guid? _appliedProfileId;
    private string? _appliedSignature;
    private int _evaluateScheduled;
    private bool _disposed;

    public DriverEngine(ProfileLibrary library, AppSettings settings, ILogger log)
    {
        _library = library;
        _settings = settings;
        _log = log;
        _watcher = new ProcessWatcher(TimeSpan.FromMilliseconds(Math.Clamp(settings.ProcessPollIntervalMs, 500, 60_000)));
        _watcher.Changed += _ => ScheduleEvaluate("process list changed");
    }

    /// <summary>Raised whenever connection, active profile or apply status changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Raised after every write to the keyboard (successful or not).</summary>
    public event EventHandler<ProfileAppliedEventArgs>? ProfileApplied;

    public ProfileLibrary Library => _library;

    public AppSettings Settings => _settings;

    public KeyboardInfo? Keyboard { get; private set; }

    public bool IsConnected => Keyboard is not null;

    public KeyLimits Limits => _keyboard?.Limits ?? KeyLimits.Default;

    public Profile? ActiveProfile { get; private set; }

    public ActivationReason ActiveReason { get; private set; }

    /// <summary>Process name that activated the current profile, when <see cref="ActiveReason"/> is <see cref="ActivationReason.Game"/>.</summary>
    public string? ActiveTrigger { get; private set; }

    public Guid? ManualOverride
    {
        get
        {
            lock (_stateLock)
            {
                return _manualOverride;
            }
        }
    }

    public ApplyStatus? LastApply { get; private set; }

    public bool IsApplying { get; private set; }

    public IReadOnlySet<string> RunningProcesses => _watcher.Current;

    public async Task StartAsync()
    {
        _hotplugSubscription = KeyboardLocator.Subscribe(OnHidListChanged);
        await RescanAsync().ConfigureAwait(false);
        _watcher.Start();
        ScheduleEvaluate("startup");
    }

    /// <summary>Runs <paramref name="mutate"/> against the library under the engine's lock, then re-evaluates.</summary>
    public void MutateLibrary(Action<ProfileLibrary> mutate, bool reevaluate = true)
    {
        lock (_stateLock)
        {
            mutate(_library);
        }

        if (reevaluate)
        {
            ScheduleEvaluate("profiles changed");
        }
    }

    /// <summary>Runs <paramref name="mutate"/> against the settings under the engine's lock, then re-evaluates.</summary>
    public void MutateSettings(Action<AppSettings> mutate)
    {
        lock (_stateLock)
        {
            mutate(_settings);
        }

        ScheduleEvaluate("settings changed");
    }

    /// <summary>Looks for the keyboard (again). Safe to call at any time.</summary>
    public async Task RescanAsync()
    {
        if (_disposed)
        {
            return;
        }

        var changed = false;
        try
        {
            await _deviceGate.WaitAsync(_lifetime.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            var interfaces = KeyboardLocator.FindConfigurationInterfaces();
            if (_keyboard is not null)
            {
                if (interfaces.Any(i => string.Equals(i.Path, _keyboard.Info.DevicePath, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                _log.Info("Keyboard interface disappeared.");
                DisposeKeyboard();
                changed = true;
            }

            foreach (var iface in interfaces)
            {
                HidSharpFeatureChannel? channel = null;
                try
                {
                    channel = HidSharpFeatureChannel.Open(iface.Device);
                    var keyboard = await Sk75Keyboard.TryConnectAsync(channel, iface.IsDongle, _log, _lifetime.Token).ConfigureAwait(false);
                    if (keyboard is null)
                    {
                        channel.Dispose();
                        continue;
                    }

                    _keyboard = keyboard;
                    Keyboard = keyboard.Info;
                    _appliedProfileId = null;
                    _appliedSignature = null;
                    changed = true;
                    _log.Info($"Connected: {keyboard.Info}");
                    break;
                }
                catch (OperationCanceledException)
                {
                    channel?.Dispose();
                    return;
                }
                catch (Exception ex)
                {
                    _log.Warn($"Could not use {iface.Path}: {ex.Message}");
                    channel?.Dispose();
                }
            }
        }
        finally
        {
            _deviceGate.Release();
        }

        if (changed)
        {
            RaiseStateChanged();
            ScheduleEvaluate("keyboard connection changed");
        }
    }

    /// <summary>Pins a profile regardless of running games, or returns to automatic selection with <c>null</c>.</summary>
    public void SetManualProfile(Guid? profileId)
    {
        lock (_stateLock)
        {
            _manualOverride = profileId;
        }

        ScheduleEvaluate(profileId is null ? "automatic selection requested" : "manual profile selected");
    }

    /// <summary>Call after editing profiles outside <see cref="MutateLibrary"/> so the engine re-evaluates and re-applies.</summary>
    public void NotifyProfilesChanged() => ScheduleEvaluate("profiles changed");

    /// <summary>Forces a write of <paramref name="profile"/> to the keyboard, even if it is already applied.</summary>
    public Task<ApplyResult> ApplyProfileAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        string signature;
        Dictionary<int, KeySettings> keys;
        lock (_stateLock)
        {
            signature = profile.KeySignature();
            keys = new Dictionary<int, KeySettings>(profile.Keys);
        }

        return ApplyInternalAsync(profile, keys, ActivationReason.Manual, signature, force: true, profileChanged: false, cancellationToken);
    }

    /// <summary>Reads the per-key settings currently stored in the keyboard; <c>null</c> when disconnected.</summary>
    public async Task<KeyboardSnapshot?> ReadKeyboardAsync(CancellationToken cancellationToken = default)
    {
        await _deviceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_keyboard is null)
            {
                return null;
            }

            return await _keyboard.ReadKeySettingsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsDeviceFailure(ex))
        {
            HandleDeviceFailure(ex);
            return null;
        }
        finally
        {
            _deviceGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetime.Cancel();
        _hotplugSubscription?.Dispose();
        _hotplugDebounce?.Cancel();
        _watcher.Dispose();
        var acquired = false;
        try
        {
            acquired = await _deviceGate.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        catch
        {
            // Dispose must not hang on a stuck device operation.
        }

        DisposeKeyboard();
        if (acquired)
        {
            _deviceGate.Release();
        }

        _lifetime.Dispose();
    }

    private static bool IsDeviceFailure(Exception ex)
        => ex is IOException or TimeoutException or ObjectDisposedException or InvalidOperationException or UnauthorizedAccessException;

    private void OnHidListChanged()
    {
        var cts = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _hotplugDebounce, cts);
        previous?.Cancel();
        previous?.Dispose();
        _ = Task.Delay(700, cts.Token).ContinueWith(
            t =>
            {
                if (!t.IsCanceled)
                {
                    _ = RescanAsync();
                }
            },
            TaskScheduler.Default);
    }

    private void ScheduleEvaluate(string reason)
    {
        if (_disposed || Interlocked.Exchange(ref _evaluateScheduled, 1) == 1)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(60).ConfigureAwait(false);
                Interlocked.Exchange(ref _evaluateScheduled, 0);
                await EvaluateAsync(reason).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _evaluateScheduled, 0);
                _log.Error($"Evaluation failed ({reason})", ex);
            }
        });
    }

    private async Task EvaluateAsync(string reason)
    {
        ProfileSelection selection;
        string? signature = null;
        Dictionary<int, KeySettings>? keys = null;
        bool profileChanged;
        lock (_stateLock)
        {
            var profiles = _library.Profiles.ToArray();
            var autoSwitch = _settings.AutoSwitchEnabled;
            var running = _watcher.Current;
            var automatic = autoSwitch ? ProfileSelector.SelectAutomatic(profiles, running) : ProfileSelection.Nothing;

            // A manual choice lasts until game activity changes (a listed game starts or stops).
            var autoId = automatic.Profile?.Id;
            if (autoId != _lastAutoProfileId || !string.Equals(automatic.MatchedProcess, _lastAutoTrigger, StringComparison.OrdinalIgnoreCase))
            {
                if (_manualOverride is not null && (_lastAutoProfileId is not null || autoId is not null))
                {
                    _log.Info("Game activity changed; returning to automatic profile selection.");
                    _manualOverride = null;
                }

                _lastAutoProfileId = autoId;
                _lastAutoTrigger = automatic.MatchedProcess;
            }

            selection = ProfileSelector.Select(profiles, running, _manualOverride, autoSwitch);
            profileChanged = ActiveProfile?.Id != selection.Profile?.Id;
            ActiveProfile = selection.Profile;
            ActiveReason = selection.Reason;
            ActiveTrigger = selection.MatchedProcess;
            if (selection.Profile is not null)
            {
                signature = selection.Profile.KeySignature();
                keys = new Dictionary<int, KeySettings>(selection.Profile.Keys);
            }
        }

        RaiseStateChanged();

        if (selection.Profile is null || keys is null || signature is null || _keyboard is null)
        {
            return;
        }

        if (_appliedProfileId == selection.Profile.Id && _appliedSignature == signature)
        {
            return;
        }

        var trigger = selection.MatchedProcess is null ? string.Empty : $", {selection.MatchedProcess}";
        _log.Info($"Applying '{selection.Profile.Name}' ({selection.Reason}{trigger}); trigger: {reason}.");
        await ApplyInternalAsync(selection.Profile, keys, selection.Reason, signature, force: false, profileChanged, _lifetime.Token).ConfigureAwait(false);
    }

    private async Task<ApplyResult> ApplyInternalAsync(
        Profile profile,
        IReadOnlyDictionary<int, KeySettings> keys,
        ActivationReason reason,
        string signature,
        bool force,
        bool profileChanged,
        CancellationToken cancellationToken)
    {
        await _deviceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        ApplyResult result;
        try
        {
            var keyboard = _keyboard;
            if (keyboard is null)
            {
                return ApplyResult.Failed("Keyboard is not connected");
            }

            if (!force && _appliedProfileId == profile.Id && _appliedSignature == signature)
            {
                return new ApplyResult(true, "Already applied", 0, 0, TimeSpan.Zero);
            }

            IsApplying = true;
            RaiseStateChanged();
            try
            {
                result = await keyboard.WriteKeySettingsAsync(keys, cancellationToken).ConfigureAwait(false);
                if (result.Success)
                {
                    _appliedProfileId = profile.Id;
                    _appliedSignature = signature;
                }

                LastApply = new ApplyStatus(DateTimeOffset.Now, result.Success, result.Message, profile.Name);
            }
            catch (Exception ex) when (IsDeviceFailure(ex))
            {
                result = ApplyResult.Failed(ex.Message);
                LastApply = new ApplyStatus(DateTimeOffset.Now, false, ex.Message, profile.Name);
                HandleDeviceFailure(ex);
            }
            finally
            {
                IsApplying = false;
            }
        }
        finally
        {
            _deviceGate.Release();
        }

        RaiseStateChanged();
        ProfileApplied?.Invoke(this, new ProfileAppliedEventArgs(profile, reason, ActiveTrigger, result, profileChanged));
        return result;
    }

    /// <summary>Must be called while holding <see cref="_deviceGate"/>.</summary>
    private void HandleDeviceFailure(Exception ex)
    {
        _log.Warn($"Keyboard I/O failed: {ex.Message}. Reconnecting.");
        DisposeKeyboard();
        _ = Task.Delay(1000).ContinueWith(_ => RescanAsync(), TaskScheduler.Default);
    }

    private void DisposeKeyboard()
    {
        _keyboard?.Dispose();
        _keyboard = null;
        Keyboard = null;
        _appliedProfileId = null;
        _appliedSignature = null;
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
