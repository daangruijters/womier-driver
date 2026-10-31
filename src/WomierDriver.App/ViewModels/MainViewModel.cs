using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WomierDriver.App.Services;
using WomierDriver.Core.Automation;
using WomierDriver.Core.Device;
using WomierDriver.Core.Profiles;
using WomierDriver.Core.Protocol;

namespace WomierDriver.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppServices _services;
    private readonly DriverEngine _engine;
    private readonly DispatcherTimer _applyTimer;
    private readonly DispatcherTimer _liveTimer;
    private readonly KeyViewModel?[] _keysBySlot = new KeyViewModel?[MagnetismStreams.SlotCount];
    private readonly double[] _pendingDepth = new double[MagnetismStreams.SlotCount];
    private readonly bool[] _pendingDirty = new bool[MagnetismStreams.SlotCount];
    private readonly List<(int Slot, double Mm)> _flushBatch = new(MagnetismStreams.SlotCount);
    private readonly object _liveLock = new();
    private bool _pendingAny;
    private bool _suppressSelectionLoad;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProfileCommand), nameof(DuplicateProfileCommand), nameof(SetDefaultCommand), nameof(ActivateProfileCommand), nameof(ReapplyCommand), nameof(ExportProfileCommand), nameof(ReadFromKeyboardCommand))]
    private ProfileViewModel? _selectedProfile;

    [ObservableProperty]
    private string _selectionSummary = string.Empty;

    [ObservableProperty]
    private string _connectionText = "Looking for the keyboard…";

    [ObservableProperty]
    private Brush _connectionBrush = Brushes.Gray;

    /// <summary>"Battery 80 %" while the keyboard uses its 2.4 GHz receiver; <c>null</c> otherwise.</summary>
    [ObservableProperty]
    private string? _batteryText;

    /// <summary>Battery glyph (Segoe Fluent Icons) matching <see cref="BatteryText"/>.</summary>
    [ObservableProperty]
    private string _batteryGlyph = string.Empty;

    /// <summary>Caution / critical colour while the battery is low, the secondary text colour otherwise.</summary>
    [ObservableProperty]
    private Brush _batteryBrush = Brushes.Gray;

    [ObservableProperty]
    private string _activeSummaryText = string.Empty;

    [ObservableProperty]
    private string _lastApplyText = string.Empty;

    [ObservableProperty]
    private bool _isApplying;

    [ObservableProperty]
    private string _newGameText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReadFromKeyboardCommand), nameof(ReapplyCommand), nameof(ActivateProfileCommand))]
    private bool _isConnected;

    /// <summary>Live view: tiles show the keyboard's current key travel and which keys Windows receives. Session-only.</summary>
    [ObservableProperty]
    private bool _isLiveViewEnabled;

    [ObservableProperty]
    private string? _liveStatusText;

    public MainViewModel(AppServices services)
    {
        _services = services;
        _engine = services.Engine;
        Editor = new SelectionEditorViewModel(ApplyToSelection);
        foreach (var definition in Sk75TmrLayout.Keys)
        {
            var key = new KeyViewModel(definition);
            Keys.Add(key);
            _keysBySlot[definition.Slot] = key;
        }

        _applyTimer = new DispatcherTimer(DispatcherPriority.Background, services.Dispatcher) { Interval = TimeSpan.FromMilliseconds(700) };
        _applyTimer.Tick += (_, _) =>
        {
            _applyTimer.Stop();
            _engine.NotifyProfilesChanged();
        };

        // Travel samples arrive at up to ~1 kHz on a background thread; the tiles are refreshed at most every 16 ms.
        _liveTimer = new DispatcherTimer(DispatcherPriority.Render, services.Dispatcher) { Interval = TimeSpan.FromMilliseconds(16) };
        _liveTimer.Tick += (_, _) => FlushLiveTravel();
        _engine.LiveTravel += OnLiveTravel;

        _engine.StateChanged += (_, _) => services.Dispatcher.BeginInvoke(RefreshEngineState);
        ReloadProfiles();
        RefreshEngineState();
    }

    /// <summary>Raised on the UI thread when the live view is switched on or off (the window registers Raw Input accordingly).</summary>
    public event Action<bool>? LiveViewChanged;

    public ObservableCollection<ProfileViewModel> Profiles { get; } = [];

    public ObservableCollection<KeyViewModel> Keys { get; } = [];

    public SelectionEditorViewModel Editor { get; }

    public string VersionText => $"Driver {AppServices.Version}";

    public bool AutoSwitchEnabled
    {
        get => _engine.Settings.AutoSwitchEnabled;
        set
        {
            if (value == _engine.Settings.AutoSwitchEnabled)
            {
                return;
            }

            _engine.MutateSettings(s => s.AutoSwitchEnabled = value);
            _services.SaveSettingsSoon();
            OnPropertyChanged();
        }
    }

    public bool NotifyOnSwitch
    {
        get => _engine.Settings.NotifyOnProfileSwitch;
        set
        {
            if (value == _engine.Settings.NotifyOnProfileSwitch)
            {
                return;
            }

            _engine.MutateSettings(s => s.NotifyOnProfileSwitch = value);
            _services.SaveSettingsSoon();
            OnPropertyChanged();
        }
    }

    public bool NotifyOnLowBattery
    {
        get => _engine.Settings.NotifyOnLowBattery;
        set
        {
            if (value == _engine.Settings.NotifyOnLowBattery)
            {
                return;
            }

            _engine.MutateSettings(s => s.NotifyOnLowBattery = value);
            _services.SaveSettingsSoon();
            OnPropertyChanged();
        }
    }

    public bool StartWithWindows
    {
        get => _engine.Settings.StartWithWindows;
        set
        {
            if (value == _engine.Settings.StartWithWindows)
            {
                return;
            }

            try
            {
                AutostartManager.SetEnabled(value);
                _engine.MutateSettings(s => s.StartWithWindows = value);
                _services.SaveSettingsSoon();
            }
            catch (Exception ex)
            {
                _services.Log.Error("Could not update the autostart entry", ex);
                MessageBox.Show($"Could not update the Windows autostart entry.\n\n{ex.Message}", "Womier SK75 TMR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            OnPropertyChanged();
        }
    }

    /// <summary>Rebuilds the profile list from the engine's library (after load or external changes).</summary>
    public void ReloadProfiles()
    {
        var selectedId = SelectedProfile?.Id;
        Profiles.Clear();
        foreach (var profile in _engine.Library.Profiles)
        {
            Profiles.Add(new ProfileViewModel(profile, OnProfileMetadataChanged));
        }

        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == selectedId) ?? Profiles.FirstOrDefault(p => p.IsActive) ?? Profiles.FirstOrDefault();
        RefreshEngineState();
    }

    /// <summary>Called by the keyboard view after the user changed the selection.</summary>
    public void SelectionChanged() => LoadSelectionIntoEditor();

    /// <summary>Called by the window's Raw Input listener, on the UI thread, when Windows receives or releases a key.</summary>
    public void OnPhysicalKey(int slot, bool isDown)
    {
        if (IsLiveViewEnabled && _keysBySlot[slot] is { } key)
        {
            key.IsPressed = isDown;
        }
    }

    /// <summary>Called by the window when it hides: the live view belongs to the window, not to the tray driver.</summary>
    public void OnWindowHidden() => IsLiveViewEnabled = false;

    partial void OnIsLiveViewEnabledChanged(bool value)
    {
        foreach (var key in Keys)
        {
            key.IsLive = value;
            if (!value)
            {
                key.ResetLive();
            }
        }

        if (value)
        {
            _liveTimer.Start();
        }
        else
        {
            _liveTimer.Stop();
            lock (_liveLock)
            {
                Array.Clear(_pendingDirty);
                _pendingAny = false;
            }
        }

        UpdateMarkers();
        RefreshLiveStatus();
        LiveViewChanged?.Invoke(value);
        _ = SetEngineLiveViewAsync(value);
    }

    private async Task SetEngineLiveViewAsync(bool enabled)
    {
        try
        {
            // Opening the input collection enumerates HID devices (a second or so); keep that off the UI thread.
            await Task.Run(() => _engine.SetLiveViewAsync(enabled));
        }
        catch (Exception ex)
        {
            _services.Log.Error("Could not switch the live view", ex);
        }
    }

    private void OnLiveTravel(object? sender, LiveTravelSample sample)
    {
        lock (_liveLock)
        {
            _pendingDepth[sample.Slot] = sample.Mm;
            _pendingDirty[sample.Slot] = true;
            _pendingAny = true;
        }
    }

    private void FlushLiveTravel()
    {
        lock (_liveLock)
        {
            if (!_pendingAny)
            {
                return;
            }

            _pendingAny = false;
            _flushBatch.Clear();
            for (var slot = 0; slot < _pendingDirty.Length; slot++)
            {
                if (_pendingDirty[slot])
                {
                    _pendingDirty[slot] = false;
                    _flushBatch.Add((slot, _pendingDepth[slot]));
                }
            }
        }

        foreach (var (slot, mm) in _flushBatch)
        {
            if (_keysBySlot[slot] is { } key)
            {
                key.LiveDepthMm = mm;
            }
        }
    }

    private void UpdateMarkers()
    {
        var enabled = SelectedProfile?.IsActive == true;
        foreach (var key in Keys)
        {
            key.MarkersEnabled = enabled;
        }
    }

    private void RefreshLiveStatus()
    {
        LiveStatusText = !IsLiveViewEnabled
            ? null
            : _engine.LiveView switch
            {
                LiveViewState.Streaming => SelectedProfile?.IsActive == true
                    ? "press keys to see their travel"
                    : "actuation marks need the active profile",
                LiveViewState.Waiting => "waiting for the keyboard",
                _ => "starting…",
            };
    }

    partial void OnSelectedProfileChanged(ProfileViewModel? value)
    {
        foreach (var key in Keys)
        {
            key.Settings = value?.Model.GetKey(key.Slot) ?? KeySettings.VendorDefault;
        }

        UpdateMarkers();
        RefreshLiveStatus();
        LoadSelectionIntoEditor();
        NewGameText = string.Empty;
    }

    [RelayCommand]
    private void NewProfile()
    {
        var template = SelectedProfile?.Model;
        var profile = template is null
            ? Profile.CreateUniform(_engine.Library.UniqueName("New profile"), KeySettings.VendorDefault)
            : template.Clone(_engine.Library.UniqueName(template.Name));
        profile.Games.Clear();
        _engine.MutateLibrary(lib => lib.Add(profile));
        _services.SaveLibrarySoon();
        var vm = new ProfileViewModel(profile, OnProfileMetadataChanged);
        Profiles.Add(vm);
        SelectedProfile = vm;
        RefreshProfilesFromModel();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void DuplicateProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var copy = SelectedProfile.Model.Clone(_engine.Library.UniqueName(SelectedProfile.Name + " copy"));
        _engine.MutateLibrary(lib => lib.Add(copy));
        _services.SaveLibrarySoon();
        var vm = new ProfileViewModel(copy, OnProfileMetadataChanged);
        Profiles.Add(vm);
        SelectedProfile = vm;
        RefreshProfilesFromModel();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void DeleteProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var answer = MessageBox.Show($"Delete the profile \"{SelectedProfile.Name}\"?", "Womier SK75 TMR", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        var id = SelectedProfile.Id;
        var index = Profiles.IndexOf(SelectedProfile);
        _engine.MutateLibrary(lib => lib.Remove(id));
        _services.SaveLibrarySoon();
        Profiles.Remove(SelectedProfile);
        SelectedProfile = Profiles.Count == 0 ? null : Profiles[Math.Clamp(index, 0, Profiles.Count - 1)];
        RefreshProfilesFromModel();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void SetDefault()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var id = SelectedProfile.Id;
        _engine.MutateLibrary(lib => lib.SetDefault(id));
        _services.SaveLibrarySoon();
        RefreshProfilesFromModel();
    }

    [RelayCommand(CanExecute = nameof(CanActivate))]
    private void ActivateProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        _engine.SetManualProfile(SelectedProfile.Id);
    }

    private bool CanActivate() => SelectedProfile is not null && IsConnected;

    [RelayCommand(CanExecute = nameof(CanActivate))]
    private async Task ReapplyAsync()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var result = await _engine.ApplyProfileAsync(SelectedProfile.Model);
        if (!result.Success)
        {
            MessageBox.Show(result.Message, "Womier SK75 TMR", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand(CanExecute = nameof(CanActivate))]
    private async Task ReadFromKeyboardAsync()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var answer = MessageBox.Show(
            $"Replace all key settings of \"{SelectedProfile.Name}\" with what the keyboard currently holds?",
            "Womier SK75 TMR",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        var snapshot = await _engine.ReadKeyboardAsync();
        if (snapshot is null)
        {
            MessageBox.Show("The keyboard did not answer.", "Womier SK75 TMR", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var profile = SelectedProfile.Model;
        _engine.MutateLibrary(_ =>
        {
            foreach (var (slot, settings) in snapshot.Keys)
            {
                profile.SetKey(slot, settings);
            }
        });
        _services.SaveLibrarySoon();
        OnSelectedProfileChanged(SelectedProfile);
        SelectedProfile.NotifyKeysChanged();
    }

    [RelayCommand]
    private void SelectGroup(string? group)
    {
        if (string.IsNullOrEmpty(group))
        {
            return;
        }

        var additive = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        Func<KeyViewModel, bool> predicate = group switch
        {
            "All" => _ => true,
            "None" => _ => false,
            "WASD" => k => k.Definition.IsIn(KeyGroup.Wasd),
            "Letters" => k => k.Definition.IsIn(KeyGroup.Letters),
            "Numbers" => k => k.Definition.IsIn(KeyGroup.Digits),
            "F-row" => k => k.Definition.IsIn(KeyGroup.FunctionRow),
            "Modifiers" => k => k.Definition.IsIn(KeyGroup.Modifiers),
            "Arrows" => k => k.Definition.IsIn(KeyGroup.Arrows),
            "RT" => k => k.Settings.RapidTrigger,
            _ => _ => false,
        };

        _suppressSelectionLoad = true;
        foreach (var key in Keys)
        {
            key.IsSelected = predicate(key) || (additive && group != "None" && key.IsSelected);
        }

        _suppressSelectionLoad = false;
        LoadSelectionIntoEditor();
    }

    [RelayCommand]
    private void AddGame()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var text = NewGameText;
        NewGameText = string.Empty;
        AddGames([text]);
    }

    [RelayCommand]
    private void RemoveGame(string? game)
    {
        if (SelectedProfile is null || game is null)
        {
            return;
        }

        var profile = SelectedProfile.Model;
        _engine.MutateLibrary(_ => profile.RemoveGame(game));
        SelectedProfile.Games.Remove(game);
        _services.SaveLibrarySoon();
        RefreshProfilesFromModel();
    }

    [RelayCommand]
    private void PickRunningProcess()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var picker = new Views.PickProcessWindow { Owner = Application.Current.MainWindow };
        if (picker.ShowDialog() == true)
        {
            AddGames(picker.SelectedProcesses);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void ExportProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export profile",
            Filter = "Womier profile (*.json)|*.json",
            FileName = SanitizeFileName(SelectedProfile.Name) + ".json",
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            JsonStore.Save(dialog.FileName, SelectedProfile.Model);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not export the profile.\n\n{ex.Message}", "Womier SK75 TMR", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void ImportProfile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import profile",
            Filter = "Womier profile (*.json)|*.json|All files (*.*)|*.*",
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var imported = JsonStore.Load<Profile>(dialog.FileName) ?? throw new InvalidDataException("The file does not contain a profile.");
            imported.Id = Guid.NewGuid();
            imported.IsDefault = false;
            imported.Name = _engine.Library.UniqueName(string.IsNullOrWhiteSpace(imported.Name) ? Path.GetFileNameWithoutExtension(dialog.FileName) : imported.Name);
            imported.Keys = imported.Keys.Where(k => Sk75TmrLayout.Slots.Contains(k.Key)).ToDictionary(k => k.Key, k => k.Value.Clamp(_engine.Limits));
            _engine.MutateLibrary(lib => lib.Add(imported));
            _services.SaveLibrarySoon();
            var vm = new ProfileViewModel(imported, OnProfileMetadataChanged);
            Profiles.Add(vm);
            SelectedProfile = vm;
            RefreshProfilesFromModel();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not import the profile.\n\n{ex.Message}", "Womier SK75 TMR", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool HasSelectedProfile() => SelectedProfile is not null;

    private void AddGames(IEnumerable<string> names)
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var profile = SelectedProfile.Model;
        var added = new List<string>();
        _engine.MutateLibrary(_ =>
        {
            foreach (var name in names)
            {
                var normalized = GameRule.Normalize(name);
                if (normalized.Length > 0 && profile.AddGame(normalized))
                {
                    added.Add(normalized);
                }
            }
        });

        foreach (var game in added)
        {
            SelectedProfile.Games.Add(game);
        }

        if (added.Count > 0)
        {
            _services.SaveLibrarySoon();
            RefreshProfilesFromModel();
        }
    }

    private void ApplyToSelection(Func<KeySettings, KeySettings> mutate)
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var profile = SelectedProfile.Model;
        var limits = _engine.Limits;
        var selected = Keys.Where(k => k.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        _engine.MutateLibrary(
            _ =>
            {
                foreach (var key in selected)
                {
                    var next = mutate(key.Settings).Clamp(limits);
                    key.Settings = next;
                    profile.SetKey(key.Slot, next);
                }
            },
            reevaluate: false);

        SelectedProfile.NotifyKeysChanged();
        _services.SaveLibrarySoon();
        _applyTimer.Stop();
        _applyTimer.Start();
    }

    private void LoadSelectionIntoEditor()
    {
        if (_suppressSelectionLoad)
        {
            return;
        }

        var selected = Keys.Where(k => k.IsSelected).ToList();
        Editor.Load(selected.Select(k => k.Settings).ToList(), _engine.Limits);
        SelectionSummary = selected.Count switch
        {
            0 => "No keys selected",
            1 => $"1 key selected: {selected[0].Code}",
            <= 6 => $"{selected.Count} keys selected: {string.Join(", ", selected.Select(k => k.Label))}",
            _ => $"{selected.Count} keys selected",
        };
    }

    private void OnProfileMetadataChanged(ProfileViewModel profile)
    {
        _services.SaveLibrarySoon();
        _engine.NotifyProfilesChanged();
    }

    private void RefreshProfilesFromModel()
    {
        foreach (var profile in Profiles)
        {
            profile.RefreshFromModel();
        }

        RefreshEngineState();
    }

    private void RefreshEngineState()
    {
        var keyboard = _engine.Keyboard;
        IsConnected = keyboard is not null;
        if (!IsConnected && IsLiveViewEnabled)
        {
            IsLiveViewEnabled = false;
        }

        ConnectionText = ConnectionDescription.Describe(_engine);
        BatteryText = ConnectionDescription.Battery(_engine);
        if (ConnectionDescription.BatteryPercent(_engine) is { } battery)
        {
            BatteryGlyph = ConnectionDescription.BatteryGlyph(battery);
            BatteryBrush = BatteryAlerts.Classify(battery) switch
            {
                BatteryAlert.Critical => Application.Current.TryFindResource("SystemFillColorCriticalBrush") as Brush ?? Brushes.Red,
                BatteryAlert.Low => Application.Current.TryFindResource("SystemFillColorCautionBrush") as Brush ?? Brushes.Orange,
                _ => Application.Current.TryFindResource("TextFillColorSecondaryBrush") as Brush ?? Brushes.Gray,
            };
        }

        ConnectionBrush = _engine.IsKeyboardAsleep
            ? Application.Current.TryFindResource("SystemFillColorCautionBrush") as Brush ?? Brushes.Orange
            : keyboard is null
                ? Application.Current.TryFindResource("TextFillColorTertiaryBrush") as Brush ?? Brushes.Gray
                : Application.Current.TryFindResource("SystemFillColorSuccessBrush") as Brush ?? Brushes.Green;

        var limits = _engine.Limits;
        if (Math.Abs(KeyViewModel.ScaleMax - limits.TravelMax) > 0.001 || Math.Abs(KeyViewModel.ScaleMin - limits.TravelMin) > 0.001)
        {
            KeyViewModel.ScaleMin = limits.TravelMin;
            KeyViewModel.ScaleMax = limits.TravelMax;
            foreach (var key in Keys)
            {
                key.RefreshVisuals();
            }
        }

        foreach (var profile in Profiles)
        {
            profile.UpdateActivation(_engine);
        }

        var active = _engine.ActiveProfile;
        ActiveSummaryText = active is null
            ? "No active profile"
            : _engine.ActiveReason switch
            {
                ActivationReason.Game => $"Active: {active.Name} ({_engine.ActiveTrigger} is running)",
                ActivationReason.Manual => $"Active: {active.Name} (chosen manually)",
                ActivationReason.Default => $"Active: {active.Name} (default)",
                _ => $"Active: {active.Name}",
            };

        IsApplying = _engine.IsApplying;
        if (IsApplying && IsLiveViewEnabled)
        {
            // The travel stream is paused while the keyboard is written; drop bars that would otherwise go stale.
            foreach (var key in Keys)
            {
                key.LiveDepthMm = 0;
            }
        }

        UpdateMarkers();
        RefreshLiveStatus();
        var last = _engine.LastApply;
        LastApplyText = _engine.IsApplying
            ? "Writing to the keyboard…"
            : last is null
                ? (keyboard is null ? "Connect the keyboard with its USB cable or receiver." : "Ready")
                : last.Success
                    ? $"{last.When:HH:mm:ss} · {last.ProfileName}: {last.Message}"
                    : $"{last.When:HH:mm:ss} · {last.ProfileName}: failed – {last.Message}";

        ReapplyCommand.NotifyCanExecuteChanged();
        ActivateProfileCommand.NotifyCanExecuteChanged();
        ReadFromKeyboardCommand.NotifyCanExecuteChanged();
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var result = new string(chars).Trim();
        return result.Length == 0 ? "profile" : result;
    }
}
