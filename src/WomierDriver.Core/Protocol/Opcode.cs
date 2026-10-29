namespace WomierDriver.Core.Protocol;

/// <summary>
/// Feature-report command bytes of the RY5088 keyboard family (byte 0 of every packet).
/// SET commands are below 0x80, GET commands are 0x80 and above.
/// Only the commands this driver uses are listed; see docs/protocol.md for the full table.
/// </summary>
public static class Opcode
{
    public const byte SetReset = 0x01;
    public const byte SetReportRate = 0x03;
    public const byte SetProfile = 0x04;
    public const byte SetKeyMatrix = 0x0A;
    public const byte SetFn = 0x10;

    /// <summary>
    /// Turns the firmware's live travel reports on (byte 1 = 1) or off (0). While on, the keyboard sends
    /// <see cref="VendorInputReports.Travel"/> input reports on its vendor input collection.
    /// </summary>
    public const byte SetMagnetismReport = 0x1B;

    // 0x1C (SET_MAGNETISM_CAL) and 0x1E (SET_MAGNETISM_MAXIMUM_CALIBRATION) re-learn the switch calibration and are
    // deliberately not defined here. See docs/protocol.md.

    public const byte SetMultiMagnetism = 0x65;

    public const byte GetRfVersion = 0x80;
    public const byte GetReportRate = 0x83;
    public const byte GetProfile = 0x84;
    public const byte GetKeyMatrix = 0x8A;
    public const byte GetUsbVersion = 0x8F;
    public const byte GetFn = 0x90;
    public const byte GetMultiMagnetism = 0xE5;

    /// <summary>Dongle-only query: returns the id of the keyboard paired over 2.4 GHz in bytes 1..2.</summary>
    public const byte DongleGetPairedId = 0xF1;
}
