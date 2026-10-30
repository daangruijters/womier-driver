namespace WomierDriver.Core.Protocol;

/// <summary>
/// The 2.4 GHz receiver's answer to <see cref="Opcode.ReceiverGetStatus"/> (docs/protocol.md §7). Unlike keyboard
/// answers it does not echo the opcode:
/// <code>
/// byte 0  1 once the keyboard's answer to the last relayed command has arrived
/// byte 1  keyboard battery level in percent (last known value while the keyboard sleeps)
/// byte 3  0 while the keyboard is connected over 2.4 GHz, 1 while it sleeps, is out of range or uses its cable
/// byte 5  0 while a relayed command is still on its way to the keyboard, 1 once it has been delivered
/// </code>
/// Bytes 2, 4 and 6 have not been seen changing (0, 1 and 1).
/// </summary>
public readonly record struct ReceiverStatus(bool AnswerReady, int BatteryPercent, bool KeyboardConnected, bool CommandDelivered)
{
    /// <summary>Bytes of the status frame that carry information.</summary>
    public const int Length = 7;

    /// <summary>Decodes a status frame. Returns false for buffers that cannot be one (too short, or an echoed opcode in byte 0).</summary>
    public static bool TryParse(ReadOnlySpan<byte> frame, out ReceiverStatus status)
    {
        status = default;
        if (frame.Length < Length || frame[0] > 1 || frame[3] > 1 || frame[5] > 1)
        {
            return false;
        }

        status = new ReceiverStatus(frame[0] == 1, frame[1], frame[3] == 0, frame[5] == 1);
        return true;
    }
}
