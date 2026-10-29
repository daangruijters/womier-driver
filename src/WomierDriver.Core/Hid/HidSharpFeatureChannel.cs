using HidSharp;
using WomierDriver.Core.Protocol;

namespace WomierDriver.Core.Hid;

/// <summary><see cref="IFeatureChannel"/> implemented on top of HidSharp (Win32 HidD_SetFeature / HidD_GetFeature).</summary>
public sealed class HidSharpFeatureChannel : IFeatureChannel
{
    private const int ReportIdLength = 1;

    private readonly HidDevice _device;
    private readonly HidStream _stream;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private volatile bool _disposed;

    private HidSharpFeatureChannel(HidDevice device, HidStream stream)
    {
        _device = device;
        _stream = stream;
        FeatureReportLength = Math.Max(SafeMaxFeatureLength(device), ReportIdLength + Ry5088Packets.ReportLength);
    }

    /// <summary>Delay between the request and the read of its answer. The vendor software uses 10 ms.</summary>
    public int ReadDelayMs { get; init; } = 10;

    /// <summary>Size of the Win32 feature buffer, including the leading report id byte (65 on the SK75 TMR).</summary>
    public int FeatureReportLength { get; }

    public string DevicePath => _device.DevicePath;

    public bool IsOpen => !_disposed;

    /// <summary>Opens the device in shared mode so the vendor software can stay running next to this driver.</summary>
    public static HidSharpFeatureChannel Open(HidDevice device)
    {
        var configuration = new OpenConfiguration();
        configuration.SetOption(OpenOption.Exclusive, false);
        var stream = device.Open(configuration);
        stream.ReadTimeout = 1000;
        stream.WriteTimeout = 1000;
        return new HidSharpFeatureChannel(device, stream);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> request, ChecksumKind checksum = ChecksumKind.Bit7, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await Task.Run(() => SetFeature(request.Span, checksum), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<byte[]> ExchangeAsync(ReadOnlyMemory<byte> request, ChecksumKind checksum = ChecksumKind.Bit7, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                SetFeature(request.Span, checksum);
                Thread.Sleep(ReadDelayMs);
                return GetFeature();
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _stream.Dispose();
        }
        catch
        {
            // Closing a vanished device can throw; nothing useful to do about it.
        }

        _gate.Dispose();
    }

    private static int SafeMaxFeatureLength(HidDevice device)
    {
        try
        {
            return device.GetMaxFeatureReportLength();
        }
        catch
        {
            return 0;
        }
    }

    private void SetFeature(ReadOnlySpan<byte> request, ChecksumKind checksum)
    {
        ThrowIfDisposed();
        var buffer = new byte[FeatureReportLength]; // buffer[0] is the report id (0)
        var payload = buffer.AsSpan(ReportIdLength, Ry5088Packets.ReportLength);
        request[..Math.Min(request.Length, payload.Length)].CopyTo(payload);
        Checksum.Apply(payload, checksum);
        _stream.SetFeature(buffer);
    }

    private byte[] GetFeature()
    {
        ThrowIfDisposed();
        var buffer = new byte[FeatureReportLength];
        _stream.GetFeature(buffer);
        return buffer.AsSpan(ReportIdLength, Ry5088Packets.ReportLength).ToArray();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HidSharpFeatureChannel));
        }
    }
}
