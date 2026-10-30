using WomierDriver.Core.Hid;
using WomierDriver.Core.Protocol;

namespace WomierDriver.Tests;

/// <summary>
/// In-memory stand-in for the SK75 TMR's 2.4 GHz receiver in front of a <see cref="FakeRy5088Device"/>, modelled on
/// captures of the real receiver: F7 returns a status frame, any keyboard command is relayed and answered after a few
/// status polls, FC loads the keyboard's answer into the buffer, and nothing is relayed while the keyboard sleeps.
/// </summary>
public sealed class FakeReceiver(FakeRy5088Device keyboard) : IFeatureChannel
{
    private byte[] _buffer = new byte[Ry5088Packets.ReportLength];
    private byte[]? _pending;
    private byte[]? _answer;
    private int _pollsLeft;
    private bool _answerReady;
    private bool _delivered = true;

    public FakeRy5088Device Keyboard { get; } = keyboard;

    public bool KeyboardConnected { get; set; } = true;

    public int Battery { get; set; } = 100;

    /// <summary>Status polls before the keyboard's answer is ready.</summary>
    public int AnswerDelayPolls { get; set; } = 2;

    /// <summary>The keyboard never answers (relay timeout).</summary>
    public bool DropAnswers { get; set; }

    /// <summary>Reproduces a capture: the answer is ready while the delivered flag stays 0.</summary>
    public bool DeliveredFlagStuck { get; set; }

    /// <summary>Puts the keyboard to sleep after this many more status polls (-1 = never).</summary>
    public int SleepAfterPolls { get; set; } = -1;

    /// <summary>The next this many keyboard commands are lost on the way: never delivered, never answered (seen on the real receiver).</summary>
    public int LoseCommands { get; set; }

    /// <summary>Every packet the host sent to the receiver, checksum applied.</summary>
    public List<byte[]> Sent { get; } = [];

    /// <summary>Keyboard commands that reached the keyboard.</summary>
    public int Relayed { get; private set; }

    public string DevicePath => "fake://sk75-receiver";

    public bool IsOpen { get; private set; } = true;

    public async Task SendAsync(ReadOnlyMemory<byte> request, ChecksumKind checksum = ChecksumKind.Bit7, CancellationToken cancellationToken = default)
    {
        var packet = new byte[Ry5088Packets.ReportLength];
        request.Span[..Math.Min(request.Length, packet.Length)].CopyTo(packet);
        Checksum.Apply(packet, checksum);
        Sent.Add(packet);
        switch (packet[0])
        {
            case Opcode.ReceiverGetStatus:
                await TickAsync().ConfigureAwait(false);
                _buffer = StatusFrame();
                break;
            case Opcode.ReceiverFetchAnswer:
                _buffer = _answer ?? new byte[Ry5088Packets.ReportLength];
                _answerReady = false;
                break;
            case Opcode.ReceiverGetVersion:
                _buffer = Ry5088Packets.Command(Opcode.ReceiverGetVersion, 0x02, 0x08);
                break;
            default:
                // A new command clears the previous answer; a sleeping keyboard never gets it.
                _answer = null;
                _answerReady = false;
                _delivered = false;
                _pending = KeyboardConnected ? packet : null;
                if (_pending is not null && LoseCommands > 0)
                {
                    LoseCommands--;
                    _pending = null;
                }

                _pollsLeft = AnswerDelayPolls;
                break;
        }
    }

    public async Task<byte[]> ExchangeAsync(ReadOnlyMemory<byte> request, ChecksumKind checksum = ChecksumKind.Bit7, CancellationToken cancellationToken = default)
    {
        await SendAsync(request, checksum, cancellationToken).ConfigureAwait(false);
        return _buffer.ToArray();
    }

    public void Dispose() => IsOpen = false;

    private async Task TickAsync()
    {
        if (SleepAfterPolls >= 0 && SleepAfterPolls-- == 0)
        {
            KeyboardConnected = false;
            _pending = null;
        }

        if (_pending is null || --_pollsLeft > 0)
        {
            return;
        }

        var packet = _pending;
        _pending = null;
        Relayed++;
        var answer = await Keyboard.ExchangeAsync(packet).ConfigureAwait(false);
        _delivered = !DeliveredFlagStuck;
        if (!DropAnswers)
        {
            _answer = answer;
            _answerReady = true;
        }
    }

    private byte[] StatusFrame()
    {
        var frame = new byte[Ry5088Packets.ReportLength];
        frame[0] = (byte)(_answerReady ? 1 : 0);
        frame[1] = (byte)Battery;
        frame[3] = (byte)(KeyboardConnected ? 0 : 1);
        frame[4] = 1;
        frame[5] = (byte)(_delivered ? 1 : 0);
        frame[6] = 1;
        return frame;
    }
}
