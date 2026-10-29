namespace WomierDriver.Core.Protocol;

/// <summary>
/// Converts between millimetres and the integer travel units used on the wire.
/// The unit depends on the firmware version, exactly as the vendor software decides it.
/// </summary>
public readonly record struct TravelScale(int UnitsPerMm)
{
    /// <summary>Scale used by firmware 768..1279 (0.01 mm units). The SK75 TMR ships with firmware 776 (v3.08).</summary>
    public static TravelScale Centi => new(100);

    /// <summary>Picks the scale for a firmware version: 1280+ uses 0.005 mm, 768+ uses 0.01 mm, older firmware uses 0.1 mm.</summary>
    public static TravelScale ForFirmware(int? usbVersion, int? rfVersion = null)
    {
        var version = rfVersion is > 0 ? rfVersion : usbVersion;
        if (version is null)
        {
            return new TravelScale(10);
        }

        if (version >= 1280)
        {
            return new TravelScale(200);
        }

        if (version >= 768)
        {
            return new TravelScale(100);
        }

        return new TravelScale(10);
    }

    /// <summary>Smallest representable step in millimetres.</summary>
    public double StepMm => 1.0 / UnitsPerMm;

    /// <summary>Decimal places needed to display a value without rounding artefacts.</summary>
    public int Decimals => UnitsPerMm >= 200 ? 3 : 2;

    public ushort ToRaw(double mm)
    {
        var raw = Math.Round(mm * UnitsPerMm, MidpointRounding.AwayFromZero);
        return (ushort)Math.Clamp(raw, 0, ushort.MaxValue);
    }

    public double ToMm(int raw) => raw / (double)UnitsPerMm;

    /// <summary>Rounds a millimetre value to the nearest representable step.</summary>
    public double Quantize(double mm) => ToMm(ToRaw(mm));
}
