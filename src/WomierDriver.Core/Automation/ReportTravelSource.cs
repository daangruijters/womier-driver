using WomierDriver.Core.Device;
using WomierDriver.Core.Hid;
using WomierDriver.Core.Protocol;

namespace WomierDriver.Core.Automation;

/// <summary>
/// Turns the firmware's live travel reports (docs/protocol.md §6) into per-key samples. Reports for matrix slots
/// without a physical key are dropped and the notifications on the same collection are ignored.
/// </summary>
public sealed class ReportTravelSource : ILiveTravelSource
{
    private readonly IInputReportChannel _channel;
    private readonly TravelScale _scale;
    private bool _disposed;

    public ReportTravelSource(IInputReportChannel channel, TravelScale scale)
    {
        _channel = channel;
        _scale = scale;
        _channel.ReportReceived += OnReport;
        _channel.Stopped += OnStopped;
    }

    public event Action<LiveTravelSample>? Sample;

    public event Action<Exception?>? Stopped;

    public void Start() => _channel.Start();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _channel.ReportReceived -= OnReport;
        _channel.Dispose();
    }

    private void OnReport(byte[] report)
    {
        if (!VendorInputReports.TryParseTravel(report, out var raw, out var slot) || !Sk75TmrLayout.Slots.Contains(slot))
        {
            return;
        }

        Sample?.Invoke(new LiveTravelSample(slot, raw, _scale.ToMm(raw)));
    }

    private void OnStopped(Exception? exception) => Stopped?.Invoke(exception);
}
