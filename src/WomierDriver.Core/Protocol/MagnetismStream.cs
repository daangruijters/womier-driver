namespace WomierDriver.Core.Protocol;

/// <summary>
/// Sub-index (byte 1) of the <see cref="Opcode.SetMultiMagnetism"/> / <see cref="Opcode.GetMultiMagnetism"/> commands.
/// Each value addresses one per-key parameter table with 128 entries (one per key-matrix slot).
/// </summary>
public enum MagnetismStream : byte
{
    /// <summary>u16 per key: actuation point ("press travel"), in travel units.</summary>
    Travel = 0,

    /// <summary>u16 per key: reset point ("release travel"), in travel units.</summary>
    LiftTravel = 1,

    /// <summary>u16 per key: rapid-trigger press sensitivity, used when the key's rapid-trigger flag is set.</summary>
    FirePressTravel = 2,

    /// <summary>u16 per key: rapid-trigger release sensitivity, used when the key's rapid-trigger flag is set.</summary>
    FireLiftTravel = 3,

    /// <summary>u16 per key: dynamic keystroke travel (not used by this driver).</summary>
    DynamicTravel = 4,

    /// <summary>u8 per key: mod-tap time in 10 ms units (not used by this driver).</summary>
    ModTapTime = 5,

    /// <summary>u16 per key: bottom dead zone, in travel units.</summary>
    DeadZoneTravel = 6,

    /// <summary>u8 per key: bit 7 = rapid trigger enabled, bits 0..6 = key mode (0 normal, 2 DKS, 3 mod-tap, 4/5 toggle, 7 snap).</summary>
    KeyModes = 7,

    /// <summary>4 x u8 per key: DKS stage actions (not used by this driver).</summary>
    TriggerModes = 8,

    /// <summary>u8 per key: matrix slot of the snap-tap partner key (not used by this driver).</summary>
    SnapBindKey = 9,

    /// <summary>Bulk DKS data (not used by this driver).</summary>
    DksData = 10,

    /// <summary>u8 per key: top dead zone, in travel units. Only on firmware 1024 and newer.</summary>
    TopDeadZoneTravel = 251,

    /// <summary>u8 per key: installed switch type code (read-only for this driver).</summary>
    SwitchBody = 252,

    /// <summary>
    /// u16 per key, read-only: the key's current travel in travel units (the "travel value" the vendor's calibration
    /// dialog polls). See docs/protocol.md for how live it is outside calibration.
    /// </summary>
    LiveTravel = 254,
}

public static class MagnetismStreams
{
    public const int SlotCount = 128;

    /// <summary>Flag in <see cref="MagnetismStream.KeyModes"/> that enables rapid trigger for a key.</summary>
    public const byte RapidTriggerFlag = 0x80;

    /// <summary>Mask for the mode part of <see cref="MagnetismStream.KeyModes"/>.</summary>
    public const byte ModeMask = 0x7F;

    /// <summary>Plain key mode: configurable actuation and reset point, optionally with rapid trigger.</summary>
    public const byte ModeNormal = 0;

    /// <summary>Bytes per key in the table.</summary>
    public static int ElementSize(this MagnetismStream stream) => stream switch
    {
        MagnetismStream.Travel or MagnetismStream.LiftTravel or MagnetismStream.FirePressTravel
            or MagnetismStream.FireLiftTravel or MagnetismStream.DynamicTravel or MagnetismStream.DeadZoneTravel
            or MagnetismStream.LiveTravel => 2,
        MagnetismStream.TriggerModes or MagnetismStream.DksData => 4,
        _ => 1,
    };

    /// <summary>Tables the firmware only reports; <see cref="Device.Sk75Keyboard.WriteTableAsync"/> refuses to write them.</summary>
    public static bool IsReadOnly(this MagnetismStream stream) => stream is MagnetismStream.SwitchBody or MagnetismStream.LiveTravel;

    /// <summary>Total payload length of the table (128 keys times the element size).</summary>
    public static int PayloadLength(this MagnetismStream stream) => SlotCount * stream.ElementSize();

    /// <summary>Number of 64-byte responses needed to read the whole table.</summary>
    public static int ReadChunkCount(this MagnetismStream stream) => (stream.PayloadLength() + 63) / 64;

    public static bool IsU16(this MagnetismStream stream) => stream.ElementSize() == 2;
}
