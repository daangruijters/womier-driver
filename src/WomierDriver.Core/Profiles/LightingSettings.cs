using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WomierDriver.Core.Profiles;

/// <summary>
/// Backlight effects of the RY5088 firmware (byte 1 of <c>SET_LEDPARAM</c>). The values are the firmware's; the names
/// follow the vendor software's English labels. Values the SK75 TMR does not offer (music and screen sync, which need
/// the vendor application to stream data) are not listed, but may still be read from a keyboard.
/// </summary>
public enum LightingEffect : byte
{
    Off = 0,
    AlwaysOn = 1,
    Breathing = 2,
    SpectrumCycle = 3,
    Wave = 4,
    Ripple = 5,
    Starlight = 6,
    Stream = 7,
    Shadow = 8,
    MountainWave = 9,
    SineWave = 10,
    ColorSpring = 11,
    FlowerWave = 12,
    CustomPattern = 13,
    Laser = 14,
    CircleWave = 15,
    ColorfulCross = 16,
    SnowFall = 17,
    Meteor = 18,
    SnowTrace = 19,
}

/// <summary>A 24-bit colour. Serialized as <c>"#RRGGBB"</c>.</summary>
[JsonConverter(typeof(RgbColorJsonConverter))]
public readonly record struct RgbColor(byte R, byte G, byte B)
{
    public static RgbColor White { get; } = new(0xFF, 0xFF, 0xFF);

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>Parses <c>#RRGGBB</c> or <c>RRGGBB</c>.</summary>
    public static bool TryParse(string? text, out RgbColor color)
    {
        color = default;
        var hex = text?.Trim().TrimStart('#');
        if (hex is not { Length: 6 } || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        color = new RgbColor((byte)(value >> 16), (byte)(value >> 8), (byte)value);
        return true;
    }
}

/// <summary>Serializes <see cref="RgbColor"/> as <c>"#RRGGBB"</c>.</summary>
public sealed class RgbColorJsonConverter : JsonConverter<RgbColor>
{
    public override RgbColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => RgbColor.TryParse(reader.GetString(), out var color) ? color : throw new JsonException("Expected a colour like \"#FF8000\".");

    public override void Write(Utf8JsonWriter writer, RgbColor value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}

/// <summary>
/// The keyboard's backlight setting: one effect for the whole keyboard with its brightness, speed, colour and
/// effect-specific option (a direction or pattern). Which fields an effect uses is described by
/// <see cref="Device.Sk75Lighting"/>.
/// </summary>
public sealed record LightingSettings
{
    /// <summary>Highest brightness and speed step (both run from 0).</summary>
    public const int MaxLevel = 4;

    public LightingEffect Effect { get; init; } = LightingEffect.AlwaysOn;

    /// <summary>0 (dark) .. <see cref="MaxLevel"/>.</summary>
    public int Brightness { get; init; } = MaxLevel;

    /// <summary>0 (slow) .. <see cref="MaxLevel"/> (fast).</summary>
    public int Speed { get; init; } = 2;

    public RgbColor Color { get; init; } = RgbColor.White;

    /// <summary>Cycle through all colours instead of using <see cref="Color"/>.</summary>
    public bool Rainbow { get; init; }

    /// <summary>Effect-specific choice, such as the direction of a wave or the custom pattern number (0-based).</summary>
    public int Option { get; init; }

    /// <summary>
    /// True when both settings look the same on the keyboard: fields the effect does not use (the speed of a static
    /// effect, the colour while cycling through all colours) are ignored.
    /// </summary>
    public bool LooksLike(LightingSettings other)
    {
        if (Effect != other.Effect)
        {
            return false;
        }

        if (Effect == LightingEffect.Off)
        {
            return true;
        }

        if (Brightness != other.Brightness)
        {
            return false;
        }

        if (Device.Sk75Lighting.Find(Effect) is not { } info)
        {
            return this == other;
        }

        return (!info.HasSpeed || Speed == other.Speed)
            && (info.Options.Count == 0 || Option == other.Option)
            && (!info.HasColor || (Rainbow == other.Rainbow && (Rainbow || Color == other.Color)));
    }

    /// <summary>Returns a copy with every value inside the range the firmware accepts.</summary>
    public LightingSettings Sanitized()
    {
        var options = Device.Sk75Lighting.Find(Effect)?.Options.Count ?? 0;
        return this with
        {
            Brightness = Math.Clamp(Brightness, 0, MaxLevel),
            Speed = Math.Clamp(Speed, 0, MaxLevel),
            Option = options == 0 ? 0 : Math.Clamp(Option, 0, options - 1),
        };
    }
}
