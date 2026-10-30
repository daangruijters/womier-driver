using WomierDriver.Core.Device;
using WomierDriver.Core.Hid;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Profiles;
using WomierDriver.Core.Protocol;
using Xunit;

namespace WomierDriver.Tests;

public class ReceiverTests
{
    private static (FakeReceiver Receiver, ReceiverRelayChannel Relay) CreateRelay(TimeSpan? timeout = null)
    {
        var receiver = new FakeReceiver(new FakeRy5088Device());
        return (receiver, new ReceiverRelayChannel(receiver, NullLogger.Instance, timeout));
    }

    [Fact]
    public async Task Relay_FollowsTheVendorSequenceAndReturnsTheKeyboardsAnswer()
    {
        var (receiver, relay) = CreateRelay();

        var answer = await relay.ExchangeAsync(Ry5088Packets.GetUsbVersion());

        Assert.True(DeviceIdentity.TryParse(answer, out var identity));
        Assert.Equal(2518u, identity.DeviceId);
        var opcodes = receiver.Sent.Select(p => p[0]).ToList();
        Assert.Equal(Opcode.ReceiverGetStatus, opcodes[0]);              // is the keyboard connected?
        Assert.Equal(Opcode.GetUsbVersion, opcodes[1]);                  // the command itself, unchanged
        Assert.All(opcodes.Skip(2).SkipLast(1), o => Assert.Equal(Opcode.ReceiverGetStatus, o));
        Assert.Equal(Opcode.ReceiverFetchAnswer, opcodes[^1]);           // collect the answer
        Assert.Equal(0x70, receiver.Sent[1][7]);                         // keyboard commands keep their checksum
        Assert.Equal(0, receiver.Sent[0][7]);                            // receiver queries have none
        Assert.Equal(0, receiver.Sent[^1][7]);
    }

    [Fact]
    public async Task Relay_IdentifiesAndWritesAKeyboardBehindTheReceiver()
    {
        var (receiver, relay) = CreateRelay();

        var keyboard = await Sk75Keyboard.TryConnectAsync(relay, isDongle: true, NullLogger.Instance);
        Assert.NotNull(keyboard);
        Assert.Equal("2.4 GHz", keyboard.Info.ConnectionLabel);

        var profile = Profile.CreateUniform("Gaming", KeySettings.Gaming);
        var result = await keyboard.WriteKeySettingsAsync(profile.Keys);

        Assert.True(result.Success, result.Message);
        Assert.Equal(1, receiver.Keyboard.CommitCount);
        Assert.Equal(100, receiver.Keyboard.U16(MagnetismStream.Travel)[14]);
        Assert.Equal(28, receiver.Keyboard.SentPackets.Count(p => p[0] == Opcode.SetMultiMagnetism));
    }

    [Fact]
    public async Task Relay_SendsNothingToASleepingKeyboard()
    {
        var (receiver, relay) = CreateRelay();
        receiver.KeyboardConnected = false;

        await Assert.ThrowsAsync<KeyboardAsleepException>(() => relay.ExchangeAsync(Ry5088Packets.GetUsbVersion()));
        await Assert.ThrowsAsync<KeyboardAsleepException>(() => Sk75Keyboard.TryConnectAsync(relay, true, NullLogger.Instance));

        Assert.All(receiver.Sent, p => Assert.Equal(Opcode.ReceiverGetStatus, p[0]));
        Assert.Equal(0, receiver.Relayed);
    }

    [Fact]
    public async Task Relay_ReportsAKeyboardThatFallsAsleepMidway()
    {
        var (receiver, relay) = CreateRelay();
        receiver.AnswerDelayPolls = 5;
        receiver.SleepAfterPolls = 2;

        await Assert.ThrowsAsync<KeyboardAsleepException>(() => relay.ExchangeAsync(Ry5088Packets.GetUsbVersion()));
    }

    [Fact]
    public async Task Relay_TimesOutWhenTheAnswerNeverComes()
    {
        var (receiver, relay) = CreateRelay(TimeSpan.FromMilliseconds(50));
        receiver.DropAnswers = true;

        await Assert.ThrowsAsync<TimeoutException>(() => relay.ExchangeAsync(Ry5088Packets.GetUsbVersion()));
        Assert.Equal(ReceiverRelayChannel.MaxAttempts, receiver.Sent.Count(p => p[0] == Opcode.GetUsbVersion));
    }

