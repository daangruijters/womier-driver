using WomierDriver.Core.Automation;

namespace WomierDriver.App.Services;

/// <summary>How the window and the tray describe the keyboard connection, so both say the same thing.</summary>
internal static class ConnectionDescription
{
    public static string Describe(DriverEngine engine) => engine.Keyboard switch
    {
        null when engine.IsKeyboardAsleep => "Asleep · 2.4 GHz · press a key",
        null => "Keyboard not connected",
        { } keyboard when engine.IsKeyboardAsleep => $"Asleep · {keyboard.ConnectionLabel} · press a key",
        { } keyboard => $"Connected · {keyboard.ConnectionLabel} · firmware {keyboard.FirmwareLabel}",
    };

    /// <summary>Battery level of a keyboard on its receiver; <c>null</c> on the cable or when unknown.</summary>
    public static int? BatteryPercent(DriverEngine engine) => engine.UsesReceiver ? engine.BatteryPercent : null;

    /// <summary>Battery line for a keyboard on its receiver ("Battery 15 % · low"); <c>null</c> on the cable or when unknown.</summary>
    public static string? Battery(DriverEngine engine) => BatteryPercent(engine) is { } percent
        ? BatteryAlerts.Classify(percent) switch
        {
            BatteryAlert.Critical => $"Battery {percent} % · almost empty",
            BatteryAlert.Low => $"Battery {percent} % · low",
            _ => $"Battery {percent} %",
        }
        : null;

    /// <summary>Segoe Fluent Icons battery glyph (Battery0 … Battery10) for a level in percent.</summary>
    public static string BatteryGlyph(int percent)
    {
        var tenths = Math.Clamp((percent + 5) / 10, 0, 10);
        return tenths == 10 ? "" : ((char)(0xE850 + tenths)).ToString();
    }
}
