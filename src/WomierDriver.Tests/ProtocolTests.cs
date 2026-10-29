using WomierDriver.Core.Protocol;
using Xunit;

namespace WomierDriver.Tests;

public class ProtocolTests
{
    [Theory]
    [InlineData(0x84, 0x7B)] // GET_PROFILE request; the keyboard echoes "84 xx 00 00 00 00 00 7B"
    [InlineData(0x8F, 0x70)] // GET_USB_VERSION request
    [InlineData(0x80, 0x7F)] // GET_RF_VERSION; live answer was "80 00 00 00 00 00 00 7F"
    [InlineData(0xF1, 0x0E)] // unknown command; live answer was "F1 00 00 00 00 00 00 0E"
    public void Bit7Checksum_MatchesLiveCaptures(int opcode, int expectedChecksum)
    {
        var packet = Ry5088Packets.Command((byte)opcode);
        Checksum.Apply(packet, ChecksumKind.Bit7);
        Assert.Equal(expectedChecksum, packet[7]);
        Assert.True(Checksum.IsValid(packet, ChecksumKind.Bit7));
    }

    [Fact]
    public void Bit8Checksum_CoversEightBytes()
    {
        var packet = new byte[] { 0x07, 1, 4, 255, 7, 10, 20, 30, 0 };
        Checksum.Apply(packet, ChecksumKind.Bit8);
        Assert.Equal(0xFF, (0x07 + 1 + 4 + 255 + 7 + 10 + 20 + 30 + packet[8]) & 0xFF);
        Assert.True(Checksum.IsValid(packet, ChecksumKind.Bit8));
    }

    [Fact]
    public void DeviceIdentity_ParsesLiveUsbVersionResponse()
    {
        var response = new byte[64];
        new byte[] { 0x8F, 0xD6, 0x09, 0x00, 0x00, 0x00, 0x00, 0x08, 0x03 }.CopyTo(response, 0);
        Assert.True(DeviceIdentity.TryParse(response, out var identity));
        Assert.Equal(2518u, identity.DeviceId);
        Assert.Equal(776, identity.UsbVersion);
        Assert.Equal("3.08", identity.VersionLabel);
    }

    [Fact]
    public void DeviceIdentity_RejectsOtherOpcodes()
    {
        var response = new byte[64];
        response[0] = 0xF1;
        Assert.False(DeviceIdentity.TryParse(response, out _));
    }

    [Theory]
    [InlineData(null, null, 10)]
    [InlineData(500, null, 10)]
    [InlineData(776, 0, 100)]
    [InlineData(1024, null, 100)]
    [InlineData(1280, null, 200)]
    [InlineData(776, 1300, 200)]
    public void TravelScale_FollowsFirmwareVersionGates(int? usb, int? rf, int expectedUnitsPerMm)
    {
        Assert.Equal(expectedUnitsPerMm, TravelScale.ForFirmware(usb, rf).UnitsPerMm);
    }

    [Fact]
    public void TravelScale_RoundTripsMillimetres()
    {
        var scale = TravelScale.Centi;
        Assert.Equal(150, scale.ToRaw(1.5));
        Assert.Equal(1.5, scale.ToMm(150));
        Assert.Equal(0.01, scale.StepMm);
        Assert.Equal(1.23, scale.Quantize(1.234), 10);
    }

    [Fact]
    public void MagnetismStreams_HaveExpectedSizes()
    {
        Assert.Equal(256, MagnetismStream.Travel.PayloadLength());
        Assert.Equal(4, MagnetismStream.Travel.ReadChunkCount());
        Assert.Equal(128, MagnetismStream.KeyModes.PayloadLength());
        Assert.Equal(2, MagnetismStream.KeyModes.ReadChunkCount());
        Assert.Equal(1, MagnetismStream.TopDeadZoneTravel.ElementSize());
        Assert.Equal(8, MagnetismStream.DksData.ReadChunkCount());
    }

    [Fact]
    public void WriteMagnetismTable_SplitsIntoFiftySixByteChunksAndFlagsTheLastOne()
    {
        var values = new ushort[128];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = (ushort)(100 + i);
        }

        var payload = Ry5088Packets.EncodeU16Table(values);
        var packets = Ry5088Packets.WriteMagnetismTable(MagnetismStream.Travel, payload, commitAtEnd: true);

        Assert.Equal(5, packets.Count); // 256 bytes = 4 x 56 + 32
        for (var i = 0; i < packets.Count; i++)
        {
            Assert.Equal(64, packets[i].Length);
            Assert.Equal(Opcode.SetMultiMagnetism, packets[i][0]);
            Assert.Equal((byte)MagnetismStream.Travel, packets[i][1]);
            Assert.Equal(1, packets[i][2]);
            Assert.Equal(i, packets[i][3]);
            Assert.Equal(i == 4 ? 1 : 0, packets[i][4]);
        }

        // Byte 8 of chunk 1 is payload byte 56 = low byte of key 28 (value 128).
        Assert.Equal(128, packets[1][8]);
        Assert.Equal(0, packets[1][9]);

        var uncommitted = Ry5088Packets.WriteMagnetismTable(MagnetismStream.Travel, payload, commitAtEnd: false);
        Assert.All(uncommitted, p => Assert.Equal(0, p[4]));
    }

    [Fact]
    public void WriteMagnetismTable_RejectsWrongLength()
    {
        Assert.Throws<ArgumentException>(() => Ry5088Packets.WriteMagnetismTable(MagnetismStream.KeyModes, new byte[256], true));
    }

    [Fact]
    public void U16Table_RoundTrips()
    {
        var values = new ushort[128];
        values[0] = 0x0118;
        values[127] = 0xBEEF;
        var decoded = Ry5088Packets.DecodeU16Table(Ry5088Packets.EncodeU16Table(values));
        Assert.Equal(values, decoded);
    }

    [Fact]
    public void ReadMagnetismChunk_HasBulkFlagAndChunkIndex()
    {
        var packet = Ry5088Packets.ReadMagnetismChunk(MagnetismStream.LiftTravel, 3);
        Assert.Equal(new byte[] { 0xE5, 1, 1, 3 }, packet[..4]);
    }
}
