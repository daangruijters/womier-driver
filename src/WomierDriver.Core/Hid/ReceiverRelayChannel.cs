using System.Diagnostics;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Protocol;

namespace WomierDriver.Core.Hid;

/// <summary>
/// <see cref="IFeatureChannel"/> for a keyboard behind its 2.4 GHz receiver (docs/protocol.md §7). The receiver
/// forwards every command to the keyboard over the radio; this channel checks with the receiver that the keyboard is
/// connected, sends the command, waits until the receiver reports it delivered (and, for queries, answered) and then
/// collects the answer. Everything above it (identification, table reads and writes) is the same as over USB.
/// </summary>
public sealed class ReceiverRelayChannel : IFeatureChannel
{
    /// <summary>Delay between a request to the receiver and reading its buffer; the receiver answers its own queries at once.</summary>
    public const int ReceiverReadDelayMs = 2;

    private static readonly byte[] StatusRequest = Ry5088Packets.Command(Opcode.ReceiverGetStatus);
    private static readonly byte[] FetchRequest = Ry5088Packets.Command(Opcode.ReceiverFetchAnswer);

    /// <summary>Attempts per command. Now and then the receiver loses a command and never delivers it; sending it again fixes that.</summary>
    public const int MaxAttempts = 4;

    private readonly IFeatureChannel _receiver;
    private readonly ILogger _log;
    private readonly TimeSpan _attemptTimeout;

    /// <param name="attemptTimeout">
    /// How long one attempt may take before the command is sent again. A relayed command normally completes in
    /// 10–25 ms, the first one after a quiet spell in up to about 160 ms.
    /// </param>
    public ReceiverRelayChannel(IFeatureChannel receiver, ILogger log, TimeSpan? attemptTimeout = null)
    {
        _receiver = receiver;
        _log = log;
        _attemptTimeout = attemptTimeout ?? TimeSpan.FromMilliseconds(600);
    }

    public string DevicePath => _receiver.DevicePath;

    public bool IsOpen => _receiver.IsOpen;

    /// <summary>The most recent status the receiver reported.</summary>
    public ReceiverStatus? LastStatus { get; private set; }

    /// <summary>Logs every status change while waiting for the keyboard (diagnostics, e.g. <c>hidprobe --trace</c>).</summary>
    public bool Trace { get; set; }

    /// <summary>Asks the receiver (not the keyboard) whether the keyboard is connected and how its battery is doing.</summary>
    public async Task<ReceiverStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        // The vendor software sends this query without a checksum.
        var frame = await _receiver.ExchangeAsync(StatusRequest, ChecksumKind.None, cancellationToken).ConfigureAwait(false);
        if (!ReceiverStatus.TryParse(frame, out var status))
        {
            throw new IOException($"Unexpected receiver status frame {Convert.ToHexString(frame, 0, 8)}.");
        }

        LastStatus = status;
        return status;
    }

    public Task SendAsync(ReadOnlyMemory<byte> request, ChecksumKind checksum = ChecksumKind.Bit7, CancellationToken cancellationToken = default)
        // An answer (the firmware echoes SET commands) proves delivery too; the delivered flag alone sometimes lags behind.
        => RelayAsync(request, checksum, s => s.CommandDelivered || s.AnswerReady, "acknowledge", cancellationToken);

    public async Task<byte[]> ExchangeAsync(ReadOnlyMemory<byte> request, ChecksumKind checksum = ChecksumKind.Bit7, CancellationToken cancellationToken = default)
    {
        // Sending clears the receiver's answer flag, so it only comes up for the answer to this request. The delivered
        // flag is not required: it has been seen stuck at 0 with the answer already waiting.
        await RelayAsync(request, checksum, s => s.AnswerReady, "answer", cancellationToken).ConfigureAwait(false);
        return await FetchAnswerAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() => _receiver.Dispose();

    /// <summary>
    /// Sends <paramref name="request"/> and waits until <paramref name="done"/> holds, sending it again when an attempt
    /// stalls. Repeating is safe for everything this driver sends: queries and the travel-report toggle are idempotent,
    /// and a repeated table chunk writes the same bytes to the same place.
    /// </summary>
    private async Task RelayAsync(ReadOnlyMemory<byte> request, ChecksumKind checksum, Func<ReceiverStatus, bool> done, string what, CancellationToken cancellationToken)
    {
        await EnsureKeyboardConnectedAsync(cancellationToken).ConfigureAwait(false);
        for (var attempt = 1; ; attempt++)
        {
            await _receiver.SendAsync(request, checksum, cancellationToken).ConfigureAwait(false);
            if (await WaitForAsync(done, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var opcode = request.Span.Length > 0 ? request.Span[0] : (byte)0;
            if (attempt >= MaxAttempts)
            {
                _log.Warn($"Receiver: the keyboard did not {what} command 0x{opcode:X2} after {MaxAttempts} attempts.");
                throw new TimeoutException($"The keyboard did not {what} a command through the 2.4 GHz receiver.");
            }

            _log.Info($"Receiver: command 0x{opcode:X2} got no {what} within {_attemptTimeout.TotalMilliseconds:0} ms; sending it again.");
        }
    }

    private async Task EnsureKeyboardConnectedAsync(CancellationToken cancellationToken)
    {
        var status = await GetStatusAsync(cancellationToken).ConfigureAwait(false);
        if (!status.KeyboardConnected)
        {
            throw new KeyboardAsleepException();
        }
    }

    /// <summary>Polls the receiver until <paramref name="done"/> holds (true) or the attempt times out (false).</summary>
    private async Task<bool> WaitForAsync(Func<ReceiverStatus, bool> done, CancellationToken cancellationToken)
    {
        var clock = Stopwatch.StartNew();
        ReceiverStatus? previous = null;
        while (true)
        {
            var status = await GetStatusAsync(cancellationToken).ConfigureAwait(false);
            if (Trace && status != previous)
            {
                _log.Info($"receiver +{clock.ElapsedMilliseconds,4} ms  {status}");
                previous = status;
            }

            if (done(status))
            {
                return true;
            }

            if (!status.KeyboardConnected)
            {
                throw new KeyboardAsleepException("The keyboard went to sleep while a command was being relayed.");
            }

            if (clock.Elapsed > _attemptTimeout)
            {
                return false;
            }

            await Task.Delay(3, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Like the vendor software: FC (no checksum) makes the receiver's next GET_FEATURE return the keyboard's answer.</summary>
    private Task<byte[]> FetchAnswerAsync(CancellationToken cancellationToken)
        => _receiver.ExchangeAsync(FetchRequest, ChecksumKind.None, cancellationToken);
}
