using WomierDriver.Core.Hid;

namespace WomierDriver.Tests;

/// <summary>In-memory stand-in for the keyboard's vendor input collection: tests push reports and failures by hand.</summary>
public sealed class FakeInputReportChannel : IInputReportChannel
{
    public string DevicePath => "fake://sk75-input";

    public int ReportLength => 33;

    public bool Started { get; private set; }

    public bool IsDisposed { get; private set; }

    public event Action<byte[]>? ReportReceived;

    public event Action<Exception?>? Stopped;

    public void Start() => Started = true;

    /// <summary>Delivers one report (byte 0 is the report id) as if the keyboard had sent it.</summary>
    public void Push(params byte[] report) => ReportReceived?.Invoke(report);

    /// <summary>Simulates the device going away.</summary>
    public void Fail(Exception exception) => Stopped?.Invoke(exception);

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        Stopped?.Invoke(null);
    }
}
