using System.Diagnostics;
using WomierDriver.Core.Hid;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Profiles;
using WomierDriver.Core.Protocol;

namespace WomierDriver.Core.Device;

/// <summary>What the keyboard currently holds, decoded into millimetres for every physical key.</summary>
public sealed record KeyboardSnapshot(
    IReadOnlyDictionary<int, KeySettings> Keys,
    IReadOnlyList<byte> RawKeyModes,
    IReadOnlyList<byte> RawSwitchCodes);

/// <summary>Outcome of writing a set of key settings to the keyboard.</summary>
public sealed record ApplyResult(bool Success, string Message, int KeysWritten, int MismatchedKeys, TimeSpan Duration)
{
    public static ApplyResult Failed(string message) => new(false, message, 0, 0, TimeSpan.Zero);
}

/// <summary>
/// High-level access to one connected SK75 TMR: identification plus reading and writing the per-key
/// analog settings (actuation, reset point, rapid trigger, dead zone).
/// </summary>
public sealed class Sk75Keyboard : IDisposable
{
    private const int MaxIdentifyAttempts = 3;

    private readonly IFeatureChannel _channel;
    private readonly ILogger _log;

    private Sk75Keyboard(IFeatureChannel channel, KeyboardInfo info, ILogger log)
    {
        _channel = channel;
        _log = log;
        Info = info;
        Limits = KeyLimits.ForFirmware(info.UsbVersion, info.Scale);
    }

    public KeyboardInfo Info { get; }

    /// <summary>Valid value ranges for this firmware.</summary>
    public KeyLimits Limits { get; }

