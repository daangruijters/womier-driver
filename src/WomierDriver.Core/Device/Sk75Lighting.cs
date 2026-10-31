using WomierDriver.Core.Profiles;

namespace WomierDriver.Core.Device;

/// <summary>What one backlight effect lets the user choose.</summary>
/// <param name="Effect">Firmware value.</param>
/// <param name="Name">Label shown to the user (the vendor software's English name).</param>
/// <param name="HasSpeed">The effect is animated and honours <see cref="LightingSettings.Speed"/>.</param>
/// <param name="HasColor">The effect uses <see cref="LightingSettings.Color"/> or, with <see cref="LightingSettings.Rainbow"/>, all colours.</param>
/// <param name="Options">Labels of <see cref="LightingSettings.Option"/> (direction or pattern); empty when the effect has none.</param>
public sealed record LightingEffectInfo(LightingEffect Effect, string Name, bool HasSpeed, bool HasColor, IReadOnlyList<string> Options);

/// <summary>
/// The backlight effects of the SK75 TMR, as listed by the vendor software for this model (its <c>lightLayout</c>).
/// Music and screen sync are left out: they only work while the vendor application streams audio or screen data.
/// </summary>
public static class Sk75Lighting
{
    public static IReadOnlyList<LightingEffectInfo> Effects { get; } =
    [
        new(LightingEffect.Off, "Off", false, false, []),
        new(LightingEffect.AlwaysOn, "Always on", false, true, []),
        new(LightingEffect.Breathing, "Breathing", true, true, []),
        new(LightingEffect.SpectrumCycle, "Spectrum cycle", true, false, []),
        new(LightingEffect.Wave, "Wave", true, true, ["Right", "Left", "Down", "Up"]),
        new(LightingEffect.Ripple, "Ripple", true, true, []),
        new(LightingEffect.Starlight, "Starlight", true, true, []),
        new(LightingEffect.Stream, "Stream", true, true, ["Zigzag", "Round"]),
        new(LightingEffect.Shadow, "Shadow", true, true, []),
        new(LightingEffect.MountainWave, "Mountain wave", true, true, []),
        new(LightingEffect.SineWave, "Sine wave", true, true, []),
        new(LightingEffect.ColorSpring, "Color spring", true, true, ["Outward", "Inward"]),
        new(LightingEffect.SnowTrace, "Snow trace", true, true, []),
        new(LightingEffect.FlowerWave, "Flower wave", true, true, ["Right", "Left"]),
        new(LightingEffect.Laser, "Laser", true, true, []),
        new(LightingEffect.CircleWave, "Circle wave", true, true, ["Counterclockwise", "Clockwise"]),
        new(LightingEffect.ColorfulCross, "Colorful cross", true, true, []),
        new(LightingEffect.SnowFall, "Snow fall", true, true, []),
        new(LightingEffect.Meteor, "Meteor", true, true, []),
        new(LightingEffect.CustomPattern, "Custom pattern", false, false, ["Pattern 1", "Pattern 2", "Pattern 3", "Pattern 4", "Pattern 5"]),
    ];

    public static LightingEffectInfo? Find(LightingEffect effect) => Effects.FirstOrDefault(e => e.Effect == effect);
}