    [Fact]
    public async Task Relay_SendsALostCommandAgain()
    {
        var (receiver, relay) = CreateRelay(TimeSpan.FromMilliseconds(50));
        receiver.LoseCommands = 2;

        var answer = await relay.ExchangeAsync(Ry5088Packets.GetUsbVersion());
        await relay.SendAsync(Ry5088Packets.SetMagnetismReport(true));

        Assert.Equal(Opcode.GetUsbVersion, answer[0]);
        Assert.Equal(3, receiver.Sent.Count(p => p[0] == Opcode.GetUsbVersion)); // lost twice, delivered the third time
        Assert.Equal(1, receiver.Keyboard.SentPackets.Count(p => p[0] == Opcode.GetUsbVersion));
        Assert.True(receiver.Keyboard.LiveReporting);
    }

    [Fact]
    public async Task Relay_WriteSurvivesLostChunks()
    {
        var (receiver, relay) = CreateRelay(TimeSpan.FromMilliseconds(50));
        var keyboard = await Sk75Keyboard.TryConnectAsync(relay, isDongle: true, NullLogger.Instance);
        receiver.LoseCommands = 3;

        var result = await keyboard!.WriteKeySettingsAsync(Profile.CreateUniform("Gaming", KeySettings.Gaming).Keys);

        Assert.True(result.Success, result.Message);
        Assert.Equal(1, receiver.Keyboard.CommitCount);
        Assert.Equal(100, receiver.Keyboard.U16(MagnetismStream.Travel)[14]);
    }

    [Fact]
    public async Task Relay_AcceptsAnAnswerWhileTheDeliveredFlagLags()
    {
        // Seen on the real receiver: answer ready, delivered flag still 0.
        var (receiver, relay) = CreateRelay(TimeSpan.FromMilliseconds(500));
        receiver.DeliveredFlagStuck = true;

        var answer = await relay.ExchangeAsync(Ry5088Packets.GetProfile());
        Assert.Equal(Opcode.GetProfile, answer[0]);

        await relay.SendAsync(Ry5088Packets.SetMagnetismReport(true));
        Assert.True(receiver.Keyboard.LiveReporting);
    }

    [Fact]
    public async Task Relay_StatusQueryNeverReachesTheKeyboard()
    {
        var (receiver, relay) = CreateRelay();
        receiver.Battery = 80;

        var status = await relay.GetStatusAsync();

        Assert.Equal(new ReceiverStatus(false, 80, true, true), status);
        Assert.Equal(0, receiver.Relayed);
        Assert.Empty(receiver.Keyboard.SentPackets);
    }

    [Theory]
    // Frames captured from the SK75 TMR's receiver (PID 0x5038) answering F7.
    [InlineData("00640000010101", false, 100, true, true)]   // keyboard connected, idle
    [InlineData("00640000010001", false, 100, true, false)]  // command on its way to the keyboard
    [InlineData("01640000010101", true, 100, true, true)]    // keyboard's answer has arrived
    [InlineData("00640001010101", false, 100, false, true)]  // keyboard asleep, last known battery
    [InlineData("00000001010101", false, 0, false, true)]    // keyboard on its cable, battery unknown
    public void Status_DecodesCapturedFrames(string hex, bool answerReady, int battery, bool connected, bool delivered)
    {
        Assert.True(ReceiverStatus.TryParse(Convert.FromHexString(hex), out var status));
        Assert.Equal(new ReceiverStatus(answerReady, battery, connected, delivered), status);
    }

    [Theory]
    [InlineData("F00208000000000F")] // the receiver's version answer
    [InlineData("8FD60900000000")]   // a keyboard answer
    [InlineData("006400")]           // too short
    public void Status_RejectsOtherBuffers(string hex)
    {
        Assert.False(ReceiverStatus.TryParse(Convert.FromHexString(hex), out _));
    }

    [Fact]
    public void StatusRequest_HasNoChecksum()
    {
        var packet = Ry5088Packets.Command(Opcode.ReceiverGetStatus);
        Checksum.Apply(packet, ChecksumKind.None);
        Assert.Equal(new byte[] { 0xF7, 0, 0, 0, 0, 0, 0, 0 }, packet[..8]); // as the vendor helper sends it
    }
}
