using System.Collections.Frozen;

namespace WomierDriver.Core.Device;

/// <summary>
/// PS/2 scan code set 1 make codes, as Windows Raw Input delivers them, for the USB HID keyboard usages of the
/// SK75 TMR's factory keymap. Extended keys carry the E0 prefix flag. Fn (usage 0) has no scan code because it
/// never reaches the operating system.
/// </summary>
public static class ScanCodeMap
{
    private static readonly (byte Usage, ushort MakeCode, bool Extended)[] Entries =
    [
        // Letters A..Z (usages 0x04..0x1D)
        (0x04, 0x1E, false), (0x05, 0x30, false), (0x06, 0x2E, false), (0x07, 0x20, false), (0x08, 0x12, false),
        (0x09, 0x21, false), (0x0A, 0x22, false), (0x0B, 0x23, false), (0x0C, 0x17, false), (0x0D, 0x24, false),
        (0x0E, 0x25, false), (0x0F, 0x26, false), (0x10, 0x32, false), (0x11, 0x31, false), (0x12, 0x18, false),
        (0x13, 0x19, false), (0x14, 0x10, false), (0x15, 0x13, false), (0x16, 0x1F, false), (0x17, 0x14, false),
        (0x18, 0x16, false), (0x19, 0x2F, false), (0x1A, 0x11, false), (0x1B, 0x2D, false), (0x1C, 0x15, false),
        (0x1D, 0x2C, false),
        // Digits 1..9, 0
        (0x1E, 0x02, false), (0x1F, 0x03, false), (0x20, 0x04, false), (0x21, 0x05, false), (0x22, 0x06, false),
        (0x23, 0x07, false), (0x24, 0x08, false), (0x25, 0x09, false), (0x26, 0x0A, false), (0x27, 0x0B, false),
        // Enter, Escape, Backspace, Tab, Space
        (0x28, 0x1C, false), (0x29, 0x01, false), (0x2A, 0x0E, false), (0x2B, 0x0F, false), (0x2C, 0x39, false),
        // - = [ ] \ ; ' ` , . /
        (0x2D, 0x0C, false), (0x2E, 0x0D, false), (0x2F, 0x1A, false), (0x30, 0x1B, false), (0x31, 0x2B, false),
        (0x33, 0x27, false), (0x34, 0x28, false), (0x35, 0x29, false), (0x36, 0x33, false), (0x37, 0x34, false),
        (0x38, 0x35, false),
        // Caps Lock, F1..F12
        (0x39, 0x3A, false), (0x3A, 0x3B, false), (0x3B, 0x3C, false), (0x3C, 0x3D, false), (0x3D, 0x3E, false),
        (0x3E, 0x3F, false), (0x3F, 0x40, false), (0x40, 0x41, false), (0x41, 0x42, false), (0x42, 0x43, false),
        (0x43, 0x44, false), (0x44, 0x57, false), (0x45, 0x58, false),
        // Insert, Home, Page Up, Delete, End, Page Down, arrows (all extended)
        (0x49, 0x52, true), (0x4A, 0x47, true), (0x4B, 0x49, true), (0x4C, 0x53, true), (0x4D, 0x4F, true),
        (0x4E, 0x51, true), (0x4F, 0x4D, true), (0x50, 0x4B, true), (0x51, 0x50, true), (0x52, 0x48, true),
        // Modifiers: left Ctrl / Shift / Alt / GUI, right Ctrl / Shift / Alt / GUI
        (0xE0, 0x1D, false), (0xE1, 0x2A, false), (0xE2, 0x38, false), (0xE3, 0x5B, true),
        (0xE4, 0x1D, true), (0xE5, 0x36, false), (0xE6, 0x38, true), (0xE7, 0x5C, true),
    ];

    private static readonly FrozenDictionary<byte, (ushort MakeCode, bool Extended)> ByUsage =
        Entries.ToFrozenDictionary(e => e.Usage, e => (e.MakeCode, e.Extended));

    private static readonly FrozenDictionary<(ushort MakeCode, bool Extended), byte> ByScanCode =
        Entries.ToFrozenDictionary(e => (e.MakeCode, e.Extended), e => e.Usage);

    public static bool TryGetScanCode(byte hidUsage, out ushort makeCode, out bool extended)
    {
        if (ByUsage.TryGetValue(hidUsage, out var entry))
        {
            (makeCode, extended) = entry;
            return true;
        }

        makeCode = 0;
        extended = false;
        return false;
    }

    public static bool TryGetHidUsage(ushort makeCode, bool extended, out byte hidUsage)
        => ByScanCode.TryGetValue((makeCode, extended), out hidUsage);

    /// <summary>Maps a Raw Input make code to the SK75 key that sends that usage in the factory keymap.</summary>
    public static bool TryGetSlot(ushort makeCode, bool extended, out int slot)
    {
        slot = -1;
        if (!TryGetHidUsage(makeCode, extended, out var usage) || Sk75TmrLayout.ByHidUsage(usage) is not { } key)
        {
            return false;
        }

        slot = key.Slot;
        return true;
    }
}
