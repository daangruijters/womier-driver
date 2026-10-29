using System.IO;
using System.Windows;
using System.Windows.Threading;
using WomierDriver.App.Services;
using WomierDriver.App.Tray;
using WomierDriver.App.ViewModels;
using WomierDriver.App.Views;
using WomierDriver.Core.Automation;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Profiles;

namespace WomierDriver.App;

public partial class App : Application
{
    // A redirected data folder (WOMIER_DRIVER_DATA) gets its own single-instance scope, so a test copy of the
    // driver can run next to the normal one.
    private static readonly string InstanceScope = AppPaths.IsRedirected ? "." + AppPaths.InstanceKey : string.Empty;
    private static readonly string MutexName = @"Local\WomierDriver.SingleInstance" + InstanceScope;
    private static readonly string ShowWindowEventName = @"Local\WomierDriver.ShowWindow" + InstanceScope;

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _showWindowEvent;
    private RegisteredWaitHandle? _showWindowWait;
    private AppServices? _services;
    private TrayIconController? _tray;
    private MainWindow? _window;
    private MainViewModel? _mainViewModel;
    private bool _exiting;

    public static new App Current => (App)Application.Current;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            // Hand over to the running instance: ask it to show its window, then quit.
            try
            {
                using var existing = EventWaitHandle.OpenExisting(ShowWindowEventName);
                existing.Set();
            }
            catch
            {
                // The other instance is starting up or shutting down; nothing more to do.
            }

            Shutdown();
            return;
        }

        _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowWindowEventName);
        _showWindowWait = ThreadPool.RegisterWaitForSingleObject(
            _showWindowEvent,
            (_, _) => Dispatcher.BeginInvoke(ShowMainWindow),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);

        var log = new FileLogger(AppPaths.LogFile);
        DispatcherUnhandledException += (_, args) =>
        {
            log.Error("Unhandled UI exception", args.Exception);
            MessageBox.Show(args.Exception.Message, "Womier SK75 TMR", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) => log.Error("Unhandled exception", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            log.Error("Unobserved task exception", args.Exception);
            args.SetObserved();
        };

        log.Info($"Starting Womier SK75 TMR driver {AppServices.Version} (data: {AppPaths.DataDirectory})");

        var library = LoadOrDefault(AppPaths.ProfilesFile, () => new ProfileLibrary(), log);
        library.EnsureInvariants();
        var settings = LoadOrDefault(AppPaths.SettingsFile, () => new AppSettings(), log);
        settings.StartWithWindows = AutostartManager.IsEnabled();

        var engine = new DriverEngine(library, settings, log);
        _services = new AppServices(engine, log, Dispatcher);
        _mainViewModel = new MainViewModel(_services);
        _tray = new TrayIconController(_services, ShowMainWindow, ExitApplication);

        var startMinimized = e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase) || a.Equals("--tray", StringComparison.OrdinalIgnoreCase));
        if (!startMinimized)
        {
            ShowMainWindow();
        }

        _ = StartEngineAsync(engine, library, log);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _showWindowWait?.Unregister(null);
        _showWindowEvent?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    /// <summary>Shows (or creates) the configuration window and brings it to the front.</summary>
    public void ShowMainWindow()
    {
        if (_services is null || _mainViewModel is null)
        {
            return;
        }

        _window ??= new MainWindow(_mainViewModel, _services);
        if (!_window.IsVisible)
        {
            _window.Show();
        }

        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

        _window.Activate();
        _window.Topmost = true;
        _window.Topmost = false;
        _window.Focus();
    }

    /// <summary>Stops the engine and quits. Called from the tray menu.</summary>
    public async void ExitApplication()
    {
        if (_exiting)
        {
            return;
        }

        _exiting = true;
        try
        {
            _services?.FlushPendingSaves();
            if (_window is not null)
            {
                _window.AllowClose = true;
                _window.Close();
            }

            if (_services is not null)
            {
                await _services.Engine.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _services?.Log.Error("Error while shutting down", ex);
        }
        finally
        {
            Shutdown();
        }
    }

    private async Task StartEngineAsync(DriverEngine engine, ProfileLibrary library, ILogger log)
    {
        try
        {
            await engine.StartAsync();
            if (library.Profiles.Count == 0)
            {
                await CreateStarterProfilesAsync(engine, log);
            }
        }
        catch (Exception ex)
        {
            log.Error("Engine failed to start", ex);
        }
    }

    /// <summary>First run: seed a default profile from what the keyboard holds right now, plus a gaming preset.</summary>
    private async Task CreateStarterProfilesAsync(DriverEngine engine, ILogger log)
    {
        Profile defaultProfile;
        var snapshot = await engine.ReadKeyboardAsync();
        if (snapshot is not null)
        {
            defaultProfile = Profile.FromSnapshot("Default", snapshot.Keys);
            log.Info("Created the default profile from the keyboard's current settings.");
        }
        else
        {
            defaultProfile = Profile.CreateUniform("Default", KeySettings.VendorDefault);
            log.Info("Created the default profile from vendor defaults (keyboard not connected).");
        }

        defaultProfile.IsDefault = true;
        var gaming = Profile.CreateUniform("Gaming", KeySettings.Gaming);

        engine.MutateLibrary(lib =>
        {
            lib.Add(defaultProfile);
            lib.Add(gaming);
        });
        _services?.SaveLibraryNow();
        await Dispatcher.InvokeAsync(() => _mainViewModel?.ReloadProfiles());
    }

    private static T LoadOrDefault<T>(string path, Func<T> fallback, ILogger log) where T : class
    {
        try
        {
            return JsonStore.Load<T>(path) ?? fallback();
        }
        catch (Exception ex)
        {
            log.Error($"Could not read {Path.GetFileName(path)}; starting with defaults", ex);
            try
            {
                File.Copy(path, path + ".corrupt", overwrite: true);
            }
            catch
            {
                // Best effort only.
            }

            return fallback();
        }
    }
}
