using WomierDriver.Core.Protocol;

namespace WomierDriver.Core.Hid;

/// <summary>
/// A serialized request/response channel to one HID feature-report collection.
/// All RY5088 traffic goes through this: a SET_FEATURE carrying the request, and for queries a
/// GET_FEATURE shortly afterwards that returns the device's answer.
/// </summary>
public interface IFeatureChannel : IDisposable
{
    string DevicePath { get; }

    bool IsOpen { get; }

    /// <summary>Sends one packet (SET_FEATURE) without reading an answer.</summary>
    Task SendAsync(ReadOnlyMemory<byte> request, ChecksumKind checksum = ChecksumKind.Bit7, CancellationToken cancellationToken = default);

    /// <summary>Sends one packet and reads the device's 64-byte answer (SET_FEATURE, short delay, GET_FEATURE).</summary>
    Task<byte[]> ExchangeAsync(ReadOnlyMemory<byte> request, ChecksumKind checksum = ChecksumKind.Bit7, CancellationToken cancellationToken = default);
}
