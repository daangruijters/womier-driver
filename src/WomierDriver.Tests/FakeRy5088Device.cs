using WomierDriver.Core.Hid;
using WomierDriver.Core.Protocol;

namespace WomierDriver.Tests;

/// <summary>
/// In-memory stand-in for the keyboard's configuration interface. Implements just enough of the RY5088
/// protocol (identity queries, per-key table reads and bulk writes) to exercise the driver end to end.
/// Behaviour mirrors what was observed on a real SK75 TMR: reads return raw table data without an opcode echo.
/// </summary>
public sealed class FakeRy5088Device : IFeatureChannel
{
    private readonly Dictionary<MagnetismStream, byte[]> _tables = new();
    private readonly Dictionary<MagnetismStream, byte[]> _pending = new();
    private byte[] _lastResponse = new byte[Ry5088Packets.ReportLength];

    public FakeRy5088Device(uint deviceId = 2518, int usbVersion = 776)
    {
        DeviceId = deviceId;
        UsbVersion = usbVersion;
        foreach (var stream in new[]
                 {
                     MagnetismStream.KeyModes, MagnetismStream.Travel, MagnetismStream.LiftTravel, MagnetismStream.FirePressTravel,
                     MagnetismStream.FireLiftTravel, MagnetismStream.DeadZoneTravel, MagnetismStream.TopDeadZoneTravel, MagnetismStream.SwitchBody,
                 })
        {
            _tables[stream] = new byte[stream.PayloadLength()];
        }

        // Factory-like content: 2.80 mm press/lift, 0.30 mm dead zone, switch code 69 on every slot.
        Fill(MagnetismStream.Travel, 280);
        Fill(MagnetismStream.LiftTravel, 280);
        Fill(MagnetismStream.DeadZoneTravel, 30);
        Array.Fill(_tables[MagnetismStream.SwitchBody], (byte)69);
    }

    public uint DeviceId { get; }

    public int UsbVersion { get; }

    public int RfVersion { get; set; }

    public byte OnboardProfile { get; set; } = 1;

    public string DevicePath => "fake://sk75";

    public bool IsOpen { get; private set; } = true;

    public int CommitCount { get; private set; }

    public List<byte[]> SentPackets { get; } = [];

    /// <summary>When set, every request is answered with an all-zero buffer (simulates a dongle without a paired keyboard).</summary>
    public bool Silent { get; set; }

    /// <summary>When set, writes are acknowledged but the tables never change (simulates a firmware that rejects the data).</summary>
    public bool IgnoreWrites { get; set; }

    public ushort[] U16(MagnetismStream stream) => Ry5088Packets.DecodeU16Table(_tables[stream]);

    public byte[] U8(MagnetismStream stream) => _tables[stream];

    public void Fill(MagnetismStream stream, ushort value)
    {
        var values = new ushort[MagnetismStreams.SlotCount];
        Array.Fill(values, value);
        _tables[stream] = Ry5088Packets.EncodeU16Table(values);
    }

    public Task SendAsync(ReadOnlyMemory<byte> request, ChecksumKind checksum = ChecksumKind.Bit7, CancellationToken cancellationToken = default)
    {
        var packet = request.ToArray();
        Checksum.Apply(packet, checksum);
        if (!Checksum.IsValid(packet, checksum))
        {
            throw new InvalidOperationException("checksum self-test failed");
        }

        SentPackets.Add(packet);
        _lastResponse = Handle(packet);
        return Task.CompletedTask;
    }

    public async Task<byte[]> ExchangeAsync(ReadOnlyMemory<byte> request, ChecksumKind checksum = ChecksumKind.Bit7, CancellationToken cancellationToken = default)
    {
        await SendAsync(request, checksum, cancellationToken);
        return _lastResponse;
    }

    public void Dispose() => IsOpen = false;

    private byte[] Handle(byte[] packet)
    {
        var response = new byte[Ry5088Packets.ReportLength];
        if (Silent)
        {
            return response;
        }

        switch (packet[0])
        {
            case Opcode.GetUsbVersion:
                response[0] = Opcode.GetUsbVersion;
                response[1] = (byte)(DeviceId & 0xFF);
                response[2] = (byte)((DeviceId >> 8) & 0xFF);
                response[3] = (byte)((DeviceId >> 16) & 0xFF);
                response[4] = (byte)((DeviceId >> 24) & 0xFF);
                response[7] = (byte)(UsbVersion & 0xFF);
                response[8] = (byte)(UsbVersion >> 8);
                return response;
            case Opcode.GetRfVersion:
                response[0] = Opcode.GetRfVersion;
                response[1] = (byte)(RfVersion & 0xFF);
                response[2] = (byte)(RfVersion >> 8);
                return response;
            case Opcode.GetProfile:
                response[0] = Opcode.GetProfile;
                response[1] = OnboardProfile;
                return response;
            case Opcode.GetMultiMagnetism:
            {
                var stream = (MagnetismStream)packet[1];
                var chunk = packet[3];
                if (_tables.TryGetValue(stream, out var table))
                {
                    var offset = chunk * Ry5088Packets.ReportLength;
                    var length = Math.Clamp(table.Length - offset, 0, Ry5088Packets.ReportLength);
                    if (length > 0)
                    {
                        Array.Copy(table, offset, response, 0, length);
                    }
                }

                return response;
            }

            case Opcode.SetMultiMagnetism:
            {
                if (IgnoreWrites)
                {
                    Array.Copy(packet, response, 8);
                    return response;
                }

                var stream = (MagnetismStream)packet[1];
                if (packet[2] == 1)
                {
                    if (!_pending.TryGetValue(stream, out var staging))
                    {
                        staging = new byte[stream.PayloadLength()];
                        _pending[stream] = staging;
                    }

                    var offset = packet[3] * Ry5088Packets.ChunkDataLength;
                    var length = Math.Min(Ry5088Packets.ChunkDataLength, staging.Length - offset);
                    Array.Copy(packet, Ry5088Packets.ChunkDataOffset, staging, offset, length);
                }
                else
                {
                    var table = _tables[stream];
                    var size = stream.ElementSize();
                    Array.Copy(packet, Ry5088Packets.ChunkDataOffset, table, packet[3] * size, size);
                }

                if (packet[4] == 1)
                {
                    foreach (var (s, data) in _pending)
                    {
                        _tables[s] = data;
                    }

                    _pending.Clear();
                    CommitCount++;
                }

                // Echo the header like the real firmware does for SET commands.
                Array.Copy(packet, response, 8);
                return response;
            }

            default:
                Array.Copy(packet, response, 8);
                return response;
        }
    }
}
