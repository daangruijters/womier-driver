namespace WomierDriver.Core.Protocol;

/// <summary>
/// Decoders for the input reports the keyboard sends on its vendor input collection (usage page 0xFFFF, usage 1).
/// Byte 0 is the HID report id, byte 1 the report type. Besides device notifications the collection carries the
/// live travel reports that <see cref="Opcode.SetMagnetismReport"/> enables.
/// </summary>
public static class VendorInputReports
{
    /// <summary>Data bytes per report; the HID report id comes in front of them.</summary>
    public const int DataLength = 32;

    /// <summary>Live travel report: bytes 2..3 hold the key's travel (u16 LE, travel units), byte 4 its matrix slot.</summary>
    public const byte Travel = 0x1B;

    /// <summary>Busy notification: byte 2 is 1 when the firmware starts a long operation and 0 when it is done.</summary>
    public const byte Busy = 0x0F;

    public const byte ProfileChanged = 0x01;

    public const byte ReportRateChanged = 0x02;

    public const byte FnLayerChanged = 0x03;

    public const byte Reset = 0x0D;

    public const byte Sleep = 0x13;

    public const byte MagnetismModeChanged = 0x1D;

    /// <summary>
    /// Decodes a travel report: <paramref name="raw"/> is the travel in travel units, <paramref name="slot"/> the
    /// matrix slot of the key. Returns false for every other report type and for too-short buffers.
    /// </summary>
    public static bool TryParseTravel(ReadOnlySpan<byte> report, out ushort raw, out int slot)
    {
        raw = 0;
        slot = -1;
        if (report.Length < 5 || report[1] != Travel)
        {
            return false;
        }

        raw = (ushort)(report[2] | (report[3] << 8));
        slot = report[4];
        return true;
    }

    /// <summary>Short name of a report, for diagnostics.</summary>
    public static string Describe(ReadOnlySpan<byte> report)
    {
        if (report.Length < 2)
        {
            return "empty";
        }

        return report[1] switch
        {
            Travel => "travel",
            Busy => report.Length > 2 && report[2] == 1 ? "busy start" : "busy end",
            ProfileChanged => "profile changed",
            ReportRateChanged => "report rate changed",
            FnLayerChanged => "fn layer / os changed",
            >= 0x04 and <= 0x0B => "lighting changed",
            Reset => "reset",
            Sleep => "sleep",
            MagnetismModeChanged => "magnetism mode changed",
            _ => $"unknown 0x{report[1]:X2}",
        };
    }
}
