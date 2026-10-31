using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using WomierDriver.App.Services;
using WomierDriver.Core.Automation;

namespace WomierDriver.App.Tray;

/// <summary>
/// The system-tray presence of the driver: icon, tooltip with the active profile, a right-click menu to switch
/// profiles or open the window, and a notification when a game switches the profile.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private static readonly Uri OnlineIcon = new("pack://application:,,,/Assets/app.ico");
    private static readonly Uri OfflineIcon = new("pack://application:,,,/Assets/app-offline.ico");

    private readonly AppServices _services;
    private readonly Action _showWindow;
    private readonly Action _exit;
    private readonly TaskbarIcon _icon;
    private bool _wasConnected = true;
    private bool _trayUpdateFailed;
    private readonly BatteryAlerts _batteryAlerts = new();

    public TrayIconController(AppServices services, Action showWindow, Action exit)
    {
        _services = services;
        _showWindow = showWindow;
        _exit = exit;

        _icon = new TaskbarIcon
        {
            ToolTipText = "Womier SK75 TMR",
            NoLeftClickDelay = true,
            MenuActivation = PopupActivationMode.RightClick,
            IconSource = new BitmapImage(OfflineIcon),
        };
        _icon.TrayLeftMouseUp += (_, _) => _showWindow();
        _icon.ContextMenu = new ContextMenu();
        _icon.ContextMenu.Opened += (_, _) => RebuildMenu();
        _icon.ForceCreate(enablesEfficiencyMode: false);

        _services.Engine.StateChanged += (_, _) => _services.Dispatcher.BeginInvoke(Refresh);
        _services.Engine.ProfileApplied += (_, e) => _services.Dispatcher.BeginInvoke(() => OnProfileApplied(e));
        Refresh();
    }

    public void Dispose() => _icon.Dispose();

    /// <summary>Shows a balloon / toast notification from the tray icon.</summary>
    public void Notify(string title, string message)
    {
        try
        {
            _icon.ShowNotification(title, message, NotificationIcon.None, sound: false);
        }
        catch
        {
            // Notifications are best effort (they can be disabled system-wide).
        }
    }

    private void Refresh()
    {
        var engine = _services.Engine;
        var connected = engine.IsConnected;
        var line2 = !connected || engine.IsKeyboardAsleep
            ? ConnectionDescription.Describe(engine)
            : engine.ActiveProfile is null
                ? "No profile"
                : engine.ActiveReason switch
                {
                    ActivationReason.Game => $"{engine.ActiveProfile.Name} · {engine.ActiveTrigger}",
                    ActivationReason.Manual => $"{engine.ActiveProfile.Name} · manual",
                    _ => engine.ActiveProfile.Name,
                };
        try
        {
            if (!_icon.IsCreated)
            {
                _icon.ForceCreate(enablesEfficiencyMode: false);
            }

            if (connected != _wasConnected)
            {
                _icon.IconSource = new BitmapImage(connected ? OnlineIcon : OfflineIcon);
                _wasConnected = connected;
            }

            _icon.ToolTipText = ConnectionDescription.Battery(engine) is { } battery
                ? $"Womier SK75 TMR\n{line2}\n{battery}"
                : $"Womier SK75 TMR\n{line2}";
            _trayUpdateFailed = false;
        }
        catch (InvalidOperationException ex)
        {
            // The shell refuses tray updates while its notification area is unavailable (seen around a locked
            // session). That must never become an error dialog; the next state change simply tries again.
            if (!_trayUpdateFailed)
            {
                _services.Log.Warn($"Tray icon update failed: {ex.Message}");
                _trayUpdateFailed = true;
            }
        }

        if (_icon.ContextMenu is { IsOpen: false })
        {
            RebuildMenu();
        }

        CheckBattery();
    }

    /// <summary>Warns once at 20 % and once more at 10 % (see <see cref="BatteryAlerts"/>).</summary>
    private void CheckBattery()
    {
        var percent = ConnectionDescription.BatteryPercent(_services.Engine);
        var alert = _batteryAlerts.Update(percent);
        if (alert == BatteryAlert.None || !_services.Engine.Settings.NotifyOnLowBattery)
        {
            return;
        }

        _services.Log.Info($"Keyboard battery at {percent} %.");
        if (alert == BatteryAlert.Critical)
        {
            Notify("Keyboard battery almost empty", $"{percent} % left. Connect the cable to charge it; it keeps working while charging.");
        }
        else
        {
            Notify("Keyboard battery low", $"{percent} % left. Connect the cable to charge it soon.");
        }
    }

    private void RebuildMenu()
    {
        var menu = _icon.ContextMenu!;
        menu.Items.Clear();
        var engine = _services.Engine;

        menu.Items.Add(new MenuItem
        {
            Header = ConnectionDescription.Battery(engine) is { } battery
                ? $"{ConnectionDescription.Describe(engine)} · {battery}"
                : ConnectionDescription.Describe(engine),
            IsEnabled = false,
        });
        menu.Items.Add(new Separator());

        var automatic = new MenuItem
        {
            Header = "Automatic (follow games)",
            IsCheckable = true,
            IsChecked = engine.ManualOverride is null,
            IsEnabled = engine.Settings.AutoSwitchEnabled,
        };
        automatic.Click += (_, _) => engine.SetManualProfile(null);
        menu.Items.Add(automatic);
        menu.Items.Add(new Separator());

        foreach (var profile in engine.Library.Profiles.ToList())
        {
            var id = profile.Id;
            var item = new MenuItem
            {
                Header = profile.IsDefault ? $"{profile.Name}  (default)" : profile.Name,
                IsCheckable = true,
                IsChecked = engine.ActiveProfile?.Id == id,
            };
            item.Click += (_, _) => engine.SetManualProfile(id);
            menu.Items.Add(item);
        }

        if (engine.Library.Profiles.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = "No profiles yet", IsEnabled = false });
        }

        menu.Items.Add(new Separator());
        var open = new MenuItem { Header = "Open Womier SK75 TMR…" };
        open.Click += (_, _) => _showWindow();
        menu.Items.Add(open);
        var exit = new MenuItem { Header = "Exit" };
        exit.Click += (_, _) => _exit();
        menu.Items.Add(exit);
    }

    private void OnProfileApplied(ProfileAppliedEventArgs e)
    {
        if (!e.ProfileChanged || !_services.Engine.Settings.NotifyOnProfileSwitch)
        {
            return;
        }

        if (!e.Result.Success && _services.Engine.IsKeyboardAsleep)
        {
            Notify("Profile switch pending", $"{e.Profile.Name} is written to the keyboard as soon as it wakes up.");
            return;
        }

        if (!e.Result.Success)
        {
            Notify("Profile not applied", $"{e.Profile.Name}: {e.Result.Message}");
            return;
        }

        var why = e.Reason switch
        {
            ActivationReason.Game when e.Trigger is not null => $"{e.Trigger} is running",
            ActivationReason.Default => "no game running",
            ActivationReason.Manual => "selected manually",
            _ => string.Empty,
        };
        Notify($"Profile: {e.Profile.Name}", why.Length == 0 ? "Applied to the keyboard" : $"Applied to the keyboard ({why})");
    }
}
