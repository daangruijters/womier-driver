namespace WomierDriver.Core.Protocol;

/// <summary>
/// Builds the 64-byte feature-report payloads of the RY5088 protocol.
/// Packets are returned without a checksum; the channel applies it right before sending.
/// </summary>
public static class Ry5088Packets
{
    /// <summary>Length of a feature report payload (excluding the HID report id).</summary>
    public const int ReportLength = 64;

    /// <summary>Length of the data part of a bulk chunk (a chunk is an 8-byte header followed by 56 data bytes).</summary>
    public const int ChunkDataLength = 56;

    /// <summary>Offset of the data part inside a bulk chunk.</summary>
    public const int ChunkDataOffset = 8;

    public static byte[] Command(byte opcode, params byte[] args)
    {
        if (args.Length > ReportLength - 1)
        {
            throw new ArgumentException("Too many arguments for one packet.", nameof(args));
        }

        var packet = new byte[ReportLength];
        packet[0] = opcode;
        args.CopyTo(packet, 1);
        return packet;
    }

    public static byte[] GetUsbVersion() => Command(Opcode.GetUsbVersion);

    public static byte[] GetRfVersion() => Command(Opcode.GetRfVersion);

    public static byte[] GetProfile() => Command(Opcode.GetProfile);

    public static byte[] SetProfile(byte index) => Command(Opcode.SetProfile, index);

    public static byte[] GetReportRate() => Command(Opcode.GetReportRate);

    public static byte[] DongleGetPairedId() => Command(Opcode.DongleGetPairedId);

    /// <summary>Request one 64-byte chunk of a per-key table.</summary>
    public static byte[] ReadMagnetismChunk(MagnetismStream stream, int chunkIndex)
        => Command(Opcode.GetMultiMagnetism, (byte)stream, 1, checked((byte)chunkIndex));

    /// <summary>
    /// Splits a complete per-key table into bulk write packets.
    /// When <paramref name="commitAtEnd"/> is set, the last packet carries the "last chunk" flag that
    /// makes the firmware apply and persist everything written since the previous commit.
    /// </summary>
    public static IReadOnlyList<byte[]> WriteMagnetismTable(MagnetismStream stream, ReadOnlySpan<byte> payload, bool commitAtEnd)
    {
        var expected = stream.PayloadLength();
        if (payload.Length != expected)
        {
            throw new ArgumentException($"Table for {stream} must be {expected} bytes, got {payload.Length}.", nameof(payload));
        }

        var chunkCount = (payload.Length + ChunkDataLength - 1) / ChunkDataLength;
        var packets = new List<byte[]>(chunkCount);
        for (var chunk = 0; chunk < chunkCount; chunk++)
        {
            var packet = new byte[ReportLength];
            packet[0] = Opcode.SetMultiMagnetism;
            packet[1] = (byte)stream;
            packet[2] = 1; // bulk mode
            packet[3] = (byte)chunk;
            packet[4] = (byte)(commitAtEnd && chunk == chunkCount - 1 ? 1 : 0);
            var start = chunk * ChunkDataLength;
            var length = Math.Min(ChunkDataLength, payload.Length - start);
            payload.Slice(start, length).CopyTo(packet.AsSpan(ChunkDataOffset));
            packets.Add(packet);
        }

        return packets;
    }

    /// <summary>Writes one key's entry of a per-key table.</summary>
    public static byte[] WriteMagnetismKey(MagnetismStream stream, int slot, ReadOnlySpan<byte> value, bool commit)
    {
        if (slot is < 0 or >= MagnetismStreams.SlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(slot));
        }

        var packet = new byte[ReportLength];
        packet[0] = Opcode.SetMultiMagnetism;
        packet[1] = (byte)stream;
        packet[2] = 0; // single key mode
        packet[3] = (byte)slot;
        packet[4] = (byte)(commit ? 1 : 0);
        value.CopyTo(packet.AsSpan(ChunkDataOffset));
        return packet;
    }

    /// <summary>Encodes a u16 table as little-endian bytes.</summary>
    public static byte[] EncodeU16Table(ReadOnlySpan<ushort> values)
    {
        var bytes = new byte[values.Length * 2];
        for (var i = 0; i < values.Length; i++)
        {
            bytes[i * 2] = (byte)(values[i] & 0xFF);
            bytes[i * 2 + 1] = (byte)(values[i] >> 8);
        }

        return bytes;
    }

    public static ushort[] DecodeU16Table(ReadOnlySpan<byte> bytes, int count = MagnetismStreams.SlotCount)
    {
        var values = new ushort[count];
        for (var i = 0; i < count; i++)
        {
            values[i] = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
        }

        return values;
    }
}
