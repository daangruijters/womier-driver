namespace WomierDriver.Core.Protocol;

/// <summary>Checksum placement used by RY5088 feature reports.</summary>
public enum ChecksumKind
{
    /// <summary>No checksum byte.</summary>
    None,

    /// <summary>Byte 7 holds the one's complement of the sum of bytes 0..6. Used by almost every command.</summary>
    Bit7,

    /// <summary>Byte 8 holds the one's complement of the sum of bytes 0..7. Used by the lighting commands only.</summary>
    Bit8,
}

/// <summary>Implements the simple byte-sum checksum of the RY5088 protocol.</summary>
public static class Checksum
{
    /// <summary>Writes the checksum byte into <paramref name="packet"/> for the given scheme.</summary>
    public static void Apply(Span<byte> packet, ChecksumKind kind)
    {
        switch (kind)
        {
            case ChecksumKind.Bit7:
                packet[7] = Compute(packet[..7]);
                break;
            case ChecksumKind.Bit8:
                packet[8] = Compute(packet[..8]);
                break;
        }
    }

    /// <summary>Returns <c>0xFF - (sum(bytes) &amp; 0xFF)</c>, so that the covered bytes plus the checksum sum to 0xFF.</summary>
    public static byte Compute(ReadOnlySpan<byte> bytes)
    {
        var sum = 0;
        foreach (var b in bytes)
        {
            sum += b;
        }

        return (byte)(0xFF - (sum & 0xFF));
    }

    /// <summary>Validates the checksum of a packet that was built with <see cref="Apply"/>.</summary>
    public static bool IsValid(ReadOnlySpan<byte> packet, ChecksumKind kind)
    {
        var covered = kind switch
        {
            ChecksumKind.Bit7 => 8,
            ChecksumKind.Bit8 => 9,
            _ => 0,
        };
        if (covered == 0)
        {
            return true;
        }

        var sum = 0;
        for (var i = 0; i < covered; i++)
        {
            sum += packet[i];
        }

        return (sum & 0xFF) == 0xFF;
    }
}
