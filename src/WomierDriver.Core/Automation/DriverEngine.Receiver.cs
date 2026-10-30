using WomierDriver.Core.Hid;
using WomierDriver.Core.Protocol;

namespace WomierDriver.Core.Automation;

/// <summary>
/// 2.4 GHz receiver support. A wireless keyboard sleeps after a while without key presses, and the receiver stays
/// plugged in, so no hot-plug event announces that it is back. While a receiver is in use the engine therefore asks it
/// every few seconds (a receiver-local query that never reaches the keyboard) whether the keyboard is connected:
/// a sleeping keyboard is kept as the current keyboard, writes wait until it wakes, and a keyboard that was asleep when
/// the receiver was first seen is identified as soon as it wakes.
/// </summary>
public sealed partial class DriverEngine
{
    private static readonly TimeSpan ReceiverPollInterval = TimeSpan.FromSeconds(2);

    private ReceiverRelayChannel? _receiver;
    private HidInterfaceInfo? _sleepingReceiver;
    private ReceiverRelayChannel? _receiverProbe;
    private volatile bool _keyboardAsleep;
    private Task? _receiverWatch;

    /// <summary>
    /// True while the keyboard is only reachable through its 2.4 GHz receiver and sleeps (or is out of range). Writes are
    /// postponed until it wakes up; a key press wakes it.
    /// </summary>
    public bool IsKeyboardAsleep => _keyboardAsleep;

    /// <summary>Battery level the 2.4 GHz receiver reports for the keyboard; <c>null</c> on the cable or when unknown.</summary>
    public int? BatteryPercent { get; private set; }

    /// <summary>A 2.4 GHz receiver is plugged in and in use (connected, or waiting for its keyboard to wake up).</summary>
    public bool UsesReceiver => _receiver is not null || _sleepingReceiver is not null;

    private void StartReceiverWatch() => _receiverWatch = Task.Run(WatchReceiverAsync);

    private async Task WatchReceiverAsync()
    {
        using var timer = new PeriodicTimer(ReceiverPollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(_lifetime.Token).ConfigureAwait(false))
            {
                try
                {
                    await PollReceiverAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _log.Warn($"Receiver poll failed: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task PollReceiverAsync()
    {
        if (_disposed || (_receiver is null && _sleepingReceiver is null))
        {
            return;
        }

        // Never wait behind a profile write: skip this round instead.
        if (!await _deviceGate.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        var changed = false;
        var woke = false;
        var identify = false;
        try
        {
            if (_keyboard is not null && _receiver is not null)
            {
                (changed, woke) = UpdateReceiverState(await _receiver.GetStatusAsync(_lifetime.Token).ConfigureAwait(false));
            }
            else if (_keyboard is null && _sleepingReceiver is { } receiver)
            {
                _receiverProbe ??= new ReceiverRelayChannel(HidSharpFeatureChannel.Open(receiver.Device, ReceiverRelayChannel.ReceiverReadDelayMs), _log);
                var status = await _receiverProbe.GetStatusAsync(_lifetime.Token).ConfigureAwait(false);
                (changed, _) = UpdateReceiverState(status);
                if (status.KeyboardConnected)
                {
                    DisposeReceiverProbe();
                    identify = true;
                }
            }
        }
        catch (Exception ex) when (IsDeviceFailure(ex))
        {
            if (_keyboard is not null)
            {
                HandleDeviceFailure(ex);
            }
            else
            {
                // The receiver went away; the hot-plug rescan takes it from here.
                DisposeReceiverProbe();
                _sleepingReceiver = null;
                _keyboardAsleep = false;
                BatteryPercent = null;
            }

            changed = true;
        }
        finally
        {
            _deviceGate.Release();
        }

        if (changed)
        {
            RaiseStateChanged();
        }

        if (identify)
        {
            _log.Info("The keyboard behind the receiver woke up; identifying it.");
            await RescanAsync().ConfigureAwait(false);
        }
        else if (woke)
        {
            ScheduleEvaluate("keyboard woke up");
            if (_liveRequested)
            {
                // The keyboard may have dropped the travel-report mode while it slept: arm it again.
                await RestartLiveSourceAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>Records sleep state and battery from a receiver status. Returns whether anything visible changed and whether the keyboard just woke up.</summary>
    private (bool Changed, bool Woke) UpdateReceiverState(ReceiverStatus status)
    {
        var asleep = !status.KeyboardConnected;
        int? battery = status.BatteryPercent is > 0 and <= 100 ? status.BatteryPercent : BatteryPercent;
        var woke = _keyboardAsleep && !asleep;
        var changed = asleep != _keyboardAsleep || battery != BatteryPercent;
        if (asleep != _keyboardAsleep)
        {
            _log.Info(asleep ? "The keyboard went to sleep (2.4 GHz)." : "The keyboard is awake again (2.4 GHz).");
        }

        _keyboardAsleep = asleep;
        BatteryPercent = battery;
        return (changed, woke);
    }

    /// <summary>Called by a device operation that found the keyboard asleep; the receiver poll notices when it wakes.</summary>
    private void MarkKeyboardAsleep()
    {
        if (!_keyboardAsleep)
        {
            _keyboardAsleep = true;
            _log.Info("The keyboard is asleep (2.4 GHz); waiting for it to wake up.");
            RaiseStateChanged();
        }
    }

    private void DisposeReceiverProbe()
    {
        _receiverProbe?.Dispose();
        _receiverProbe = null;
    }
}
