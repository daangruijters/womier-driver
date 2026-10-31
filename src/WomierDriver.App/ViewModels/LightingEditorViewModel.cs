using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WomierDriver.Core.Device;
using WomierDriver.Core.Profiles;

namespace WomierDriver.App.ViewModels;

/// <summary>A colour button in the lighting editor.</summary>
public sealed record ColorSwatch(string Name, RgbColor Color)
{
    public Brush Brush { get; } = LightingEditorViewModel.ToBrush(Color);
}

/// <summary>
/// Edits the backlight of the selected profile. When <see cref="IsEnabled"/> is off the profile leaves the keyboard's
/// lighting alone; every change is handed to the commit callback, which stores it and lets the engine write it.
/// </summary>
public sealed partial class LightingEditorViewModel : ObservableObject
{
    private readonly Action<LightingSettings?> _commit;
    private readonly Func<Task<LightingSettings?>> _readKeyboard;
    private bool _loading;

    /// <summary>The profile sets the lighting (otherwise the keyboard keeps whatever it shows).</summary>
    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSpeed), nameof(HasColor), nameof(HasOptions), nameof(Options), nameof(OptionLabel), nameof(ShowColorPicker), nameof(IsCustomPattern))]
    private LightingEffectInfo _selectedEffect = Sk75Lighting.Effects[1];

    [ObservableProperty]
    private int _optionIndex;

    [ObservableProperty]
    private double _brightness = LightingSettings.MaxLevel;

    [ObservableProperty]
    private double _speed = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowColorPicker))]
    private bool _rainbow;

    [ObservableProperty]
    private RgbColor _color = RgbColor.White;

    [ObservableProperty]
    private string _colorText = RgbColor.White.ToString();

    [ObservableProperty]
    private Brush _colorBrush = ToBrush(RgbColor.White);

    public LightingEditorViewModel(Action<LightingSettings?> commit, Func<Task<LightingSettings?>> readKeyboard)
    {
        _commit = commit;
        _readKeyboard = readKeyboard;
    }

    public IReadOnlyList<LightingEffectInfo> Effects => Sk75Lighting.Effects;

    /// <summary>The firmware's own preset colours plus white.</summary>
    public IReadOnlyList<ColorSwatch> Swatches { get; } =
    [
        new("Red", new RgbColor(0xFF, 0x00, 0x00)),
        new("Orange", new RgbColor(0xFF, 0x80, 0x00)),
        new("Yellow", new RgbColor(0xFF, 0xFF, 0x00)),
        new("Green", new RgbColor(0x00, 0xFF, 0x00)),
        new("Cyan", new RgbColor(0x00, 0xFF, 0xFF)),
        new("Blue", new RgbColor(0x00, 0x00, 0xFF)),
        new("Purple", new RgbColor(0xFF, 0x00, 0xFF)),
        new("White", RgbColor.White),
    ];

    public bool HasSpeed => SelectedEffect.HasSpeed;

    public bool HasColor => SelectedEffect.HasColor;

    public bool HasOptions => SelectedEffect.Options.Count > 0;

    public IReadOnlyList<string> Options => SelectedEffect.Options;

    public string OptionLabel => SelectedEffect.Effect == LightingEffect.CustomPattern ? "Pattern" : "Direction";

    public bool ShowColorPicker => HasColor && !Rainbow;

    public bool IsCustomPattern => SelectedEffect.Effect == LightingEffect.CustomPattern;

    public static Brush ToBrush(RgbColor color)
    {
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }

    /// <summary>Shows <paramref name="lighting"/> (the selected profile's setting, <c>null</c> = not set) without committing anything.</summary>
    public void Load(LightingSettings? lighting)
    {
        _loading = true;
        try
        {
            IsEnabled = lighting is not null;
            Show(lighting ?? new LightingSettings());
        }
        finally
        {
            _loading = false;
        }
    }

    [RelayCommand]
    private void PickColor(ColorSwatch? swatch)
    {
        if (swatch is not null)
        {
            Color = swatch.Color;
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_loading)
        {
            return;
        }

        if (!value)
        {
            _commit(null);
            return;
        }

        _ = StartFromKeyboardAsync();
    }

    /// <summary>Switching the lighting on starts from what the keyboard shows right now, so nothing changes until edited.</summary>
    private async Task StartFromKeyboardAsync()
    {
        LightingSettings? current = null;
        try
        {
            current = await _readKeyboard();
        }
        catch
        {
            // Not connected or not answering: start from the defaults.
        }

        if (!IsEnabled)
        {
            return;
        }

        if (current is not null && Sk75Lighting.Find(current.Effect) is not null)
        {
            _loading = true;
            try
            {
                Show(current.Sanitized());
            }
            finally
            {
                _loading = false;
            }
        }

        Commit();
    }

    partial void OnSelectedEffectChanged(LightingEffectInfo value)
    {
        if (OptionIndex < 0 || OptionIndex >= value.Options.Count)
        {
            OptionIndex = 0;
        }

        Commit();
    }

    partial void OnOptionIndexChanged(int value)
    {
        // The combo box drops its selection (-1) whenever its list changes with the effect; fall back to the first option.
        if (value < 0 && HasOptions)
        {
            OptionIndex = 0;
            return;
        }

        Commit();
    }

    partial void OnBrightnessChanged(double value) => Commit();

    partial void OnSpeedChanged(double value) => Commit();

    partial void OnRainbowChanged(bool value) => Commit();

    partial void OnColorChanged(RgbColor value)
    {
        ColorBrush = ToBrush(value);
        if (!RgbColor.TryParse(ColorText, out var typed) || typed != value)
        {
            ColorText = value.ToString();
        }

        Commit();
    }

    partial void OnColorTextChanged(string value)
    {
        if (RgbColor.TryParse(value, out var color))
        {
            Color = color;
        }
    }

    private void Show(LightingSettings lighting)
    {
        SelectedEffect = Sk75Lighting.Find(lighting.Effect) ?? Sk75Lighting.Effects[1];
        OptionIndex = lighting.Option < SelectedEffect.Options.Count ? lighting.Option : 0;
        Brightness = lighting.Brightness;
        Speed = lighting.Speed;
        Rainbow = lighting.Rainbow;
        Color = lighting.Color;
    }

    private void Commit()
    {
        if (_loading || !IsEnabled)
        {
            return;
        }

        _commit(new LightingSettings
        {
            Effect = SelectedEffect.Effect,
            Option = Math.Max(0, OptionIndex),
            Brightness = (int)Math.Round(Brightness),
            Speed = (int)Math.Round(Speed),
            Rainbow = Rainbow,
            Color = Color,
        }.Sanitized());
    }
}
