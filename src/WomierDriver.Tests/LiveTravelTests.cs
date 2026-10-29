using WomierDriver.Core.Automation;
using WomierDriver.Core.Device;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Profiles;
using WomierDriver.Core.Protocol;
using Xunit;

namespace WomierDriver.Tests;

public class LiveTravelTests
{
    [Theory]
    [InlineData(true, 0xE3)]  // vendor: 1B 01 00 00 00 00 00 E3
    [InlineData(false, 0xE4)] // vendor: 1B 00 00 00 00 00 00 E4
    public void SetMagnetismReport_MatchesVendorPackets(bool enabled, int expectedChecksum)
    {
        var packet = Ry5088Packets.SetMagnetismReport(enabled);
        Checksum.Apply(packet, ChecksumKind.Bit7);

        Assert.Equal(Opcode.SetMagnetismReport, packet[0]);
        Assert.Equal(enabled ? 1 : 0, packet[1]);
        Assert.Equal(expectedChecksum, packet[7]);
    }

    [Fact]
    public void LiveTravelTable_IsFourChunksOfU16AndReadOnly()
    {
        Assert.Equal(2, MagnetismStream.LiveTravel.ElementSize());
        Assert.Equal(256, MagnetismStream.LiveTravel.PayloadLength());
        Assert.Equal(4, MagnetismStream.LiveTravel.ReadChunkCount());
        Assert.True(MagnetismStream.LiveTravel.IsReadOnly());
        Assert.True(MagnetismStream.SwitchBody.IsReadOnly());
        Assert.False(MagnetismStream.Travel.IsReadOnly());

        var packet = Ry5088Packets.ReadMagnetismChunk(MagnetismStream.LiveTravel, 0);
        Checksum.Apply(packet, ChecksumKind.Bit7);
        Assert.Equal(new byte[] { 0xE5, 0xFE, 0x01, 0x00, 0, 0, 0, 0x1B }, packet[..8]); // vendor: E5 FE 01 00 00 00 00 1B
    }

    [Fact]
    public void TravelReport_DecodesValueAndSlotAndRejectsNotifications()
    {
        // Captured on the keyboard: 05 1B 2C 01 0E 00 ... = W (slot 14) at 3.00 mm.
        Assert.True(VendorInputReports.TryParseTravel([0x05, 0x1B, 0x2C, 0x01, 0x0E, 0, 0], out var raw, out var slot));
        Assert.Equal(300, raw);
        Assert.Equal(14, slot);
        Assert.Equal(3.0, TravelScale.Centi.ToMm(raw));

        Assert.False(VendorInputReports.TryParseTravel([0x05, 0x0F, 0x01, 0x00, 0x00], out _, out _));
        Assert.False(VendorInputReports.TryParseTravel([0x05, 0x1B, 0x2C, 0x01], out _, out _)); // too short for the slot
        Assert.Equal("busy start", VendorInputReports.Describe([0x05, 0x0F, 0x01]));
        Assert.Equal("travel", VendorInputReports.Describe([0x05, 0x1B, 0x00, 0x00, 0x00]));
    }

    [Fact]
    public async Task Keyboard_TogglesLiveReportingAndReadsTable254()
    {
        var fake = new FakeRy5088Device();
        var keyboard = await Sk75Keyboard.TryConnectAsync(fake, false, NullLogger.Instance);

        await keyboard!.SetLiveReportingAsync(true);
        Assert.True(fake.LiveReporting);

        fake.SetU16(MagnetismStream.LiveTravel, 14, 120); // W pressed 1.20 mm
        var travel = await keyboard.ReadLiveTravelAsync();
        Assert.Equal(MagnetismStreams.SlotCount, travel.Length);
        Assert.Equal(120, travel[14]);
        Assert.Equal(0, travel[13]);

        await keyboard.SetLiveReportingAsync(false);
        Assert.False(fake.LiveReporting);
    }

    [Fact]
    public async Task Keyboard_RefusesToWriteReadOnlyTables()
    {
        var fake = new FakeRy5088Device();
        var keyboard = await Sk75Keyboard.TryConnectAsync(fake, false, NullLogger.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => keyboard!.WriteTableAsync(MagnetismStream.LiveTravel, new byte[256]));
        await Assert.ThrowsAsync<ArgumentException>(() => keyboard!.WriteTableAsync(MagnetismStream.SwitchBody, new byte[128]));
        Assert.Equal(0, fake.CommitCount);
    }

    [Fact]
    public async Task ProfileWrite_NeverTouchesCalibrationOrLiveTables()
    {
        var fake = new FakeRy5088Device();
        var keyboard = await Sk75Keyboard.TryConnectAsync(fake, false, NullLogger.Instance);
        var profile = Profile.CreateUniform("Gaming", KeySettings.Gaming);

        var result = await keyboard!.WriteKeySettingsAsync(profile.Keys);

        Assert.True(result.Success);
        Assert.DoesNotContain(fake.SentPackets, p => p[0] is 0x1C or 0x1E);
        Assert.DoesNotContain(fake.SentPackets, p => p[0] is Opcode.SetMultiMagnetism or Opcode.GetMultiMagnetism && p[1] == (byte)MagnetismStream.LiveTravel);
    }

    [Fact]
    public void ReportSource_RaisesSamplesForPhysicalKeysOnly()
    {
        var channel = new FakeInputReportChannel();
        using var source = new ReportTravelSource(channel, TravelScale.Centi);
        var samples = new List<LiveTravelSample>();
        source.Sample += samples.Add;

        source.Start();
        Assert.True(channel.Started);

        channel.Push(0x05, 0x1B, 0x2C, 0x01, 0x0E, 0, 0); // W at 3.00 mm
        channel.Push(0x05, 0x0F, 0x01, 0x00, 0x00);       // busy notification
        channel.Push(0x05, 0x1B, 0x10, 0x00, 0x0A, 0, 0); // slot 10 has no key
        channel.Push(0x05, 0x1B, 0x00, 0x00, 0x0E, 0, 0); // W released

        Assert.Equal(2, samples.Count);
        Assert.Equal(new LiveTravelSample(14, 300, 3.0), samples[0]);
        Assert.Equal(new LiveTravelSample(14, 0, 0.0), samples[1]);
    }

    [Fact]
    public void ReportSource_ForwardsStopsAndDisposesItsChannel()
    {
        var channel = new FakeInputReportChannel();
        var source = new ReportTravelSource(channel, TravelScale.Centi);
        var stops = new List<Exception?>();
        source.Stopped += stops.Add;

        channel.Fail(new IOException("gone"));
        Assert.Single(stops);
        Assert.IsType<IOException>(stops[0]);

        source.Dispose();
        Assert.True(channel.IsDisposed);
        Assert.Equal(2, stops.Count);
        Assert.Null(stops[1]);

        source.Dispose(); // idempotent
        Assert.Equal(2, stops.Count);
    }
}