    /// <summary>
    /// Identifies the device behind <paramref name="channel"/>. Returns <c>null</c> (and leaves the channel open)
    /// when nothing answers or when the answering board is not an SK75 TMR.
    /// </summary>
    public static async Task<Sk75Keyboard?> TryConnectAsync(IFeatureChannel channel, bool isDongle, ILogger log, CancellationToken cancellationToken = default)
    {
        DeviceIdentity? identity = null;
        for (var attempt = 0; attempt < MaxIdentifyAttempts && identity is null; attempt++)
        {
            var response = await channel.ExchangeAsync(Ry5088Packets.GetUsbVersion(), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (DeviceIdentity.TryParse(response, out var parsed))
            {
                identity = parsed;
            }
            else
            {
                await Task.Delay(40, cancellationToken).ConfigureAwait(false);
            }
        }

        if (identity is null)
        {
            log.Info(isDongle
                ? $"Receiver at {channel.DevicePath} has no keyboard answering (keyboard is probably wired or off)."
                : $"No RY5088 answer from {channel.DevicePath}.");
            return null;
        }

        if (identity.Value.DeviceId != Sk75TmrLayout.DeviceId)
        {
            log.Warn($"Device id {identity.Value.DeviceId} at {channel.DevicePath} is not an SK75 TMR ({Sk75TmrLayout.DeviceId}); ignoring it.");
            return null;
        }

        var rfResponse = await channel.ExchangeAsync(Ry5088Packets.GetRfVersion(), cancellationToken: cancellationToken).ConfigureAwait(false);
        var rfVersion = rfResponse[0] == Opcode.GetRfVersion ? rfResponse[1] | (rfResponse[2] << 8) : 0;

        var profileResponse = await channel.ExchangeAsync(Ry5088Packets.GetProfile(), cancellationToken: cancellationToken).ConfigureAwait(false);
        byte? onboardProfile = profileResponse[0] == Opcode.GetProfile ? profileResponse[1] : null;

        var scale = TravelScale.ForFirmware(identity.Value.UsbVersion, rfVersion);
        var supportsTopDeadZone = identity.Value.UsbVersion >= 1024 || rfVersion >= 1024;
        var info = new KeyboardInfo(channel.DevicePath, isDongle, identity.Value.DeviceId, identity.Value.UsbVersion, rfVersion, onboardProfile, scale, supportsTopDeadZone);
        return new Sk75Keyboard(channel, info, log);
    }

    /// <summary>Reads every per-key table this driver manages and decodes them into millimetres.</summary>
    public async Task<KeyboardSnapshot> ReadKeySettingsAsync(CancellationToken cancellationToken = default)
    {
        var scale = Info.Scale;
        var modes = await ReadTableAsync(MagnetismStream.KeyModes, cancellationToken).ConfigureAwait(false);
        var travel = Ry5088Packets.DecodeU16Table(await ReadTableAsync(MagnetismStream.Travel, cancellationToken).ConfigureAwait(false));
        var lift = Ry5088Packets.DecodeU16Table(await ReadTableAsync(MagnetismStream.LiftTravel, cancellationToken).ConfigureAwait(false));
        var firePress = Ry5088Packets.DecodeU16Table(await ReadTableAsync(MagnetismStream.FirePressTravel, cancellationToken).ConfigureAwait(false));
        var fireLift = Ry5088Packets.DecodeU16Table(await ReadTableAsync(MagnetismStream.FireLiftTravel, cancellationToken).ConfigureAwait(false));
        var deadZone = Ry5088Packets.DecodeU16Table(await ReadTableAsync(MagnetismStream.DeadZoneTravel, cancellationToken).ConfigureAwait(false));
        var topDeadZone = Info.SupportsTopDeadZone ? await ReadTableAsync(MagnetismStream.TopDeadZoneTravel, cancellationToken).ConfigureAwait(false) : null;
        var switches = await ReadTableAsync(MagnetismStream.SwitchBody, cancellationToken).ConfigureAwait(false);

        var keys = new Dictionary<int, KeySettings>(Sk75TmrLayout.Keys.Count);
        foreach (var key in Sk75TmrLayout.Keys)
        {
            var slot = key.Slot;
            keys[slot] = new KeySettings
            {
                ActuationMm = scale.ToMm(travel[slot]),
                ReleaseMm = scale.ToMm(lift[slot]),
                RapidTrigger = (modes[slot] & MagnetismStreams.RapidTriggerFlag) != 0,
                // The firmware leaves the rapid-trigger tables at 0 until RT is used once; 0 would be an unusable
                // sensitivity, so fall back to the vendor default for keys that never had RT configured.
                RtPressMm = firePress[slot] == 0 ? KeySettings.VendorDefault.RtPressMm : scale.ToMm(firePress[slot]),
                RtReleaseMm = fireLift[slot] == 0 ? KeySettings.VendorDefault.RtReleaseMm : scale.ToMm(fireLift[slot]),
                DeadZoneMm = scale.ToMm(deadZone[slot]),
                TopDeadZoneMm = topDeadZone is null ? KeySettings.VendorDefault.TopDeadZoneMm : scale.ToMm(topDeadZone[slot]),
            };
        }

        return new KeyboardSnapshot(keys, modes, switches);
    }

    /// <summary>
    /// Writes the given per-key settings. Slots that are not in <paramref name="keys"/> keep the values the
    /// firmware already has (the tables are read, patched and written back in full, as the vendor software does).
    /// The write is verified by reading the actuation and mode tables back.
    /// </summary>
    public async Task<ApplyResult> WriteKeySettingsAsync(IReadOnlyDictionary<int, KeySettings> keys, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var scale = Info.Scale;

        var modes = await ReadTableAsync(MagnetismStream.KeyModes, cancellationToken).ConfigureAwait(false);
        var travel = Ry5088Packets.DecodeU16Table(await ReadTableAsync(MagnetismStream.Travel, cancellationToken).ConfigureAwait(false));
        var lift = Ry5088Packets.DecodeU16Table(await ReadTableAsync(MagnetismStream.LiftTravel, cancellationToken).ConfigureAwait(false));
        var firePress = Ry5088Packets.DecodeU16Table(await ReadTableAsync(MagnetismStream.FirePressTravel, cancellationToken).ConfigureAwait(false));
        var fireLift = Ry5088Packets.DecodeU16Table(await ReadTableAsync(MagnetismStream.FireLiftTravel, cancellationToken).ConfigureAwait(false));
        var deadZone = Ry5088Packets.DecodeU16Table(await ReadTableAsync(MagnetismStream.DeadZoneTravel, cancellationToken).ConfigureAwait(false));
        var topDeadZone = Info.SupportsTopDeadZone ? await ReadTableAsync(MagnetismStream.TopDeadZoneTravel, cancellationToken).ConfigureAwait(false) : null;

        var written = 0;
        foreach (var (slot, requested) in keys)
        {
            if (slot is < 0 or >= MagnetismStreams.SlotCount)
            {
                continue;
            }

            var settings = requested.Clamp(Limits);
            var rapid = settings.RapidTrigger ? MagnetismStreams.RapidTriggerFlag : 0;
            modes[slot] = (byte)(MagnetismStreams.ModeNormal | rapid);
            travel[slot] = scale.ToRaw(settings.ActuationMm);
            lift[slot] = scale.ToRaw(settings.ReleaseMm);
            if (settings.RapidTrigger)
            {
                // The RT sensitivity tables are only consulted while the RT flag is set; leave them alone otherwise
                // so a profile without rapid trigger does not churn flash cells for nothing.
                firePress[slot] = scale.ToRaw(settings.RtPressMm);
                fireLift[slot] = scale.ToRaw(settings.RtReleaseMm);
            }

            deadZone[slot] = scale.ToRaw(settings.DeadZoneMm);
            if (topDeadZone is not null)
            {
                topDeadZone[slot] = (byte)Math.Min(byte.MaxValue, (int)scale.ToRaw(settings.TopDeadZoneMm));
            }

            written++;
        }

        // Same order as the vendor software: modes first, then the travel tables; the very last chunk commits.
        var tables = new List<(MagnetismStream Stream, byte[] Payload)>
        {
            (MagnetismStream.KeyModes, modes),
            (MagnetismStream.Travel, Ry5088Packets.EncodeU16Table(travel)),
            (MagnetismStream.LiftTravel, Ry5088Packets.EncodeU16Table(lift)),
            (MagnetismStream.FirePressTravel, Ry5088Packets.EncodeU16Table(firePress)),
            (MagnetismStream.FireLiftTravel, Ry5088Packets.EncodeU16Table(fireLift)),
            (MagnetismStream.DeadZoneTravel, Ry5088Packets.EncodeU16Table(deadZone)),
        };
        if (topDeadZone is not null)
        {
            tables.Add((MagnetismStream.TopDeadZoneTravel, topDeadZone));
        }

        for (var i = 0; i < tables.Count; i++)
        {
            var (stream, payload) = tables[i];
            var commit = i == tables.Count - 1;
            foreach (var packet in Ry5088Packets.WriteMagnetismTable(stream, payload, commit))
            {
                await _channel.SendAsync(packet, cancellationToken: cancellationToken).ConfigureAwait(false);
                await Task.Delay(2, cancellationToken).ConfigureAwait(false);
            }
        }

        // The vendor software waits 500 ms after the commit chunk and another 100 ms before touching the device again.
        await Task.Delay(600, cancellationToken).ConfigureAwait(false);

        var travelBack = Ry5088Packets.DecodeU16Table(await ReadTableAsync(MagnetismStream.Travel, cancellationToken).ConfigureAwait(false));
        var modesBack = await ReadTableAsync(MagnetismStream.KeyModes, cancellationToken).ConfigureAwait(false);
        var mismatches = 0;
        foreach (var slot in keys.Keys)
        {
            if (slot is < 0 or >= MagnetismStreams.SlotCount)
            {
                continue;
            }

            if (travelBack[slot] != travel[slot] || (modesBack[slot] & MagnetismStreams.RapidTriggerFlag) != (modes[slot] & MagnetismStreams.RapidTriggerFlag))
            {
                mismatches++;
            }
        }

        stopwatch.Stop();
        if (mismatches == 0)
        {
            _log.Info($"Wrote {written} keys in {stopwatch.ElapsedMilliseconds} ms, verified.");
            return new ApplyResult(true, $"{written} keys written and verified", written, 0, stopwatch.Elapsed);
        }

        _log.Warn($"Wrote {written} keys but {mismatches} did not read back as written.");
        return new ApplyResult(false, $"{mismatches} of {written} keys did not read back as written", written, mismatches, stopwatch.Elapsed);
    }

    /// <summary>
    /// Low-level: writes one complete per-key table as-is and commits it. Meant for diagnostics and recovery
    /// (<c>hidprobe fill</c>); the profile machinery uses <see cref="WriteKeySettingsAsync"/>.
    /// </summary>
    public async Task WriteTableAsync(MagnetismStream stream, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        foreach (var packet in Ry5088Packets.WriteMagnetismTable(stream, payload.Span, commitAtEnd: true))
        {
            await _channel.SendAsync(packet, cancellationToken: cancellationToken).ConfigureAwait(false);
            await Task.Delay(2, cancellationToken).ConfigureAwait(false);
        }

        await Task.Delay(600, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads one complete per-key table (raw bytes, little-endian u16 for the travel tables).</summary>
    public async Task<byte[]> ReadTableAsync(MagnetismStream stream, CancellationToken cancellationToken = default)
    {
        var chunks = stream.ReadChunkCount();
        var buffer = new byte[chunks * Ry5088Packets.ReportLength];
        for (var chunk = 0; chunk < chunks; chunk++)
        {
            var response = await _channel.ExchangeAsync(Ry5088Packets.ReadMagnetismChunk(stream, chunk), cancellationToken: cancellationToken).ConfigureAwait(false);
            response.AsSpan(0, Ry5088Packets.ReportLength).CopyTo(buffer.AsSpan(chunk * Ry5088Packets.ReportLength));
        }

        return buffer[..stream.PayloadLength()];
    }

    public void Dispose() => _channel.Dispose();
}
