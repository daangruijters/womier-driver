using System.Globalization;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using WomierDriver.Core.Device;
using WomierDriver.Core.Profiles;

namespace WomierDriver.App.ViewModels;

/// <summary>One key on the keyboard canvas: geometry, current settings of the selected profile, selection state.</summary>
public sealed partial class KeyViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Fill), nameof(ValueText), nameof(ShowRapidTrigger), nameof(Foreground))]
    private KeySettings _settings = KeySettings.VendorDefault;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Stroke), nameof(StrokeThickness), nameof(Fill), nameof(Foreground))]
    private bool _isSelected;

    public KeyViewModel(KeyDefinition definition)
    {
        Definition = definition;
    }

    public KeyDefinition Definition { get; }

    public int Slot => Definition.Slot;

    public string Label => Definition.Label;

    public string Code => Definition.Code;

    public double X => Definition.X;

    public double Y => Definition.Y;

    public double Width => Definition.Width;

    public double Height => Definition.Height;

    /// <summary>Lower bound of the actuation range, used to normalize the colour scale.</summary>
    public static double ScaleMin { get; set; } = 0.1;

    /// <summary>Upper bound of the actuation range, used to normalize the colour scale.</summary>
    public static double ScaleMax { get; set; } = 3.3;

    public string ValueText => Settings.ActuationMm.ToString("0.00", CultureInfo.InvariantCulture);

    public bool ShowRapidTrigger => Settings.RapidTrigger;

    public Brush Fill => KeyPalette.Fill(Normalized(Settings.ActuationMm), IsSelected);

    public Brush Foreground => KeyPalette.Foreground(Normalized(Settings.ActuationMm), IsSelected);

    public Brush Stroke => IsSelected ? KeyPalette.SelectedStroke : KeyPalette.Stroke;

    public Thickness StrokeThickness => IsSelected ? new Thickness(2) : new Thickness(1);

    /// <summary>Re-reads theme brushes (after a light/dark switch) and refreshes the visuals.</summary>
    public void RefreshVisuals()
    {
        OnPropertyChanged(nameof(Fill));
        OnPropertyChanged(nameof(Foreground));
        OnPropertyChanged(nameof(Stroke));
    }

    private static double Normalized(double mm)
    {
        var range = ScaleMax - ScaleMin;
        return range <= 0 ? 0 : Math.Clamp((mm - ScaleMin) / range, 0, 1);
    }
}

/// <summary>
/// Colour scale for the key canvas. Shallow actuation (sensitive keys) is tinted strongly with the accent colour,
/// deep actuation fades to the neutral control colour, so a glance shows which keys are "hair triggers".
/// Brushes are resolved from the live Fluent theme so light and dark mode both work.
/// </summary>
public static class KeyPalette
{
    private static readonly Dictionary<int, SolidColorBrush> FillCache = new();
    private static readonly Dictionary<int, SolidColorBrush> TextCache = new();
    private static Color _accent;
    private static Color _neutral;
    private static Color _textPrimary;
    private static Color _textOnAccent;
    private static bool _resolved;

    public static Brush Stroke => Resource("ControlStrokeColorDefaultBrush");

    public static Brush SelectedStroke => Resource("AccentFillColorDefaultBrush");

    /// <summary>Forget cached theme colours; call when the system theme changes.</summary>
    public static void Invalidate()
    {
        _resolved = false;
        FillCache.Clear();
        TextCache.Clear();
    }

    public static Brush Fill(double normalizedDepth, bool selected)
    {
        Resolve();
        var strength = 1 - normalizedDepth; // shallow = strong accent
        var amount = 0.12 + 0.78 * strength * strength;
        var key = (int)(amount * 1000) * 2 + (selected ? 1 : 0);
        if (!FillCache.TryGetValue(key, out var brush))
        {
            var color = Blend(_neutral, _accent, selected ? Math.Min(1, amount + 0.1) : amount);
            brush = new SolidColorBrush(color);
            brush.Freeze();
            FillCache[key] = brush;
        }

        return brush;
    }

    public static Brush Foreground(double normalizedDepth, bool selected)
    {
        Resolve();
        var strength = 1 - normalizedDepth;
        var amount = 0.12 + 0.78 * strength * strength;
        var onAccent = amount > 0.45;
        var key = onAccent ? 1 : 0;
        if (!TextCache.TryGetValue(key, out var brush))
        {
            brush = new SolidColorBrush(onAccent ? _textOnAccent : _textPrimary);
            brush.Freeze();
            TextCache[key] = brush;
        }

        return brush;
    }

    private static void Resolve()
    {
        if (_resolved)
        {
            return;
        }

        _accent = Color("AccentFillColorDefaultBrush", System.Windows.Media.Color.FromRgb(0x00, 0x78, 0xD4));
        _neutral = Color("ControlFillColorDefaultBrush", System.Windows.Media.Color.FromRgb(0xF3, 0xF3, 0xF3));
        _textPrimary = Color("TextFillColorPrimaryBrush", Colors.Black);
        _textOnAccent = Color("TextOnAccentFillColorPrimaryBrush", Colors.White);

        // Control fills are translucent in the Fluent theme; composite them over the card colour so blends stay opaque.
        var card = Color("CardBackgroundFillColorDefaultBrush", System.Windows.Media.Color.FromRgb(0xFB, 0xFB, 0xFB));
        var baseLayer = Color("SolidBackgroundFillColorBaseBrush", System.Windows.Media.Color.FromRgb(0xF3, 0xF3, 0xF3));
        var cardOpaque = Over(card, baseLayer);
        _neutral = Over(_neutral, cardOpaque);
        _resolved = true;
    }

    private static Brush Resource(string key)
        => Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;

    private static Color Color(string key, Color fallback)
        => Application.Current?.TryFindResource(key) is SolidColorBrush brush ? brush.Color : fallback;

    private static Color Over(Color top, Color bottom)
    {
        var a = top.A / 255.0;
        return System.Windows.Media.Color.FromRgb(
            (byte)Math.Round(top.R * a + bottom.R * (1 - a)),
            (byte)Math.Round(top.G * a + bottom.G * (1 - a)),
            (byte)Math.Round(top.B * a + bottom.B * (1 - a)));
    }

    private static Color Blend(Color from, Color to, double amount)
        => System.Windows.Media.Color.FromRgb(
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));
}
