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

    /// <summary>Battery line for a keyboard on its receiver; <c>null</c> on the cable or when unknown.</summary>
    public static string? Battery(DriverEngine engine)
        => engine.BatteryPercent is { } percent && engine.UsesReceiver ? $"Battery {percent} %" : null;
}
