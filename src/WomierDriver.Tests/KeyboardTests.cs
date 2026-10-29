using WomierDriver.Core.Device;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Profiles;
using WomierDriver.Core.Protocol;
using Xunit;

namespace WomierDriver.Tests;

public class KeyboardTests
{
    [Fact]
    public async Task Connect_IdentifiesTheSk75AndPicksTheCentiScale()
    {
        var fake = new FakeRy5088Device();
        var keyboard = await Sk75Keyboard.TryConnectAsync(fake, isDongle: false, NullLogger.Instance);

        Assert.NotNull(keyboard);
        Assert.Equal(2518u, keyboard.Info.DeviceId);
        Assert.Equal(776, keyboard.Info.UsbVersion);
        Assert.Equal(100, keyboard.Info.Scale.UnitsPerMm);
        Assert.False(keyboard.Info.SupportsTopDeadZone);
        Assert.Equal((byte?)1, keyboard.Info.OnboardProfile);
        Assert.Equal("3.08", keyboard.Info.FirmwareLabel);
        Assert.Equal(3.3, keyboard.Limits.TravelMax);
    }

    [Fact]
    public async Task Connect_RejectsOtherBoardsAndSilentDongles()
    {
        var other = new FakeRy5088Device(deviceId: 2426);
        Assert.Null(await Sk75Keyboard.TryConnectAsync(other, false, NullLogger.Instance));

        var silent = new FakeRy5088Device { Silent = true };
        Assert.Null(await Sk75Keyboard.TryConnectAsync(silent, isDongle: true, NullLogger.Instance));
    }

    [Fact]
    public async Task Read_DecodesFactoryTables()
    {
        var fake = new FakeRy5088Device();
        var keyboard = await Sk75Keyboard.TryConnectAsync(fake, false, NullLogger.Instance);
        var snapshot = await keyboard!.ReadKeySettingsAsync();

        Assert.Equal(81, snapshot.Keys.Count);
        var a = snapshot.Keys[9];
        Assert.Equal(2.8, a.ActuationMm);
        Assert.Equal(2.8, a.ReleaseMm);
        Assert.Equal(0.3, a.DeadZoneMm);
        Assert.False(a.RapidTrigger);
        Assert.Equal(69, snapshot.RawSwitchCodes[9]);
    }

    [Fact]
    public async Task Write_PatchesOnlyPhysicalKeysCommitsOnceAndVerifies()
    {
        var fake = new FakeRy5088Device();
        var keyboard = await Sk75Keyboard.TryConnectAsync(fake, false, NullLogger.Instance);

        var profile = Profile.CreateUniform("Gaming", KeySettings.Gaming);
        profile.SetKey(14, KeySettings.Gaming with { ActuationMm = 0.5, RtPressMm = 0.1 }); // W

        var result = await keyboard!.WriteKeySettingsAsync(profile.Keys);

        Assert.True(result.Success, result.Message);
        Assert.Equal(81, result.KeysWritten);
        Assert.Equal(0, result.MismatchedKeys);
        Assert.Equal(1, fake.CommitCount);

        var travel = fake.U16(MagnetismStream.Travel);
        var modes = fake.U8(MagnetismStream.KeyModes);
        var firePress = fake.U16(MagnetismStream.FirePressTravel);
        Assert.Equal(50, travel[14]);
        Assert.Equal(100, travel[9]);
        Assert.Equal(280, travel[10]); // slot without a key keeps the firmware value
        Assert.Equal(0x80, modes[14]);
        Assert.Equal(0, modes[10]);
        Assert.Equal(10, firePress[14]);
        Assert.Equal(20, firePress[9]);

        // Six tables (no top dead zone on firmware 776): 3 + 5 x 5 = 28 bulk packets, only the last one commits.
        var bulk = fake.SentPackets.Where(p => p[0] == Opcode.SetMultiMagnetism).ToList();
        Assert.Equal(28, bulk.Count);
        Assert.Equal(1, bulk.Count(p => p[4] == 1));
        Assert.Equal((byte)MagnetismStream.KeyModes, bulk[0][1]);
        Assert.Equal((byte)MagnetismStream.DeadZoneTravel, bulk[^1][1]);
    }

    [Fact]
    public async Task Write_ReportsMismatchesInsteadOfLying()
    {
        var fake = new FakeRy5088Device { IgnoreWrites = true };
        var keyboard = await Sk75Keyboard.TryConnectAsync(fake, false, NullLogger.Instance);
        var result = await keyboard!.WriteKeySettingsAsync(Profile.CreateUniform("P", KeySettings.Gaming).Keys);
        Assert.False(result.Success);
        Assert.Equal(81, result.MismatchedKeys);
    }

    [Fact]
    public async Task Write_IncludesTopDeadZoneOnNewFirmware()
    {
        var fake = new FakeRy5088Device(usbVersion: 1300);
        var keyboard = await Sk75Keyboard.TryConnectAsync(fake, false, NullLogger.Instance);
        Assert.True(keyboard!.Info.SupportsTopDeadZone);
        Assert.Equal(200, keyboard.Info.Scale.UnitsPerMm);

        var result = await keyboard.WriteKeySettingsAsync(Profile.CreateUniform("P", KeySettings.VendorDefault with { TopDeadZoneMm = 0.25 }).Keys);
        Assert.True(result.Success, result.Message);
        Assert.Equal(50, fake.U8(MagnetismStream.TopDeadZoneTravel)[0]);
        Assert.Equal(400, fake.U16(MagnetismStream.Travel)[0]); // 2.0 mm at 200 units/mm
    }
}
