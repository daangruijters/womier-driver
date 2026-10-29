using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WomierDriver.Core.Automation;
using WomierDriver.Core.Profiles;

namespace WomierDriver.App.ViewModels;

/// <summary>Sidebar / header representation of one <see cref="Profile"/>.</summary>
public sealed partial class ProfileViewModel : ObservableObject
{
    private readonly Action<ProfileViewModel> _onMetadataChanged;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtitle))]
    private bool _isDefault;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLine), nameof(Subtitle))]
    private bool _isActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLine))]
    private string _activeDetail = string.Empty;

    public ProfileViewModel(Profile model, Action<ProfileViewModel> onMetadataChanged)
    {
        Model = model;
        _onMetadataChanged = onMetadataChanged;
        _isDefault = model.IsDefault;
        Games = new ObservableCollection<string>(model.Games);
        Games.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Subtitle));
            OnPropertyChanged(nameof(StatusLine));
            OnPropertyChanged(nameof(HasGames));
        };
    }

    public Profile Model { get; }

    public Guid Id => Model.Id;

    public ObservableCollection<string> Games { get; }

    public bool HasGames => Games.Count > 0;

    public string Name
    {
        get => Model.Name;
        set
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length == 0 || trimmed == Model.Name)
            {
                return;
            }

            Model.Name = trimmed;
            Model.Touch();
            OnPropertyChanged();
            _onMetadataChanged(this);
        }
    }

    /// <summary>Second line in the sidebar.</summary>
    public string Subtitle
    {
        get
        {
            var parts = new List<string>
            {
                Games.Count switch
                {
                    0 => IsDefault ? "When no game runs" : "No games assigned",
                    1 => Games[0],
                    _ => $"{Games.Count} games",
                },
            };

            var rt = Model.RapidTriggerKeyCount;
            if (rt > 0)
            {
                parts.Add(rt == Model.Keys.Count ? "rapid trigger" : $"{rt} RT keys");
            }

            return string.Join(" · ", parts);
        }
    }

    /// <summary>Status under the profile name in the editor header.</summary>
    public string StatusLine
    {
        get
        {
            if (IsActive)
            {
                return ActiveDetail.Length == 0 ? "Active on the keyboard" : $"Active on the keyboard · {ActiveDetail}";
            }

            if (Games.Count > 0)
            {
                return $"Applied automatically when {string.Join(", ", Games)} is running";
            }

            return IsDefault ? "Applied whenever no game profile matches" : "Not active · assign a game or activate it from the tray";
        }
    }

    public void RefreshFromModel()
    {
        IsDefault = Model.IsDefault;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(StatusLine));
    }

    public void UpdateActivation(DriverEngine engine)
    {
        var active = engine.ActiveProfile?.Id == Model.Id;
        var detail = active
            ? engine.ActiveReason switch
            {
                ActivationReason.Game when engine.ActiveTrigger is not null => $"{engine.ActiveTrigger} is running",
                ActivationReason.Manual => "selected manually",
                ActivationReason.Default => "default profile",
                _ => string.Empty,
            }
            : string.Empty;
        IsActive = active;
        ActiveDetail = detail;
    }

    public void NotifyKeysChanged() => OnPropertyChanged(nameof(Subtitle));
}
