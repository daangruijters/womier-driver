namespace WomierDriver.Core.Device;

/// <summary>Logical groups used for quick multi-selection in the UI.</summary>
[Flags]
public enum KeyGroup
{
    None = 0,
    Letters = 1 << 0,
    Digits = 1 << 1,
    FunctionRow = 1 << 2,
    Modifiers = 1 << 3,
    Navigation = 1 << 4,
    Arrows = 1 << 5,
    Wasd = 1 << 6,
    Symbols = 1 << 7,
    Fn = 1 << 8,
}

/// <summary>
/// One physical key of the keyboard.
/// </summary>
/// <param name="Slot">Index in the firmware's 128-entry key matrix; this is what every per-key command addresses.</param>
/// <param name="HidUsage">USB HID keyboard usage of the factory keymap (0 for the Fn key).</param>
/// <param name="Code">W3C <c>KeyboardEvent.code</c> name, e.g. "KeyA" or "ShiftLeft".</param>
/// <param name="Label">Short legend shown on the key.</param>
/// <param name="X">Left edge in layout units (732 x 281 canvas, one key unit is 46).</param>
/// <param name="Y">Top edge in layout units.</param>
/// <param name="Width">Width in layout units.</param>
/// <param name="Height">Height in layout units.</param>
/// <param name="Groups">Quick-select groups this key belongs to.</param>
public sealed record KeyDefinition(
    int Slot,
    byte HidUsage,
    string Code,
    string Label,
    double X,
    double Y,
    double Width,
    double Height,
    KeyGroup Groups)
{
    public bool IsIn(KeyGroup group) => (Groups & group) != 0;
}
