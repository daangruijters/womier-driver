namespace WomierDriver.Core.Protocol;

/// <summary>
/// Fields of the <see cref="Opcode.GetUsbVersion"/> response: the vendor's model id in bytes 1..4 and the
/// USB firmware version in bytes 7..8 (both little-endian). Verified live on an SK75 TMR:
/// <c>8F D6 09 00 00 00 00 08 03</c> decodes to id 2518 and version 776 (0x0308, shown as v3.08).
/// </summary>
public readonly record struct DeviceIdentity(uint DeviceId, int UsbVersion)
{
    public static bool TryParse(ReadOnlySpan<byte> response, out DeviceIdentity identity)
    {
        identity = default;
        if (response.Length < 9 || response[0] != Opcode.GetUsbVersion)
        {
            return false;
        }

        var id = (uint)(response[1] | (response[2] << 8) | (response[3] << 16) | (response[4] << 24));
        var version = response[7] | (response[8] << 8);
        identity = new DeviceIdentity(id, version);
        return true;
    }

    /// <summary>Human readable firmware version, e.g. 776 becomes "3.08".</summary>
    public string VersionLabel => $"{UsbVersion >> 8}.{UsbVersion & 0xFF:D2}";
}
