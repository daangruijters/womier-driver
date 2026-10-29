using WomierDriver.Core.Hid;

namespace WomierDriver.Core.Automation;

/// <summary>
/// Live travel view. The engine owns the stream so that switching it on and off never interleaves with a
/// profile write: the stream is paused around every write and dropped synchronously when the keyboard goes away.
/// </summary>
public sealed partial class DriverEngine
{
    private volatile bool _liveRequested;
    private ReportTravelSource? _liveSource;

    /// <summary>Raised for every live travel sample while the live view is on. Raised on a background thread.</summary>
    public event EventHandler<LiveTravelSample>? LiveTravel;

    /// <summary>Whether the UI asked for live travel. Survives disconnects; the stream is re-armed on reconnect.</summary>
    public bool IsLiveViewRequested => _liveRequested;

    /// <summary>Current state of the live view; changes are announced through <see cref="StateChanged"/>.</summary>
    public LiveViewState LiveView { get; private set; }

    /// <summary>Turns the live view on or off. Safe to call at any time, connected or not.</summary>
    public async Task SetLiveViewAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        await _deviceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _liveRequested = enabled;
            if (enabled)
            {
                if (_keyboard is null)
                {
                    LiveView = LiveViewState.Waiting;
                }
                else if (_liveSource is null)
                {
                    await StartLiveSourceLockedAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                StopLiveSourceLocked();
                if (_keyboard is not null)
                {
                    try
                    {
                        await _keyboard.SetLiveReportingAsync(false, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (IsDeviceFailure(ex))
                    {
                        HandleDeviceFailure(ex);
                    }
                }
            }
        }
        finally
        {
            _deviceGate.Release();
        }

        RaiseStateChanged();
    }

    /// <summary>Must be called while holding <see cref="_deviceGate"/>; opens the vendor input collection and starts the stream.</summary>
    private async Task StartLiveSourceLockedAsync(CancellationToken cancellationToken)
    {
        var keyboard = _keyboard;
        if (keyboard is null)
        {
            LiveView = LiveViewState.Waiting;
            return;
        }

        var productId = keyboard.Info.IsDongle ? KeyboardLocator.DongleProductId : KeyboardLocator.WiredProductId;
        var inputs = KeyboardLocator.FindVendorInputInterfaces(productId);
        if (inputs.Count == 0)
        {
            _log.Warn("Live view: the keyboard's vendor input collection was not found.");
            LiveView = LiveViewState.Waiting;
            return;
        }

        HidSharpInputReportChannel? channel = null;
        try
        {
            channel = HidSharpInputReportChannel.Open(inputs[0].Device);
            var source = new ReportTravelSource(channel, keyboard.Info.Scale);
            source.Sample += OnLiveSample;
            source.Stopped += OnLiveSourceStopped;
            _liveSource = source;
            source.Start();
            await keyboard.SetLiveReportingAsync(true, cancellationToken).ConfigureAwait(false);
            LiveView = LiveViewState.Streaming;
            _log.Info("Live view on.");
        }
        catch (OperationCanceledException)
        {
            StopLiveSourceLocked();
            channel?.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            StopLiveSourceLocked();
            channel?.Dispose();
            if (IsDeviceFailure(ex))
            {
                HandleDeviceFailure(ex);
            }
            else
            {
                _log.Warn($"Live view could not start: {ex.Message}");
            }

            LiveView = LiveViewState.Waiting;
        }
    }

    /// <summary>Must be called while holding <see cref="_deviceGate"/>. Drops the source without talking to the keyboard.</summary>
    private void StopLiveSourceLocked()
    {
        if (_liveSource is { } source)
        {
            _liveSource = null;
            source.Sample -= OnLiveSample;
            source.Stopped -= OnLiveSourceStopped;
            source.Dispose();
        }

        LiveView = _liveRequested ? LiveViewState.Waiting : LiveViewState.Off;
    }

    private void OnLiveSample(LiveTravelSample sample) => LiveTravel?.Invoke(this, sample);

    private void OnLiveSourceStopped(Exception? exception)
    {
        if (exception is null || _disposed)
        {
            return;
        }

        _log.Warn($"Live travel stream stopped: {exception.GetType().Name}: {exception.Message}. Retrying in 1 s.");
        LiveView = LiveViewState.Waiting;
        RaiseStateChanged();
        _ = Task.Delay(1000, _lifetime.Token).ContinueWith(
            t =>
            {
                if (!t.IsCanceled)
                {
                    _ = RestartLiveSourceAsync();
                }
            },
            TaskScheduler.Default);
    }

    private async Task RestartLiveSourceAsync()
    {
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
            StopLiveSourceLocked();
            if (_liveRequested && _keyboard is not null)
            {
                await StartLiveSourceLockedAsync(_lifetime.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _deviceGate.Release();
        }

        RaiseStateChanged();
    }
}
