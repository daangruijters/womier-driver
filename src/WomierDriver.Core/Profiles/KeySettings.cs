using WomierDriver.Core.Protocol;

namespace WomierDriver.Core.Profiles;

/// <summary>
/// Analog settings of one key, in millimetres of key travel measured from the top.
/// Defaults match the vendor software's defaults for a fresh key.
/// </summary>
public sealed record KeySettings
{
    /// <summary>Actuation point ("press travel"): depth at which the key registers.</summary>
    public double ActuationMm { get; init; } = 2.0;

    /// <summary>Reset point ("release travel") used when rapid trigger is off.</summary>
    public double ReleaseMm { get; init; } = 2.8;

    /// <summary>Rapid trigger: re-trigger and release relative to the key's direction changes instead of fixed points.</summary>
    public bool RapidTrigger { get; init; }

    /// <summary>Rapid-trigger press sensitivity: downward movement needed to (re)actuate.</summary>
    public double RtPressMm { get; init; } = 0.2;

    /// <summary>Rapid-trigger release sensitivity: upward movement needed to release.</summary>
    public double RtReleaseMm { get; init; } = 0.2;

    /// <summary>Bottom dead zone: travel near the bottom that is ignored.</summary>
    public double DeadZoneMm { get; init; } = 0.3;

    /// <summary>Top dead zone (only honoured by firmware 1024 and newer).</summary>
    public double TopDeadZoneMm { get; init; } = 0.3;

    public static KeySettings VendorDefault { get; } = new();

    /// <summary>A typical competitive gaming setup: shallow actuation with rapid trigger.</summary>
    public static KeySettings Gaming { get; } = new()
    {
        ActuationMm = 1.0,
        ReleaseMm = 1.0,
        RapidTrigger = true,
        RtPressMm = 0.2,
        RtReleaseMm = 0.2,
        DeadZoneMm = 0.3,
    };

    /// <summary>
    /// Repairs values that cannot come from the UI but can come from old files or the firmware: rapid-trigger
    /// sensitivities of 0 (never configured) become the vendor default.
    /// </summary>
    public KeySettings Sanitized()
    {
        if (RtPressMm > 0 && RtReleaseMm > 0)
        {
            return this;
        }

        return this with
        {
            RtPressMm = RtPressMm > 0 ? RtPressMm : VendorDefault.RtPressMm,
            RtReleaseMm = RtReleaseMm > 0 ? RtReleaseMm : VendorDefault.RtReleaseMm,
        };
    }

    /// <summary>Returns a copy whose values are inside <paramref name="limits"/> and on the firmware's value grid.</summary>
    public KeySettings Clamp(KeyLimits limits)
    {
        var scale = new TravelScale((int)Math.Round(1.0 / limits.StepMm));
        var deadZone = Quantize(Math.Clamp(DeadZoneMm, limits.DeadZoneMin, limits.DeadZoneMax), scale);
        return new KeySettings
        {
            ActuationMm = Quantize(Math.Clamp(ActuationMm, limits.TravelMin, limits.TravelMax), scale),
            ReleaseMm = Quantize(Math.Clamp(ReleaseMm, limits.TravelMin, limits.ReleaseMax(deadZone)), scale),
            RapidTrigger = RapidTrigger,
            RtPressMm = Quantize(Math.Clamp(RtPressMm, limits.RtMin, limits.RtMax), scale),
            RtReleaseMm = Quantize(Math.Clamp(RtReleaseMm, limits.RtMin, limits.RtMax), scale),
            DeadZoneMm = deadZone,
            TopDeadZoneMm = Quantize(Math.Clamp(TopDeadZoneMm, limits.DeadZoneMin, limits.DeadZoneMax), scale),
        };
    }

    private static double Quantize(double mm, TravelScale scale) => Math.Round(scale.Quantize(mm), 4);
}

/// <summary>Allowed ranges for <see cref="KeySettings"/>, as enforced by the vendor software for this firmware generation.</summary>
public sealed record KeyLimits(
    double TravelMin,
    double TravelMax,
    double RtMin,
    double RtMax,
    double DeadZoneMin,
    double DeadZoneMax,
    double StepMm)
{
    /// <summary>Limits for firmware 768 and newer (0.1 .. 3.3 mm travel in 0.01 mm steps).</summary>
    public static KeyLimits Default { get; } = new(0.1, 3.3, 0.01, 2.0, 0.0, 1.0, 0.01);

    public static KeyLimits ForFirmware(int? usbVersion, TravelScale scale)
        => Default with { TravelMax = usbVersion is < 768 ? 4.0 : 3.3, StepMm = scale.StepMm };

    /// <summary>The vendor software caps the reset point at the maximum travel minus the bottom dead zone.</summary>
    public double ReleaseMax(double deadZoneMm) => Math.Max(TravelMin, TravelMax - deadZoneMm);
}
