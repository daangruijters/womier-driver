using System.Reflection;
using System.Windows.Threading;
using Microsoft.Win32;
using WomierDriver.Core.Automation;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Profiles;

namespace WomierDriver.App.Services;

/// <summary>Composition root shared by the window, the tray icon and the view models.</summary>
public sealed class AppServices
{
    private readonly DispatcherTimer _saveTimer;
    private bool _librarySavePending;
    private bool _settingsSavePending;

    public AppServices(DriverEngine engine, FileLogger log, Dispatcher dispatcher)
    {
        Engine = engine;
        Log = log;
        Dispatcher = dispatcher;
        _saveTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher) { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            FlushPendingSaves();
        };
    }

    public static string Version { get; } = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
                                            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
                                            ?? "dev";

    public DriverEngine Engine { get; }

    public FileLogger Log { get; }

    public Dispatcher Dispatcher { get; }

    /// <summary>Persists profiles.json shortly (debounced so slider drags do not hammer the disk).</summary>
    public void SaveLibrarySoon()
    {
        _librarySavePending = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void SaveSettingsSoon()
    {
        _settingsSavePending = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void SaveLibraryNow()
    {
        try
        {
            // Serialize under the engine lock so a concurrent edit cannot corrupt the snapshot.
            var json = string.Empty;
            Engine.MutateLibrary(library => json = JsonStore.Serialize(library), reevaluate: false);
            JsonStore.SaveText(AppPaths.ProfilesFile, json);
            _librarySavePending = false;
        }
        catch (Exception ex)
        {
            Log.Error("Could not save profiles", ex);
        }
    }

    public void SaveSettingsNow()
    {
        try
        {
            JsonStore.Save(AppPaths.SettingsFile, Engine.Settings);
            _settingsSavePending = false;
        }
        catch (Exception ex)
        {
            Log.Error("Could not save settings", ex);
        }
    }

    public void FlushPendingSaves()
    {
        if (_librarySavePending)
        {
            SaveLibraryNow();
        }

        if (_settingsSavePending)
        {
            SaveSettingsNow();
        }
    }
}

/// <summary>Manages the HKCU Run entry that starts the driver (minimized to the tray) at sign-in.</summary>
public static class AutostartManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WomierDriver";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
        {
            var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine the executable path.");
            key.SetValue(ValueName, $"\"{exe}\" --minimized");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
