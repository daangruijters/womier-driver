namespace WomierDriver.Core.Hid;

/// <summary>
/// Continuous reader of HID input reports from one collection. Reports are delivered on the reader thread;
/// consumers marshal as needed.
/// </summary>
public interface IInputReportChannel : IDisposable
{
    string DevicePath { get; }

    /// <summary>Largest report the collection can deliver, including the leading report id byte.</summary>
    int ReportLength { get; }

    /// <summary>One raw report; byte 0 is the HID report id.</summary>
    event Action<byte[]>? ReportReceived;

    /// <summary>Raised once when reading ends. The exception is <c>null</c> when the channel was disposed on purpose.</summary>
    event Action<Exception?>? Stopped;

    /// <summary>Starts the reader. Call once.</summary>
    void Start();
}
