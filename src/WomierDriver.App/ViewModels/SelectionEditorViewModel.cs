using CommunityToolkit.Mvvm.ComponentModel;
using WomierDriver.Core.Profiles;

namespace WomierDriver.App.ViewModels;

/// <summary>
/// Edits the settings of all selected keys at once. Each property shows the common value (or marks the
/// selection as "mixed"); setting a property writes it to every selected key.
/// </summary>
public sealed partial class SelectionEditorViewModel : ObservableObject
{
    private readonly Action<Func<KeySettings, KeySettings>> _applyToSelection;
    private bool _loading;

    [ObservableProperty]
    private double _actuation = 2.0;

    [ObservableProperty]
    private bool _isActuationMixed;

    [ObservableProperty]
    private bool? _rapidTrigger;

    [ObservableProperty]
    private double _rtPress = 0.2;

    [ObservableProperty]
    private bool _isRtPressMixed;

    [ObservableProperty]
    private double _rtRelease = 0.2;

    [ObservableProperty]
    private bool _isRtReleaseMixed;

    [ObservableProperty]
    private double _release = 2.8;

    [ObservableProperty]
    private bool _isReleaseMixed;

    [ObservableProperty]
    private double _deadZone = 0.3;

    [ObservableProperty]
    private bool _isDeadZoneMixed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private int _selectedCount;

    [ObservableProperty]
    private KeyLimits _limits = KeyLimits.Default;

    public SelectionEditorViewModel(Action<Func<KeySettings, KeySettings>> applyToSelection)
    {
        _applyToSelection = applyToSelection;
    }

    public bool HasSelection => SelectedCount > 0;

    /// <summary>Rapid-trigger sliders are editable when RT is on for all or some of the selected keys.</summary>
    public bool RapidTriggerControlsEnabled => RapidTrigger != false;

    public void Load(IReadOnlyList<KeySettings> selection, KeyLimits limits)
    {
        _loading = true;
        try
        {
            Limits = limits;
            SelectedCount = selection.Count;
            if (selection.Count == 0)
            {
                return;
            }

            (Actuation, IsActuationMixed) = Common(selection, s => s.ActuationMm);
            (Release, IsReleaseMixed) = Common(selection, s => s.ReleaseMm);
            (RtPress, IsRtPressMixed) = Common(selection, s => s.RtPressMm);
            (RtRelease, IsRtReleaseMixed) = Common(selection, s => s.RtReleaseMm);
            (DeadZone, IsDeadZoneMixed) = Common(selection, s => s.DeadZoneMm);

            var allOn = selection.All(s => s.RapidTrigger);
            var allOff = selection.All(s => !s.RapidTrigger);
            RapidTrigger = allOn ? true : allOff ? false : null;
            OnPropertyChanged(nameof(RapidTriggerControlsEnabled));
        }
        finally
        {
            _loading = false;
        }
    }

    partial void OnActuationChanged(double value)
    {
        if (_loading)
        {
            return;
        }

        IsActuationMixed = false;
        _applyToSelection(s => s with { ActuationMm = value });
    }

    partial void OnReleaseChanged(double value)
    {
        if (_loading)
        {
            return;
        }

        IsReleaseMixed = false;
        _applyToSelection(s => s with { ReleaseMm = value });
    }

    partial void OnRtPressChanged(double value)
    {
        if (_loading)
        {
            return;
        }

        IsRtPressMixed = false;
        _applyToSelection(s => s with { RtPressMm = value });
    }

    partial void OnRtReleaseChanged(double value)
    {
        if (_loading)
        {
            return;
        }

        IsRtReleaseMixed = false;
        _applyToSelection(s => s with { RtReleaseMm = value });
    }

    partial void OnDeadZoneChanged(double value)
    {
        if (_loading)
        {
            return;
        }

        IsDeadZoneMixed = false;
        _applyToSelection(s => s with { DeadZoneMm = value });
    }

    partial void OnRapidTriggerChanged(bool? value)
    {
        OnPropertyChanged(nameof(RapidTriggerControlsEnabled));
        if (_loading || value is null)
        {
            return;
        }

        var on = value.Value;
        _applyToSelection(s => s with { RapidTrigger = on });
    }

    private static (double Value, bool Mixed) Common(IReadOnlyList<KeySettings> selection, Func<KeySettings, double> pick)
    {
        var first = pick(selection[0]);
        var mixed = selection.Any(s => Math.Abs(pick(s) - first) > 0.0001);
        return mixed ? (Math.Round(selection.Average(pick), 2), true) : (first, false);
    }
}
