using HidSharp;

namespace WomierDriver.Core.Hid;

/// <summary>
/// <see cref="IInputReportChannel"/> on top of HidSharp: a dedicated background thread blocks in
/// <see cref="HidStream.Read(byte[], int, int)"/> with a short timeout, so disposing the channel ends the loop
/// within one timeout and a vanished device surfaces as the exception the read throws.
/// </summary>
public sealed class HidSharpInputReportChannel : IInputReportChannel
{
    private readonly HidDevice _device;
    private readonly HidStream _stream;
    private Thread? _thread;
    private volatile bool _disposed;
    private int _stoppedRaised;

    private HidSharpInputReportChannel(HidDevice device, HidStream stream, int reportLength)
    {
        _device = device;
        _stream = stream;
        ReportLength = reportLength;
    }

    public string DevicePath => _device.DevicePath;

    public int ReportLength { get; }

    public event Action<byte[]>? ReportReceived;

    public event Action<Exception?>? Stopped;

    /// <summary>Opens the collection in shared mode (the vendor software may be listening too).</summary>
    public static HidSharpInputReportChannel Open(HidDevice device, int readTimeoutMs = 250)
    {
        var configuration = new OpenConfiguration();
        configuration.SetOption(OpenOption.Exclusive, false);
        var stream = device.Open(configuration);
        stream.ReadTimeout = readTimeoutMs;
        int length;
        try
        {
            length = device.GetMaxInputReportLength();
        }
        catch
        {
            length = 0;
        }

        return new HidSharpInputReportChannel(device, stream, Math.Max(length, 65));
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_thread is not null)
        {
            throw new InvalidOperationException("The channel has already been started.");
        }

        _thread = new Thread(ReadLoop) { IsBackground = true, Name = "sk75-input-reports" };
        _thread.Start();
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

        if (_thread is { } thread && thread != Thread.CurrentThread)
        {
            thread.Join(1000);
        }

        RaiseStopped(null);
    }

    private void ReadLoop()
    {
        var buffer = new byte[ReportLength];
        while (!_disposed)
        {
            int count;
            try
            {
                count = _stream.Read(buffer, 0, buffer.Length);
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch (Exception ex)
            {
                RaiseStopped(_disposed ? null : ex);
                return;
            }

            if (count > 0)
            {
                ReportReceived?.Invoke(buffer[..count]);
            }
        }

        RaiseStopped(null);
    }

    private void RaiseStopped(Exception? exception)
    {
        if (Interlocked.Exchange(ref _stoppedRaised, 1) == 0)
        {
            Stopped?.Invoke(exception);
        }
    }
}
