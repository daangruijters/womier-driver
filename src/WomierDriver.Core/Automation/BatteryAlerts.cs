namespace WomierDriver.Core.Automation;

/// <summary>How urgent a low battery is.</summary>
public enum BatteryAlert
{
    /// <summary>Nothing to report.</summary>
    None,

    /// <summary>At or below <see cref="BatteryAlerts.LowPercent"/>.</summary>
    Low,

    /// <summary>At or below <see cref="BatteryAlerts.CriticalPercent"/>.</summary>
    Critical,
}

/// <summary>
/// Decides when to warn about the keyboard battery: once when it reaches <see cref="LowPercent"/>, once more at
/// <see cref="CriticalPercent"/>. A warning is only repeated after the battery charged <see cref="Hysteresis"/> points
/// above its threshold, so a reading that wobbles around a threshold does not nag. Not thread-safe; feed it from one
/// thread.
/// </summary>
public sealed class BatteryAlerts
{
    /// <summary>Battery level (percent) at which the first warning is given.</summary>
    public const int LowPercent = 20;

    /// <summary>Battery level (percent) at which the second, more urgent warning is given.</summary>
    public const int CriticalPercent = 10;

    /// <summary>Points above a threshold the battery must reach before that warning can be given again.</summary>
    public const int Hysteresis = 5;

    private BatteryAlert _given;

    /// <summary>The level a battery percentage falls into, regardless of earlier warnings.</summary>
    public static BatteryAlert Classify(int percent)
        => percent <= CriticalPercent ? BatteryAlert.Critical : percent <= LowPercent ? BatteryAlert.Low : BatteryAlert.None;

    /// <summary>
    /// Feeds the latest battery level. Returns the warning to show now, or <see cref="BatteryAlert.None"/>.
    /// <c>null</c> (on the cable, or not known yet) keeps the current state.
    /// </summary>
    public BatteryAlert Update(int? percent)
    {
        if (percent is not { } level)
        {
            return BatteryAlert.None;
        }

        // Re-arm the warnings the battery has charged well past.
        if (_given == BatteryAlert.Critical && level >= CriticalPercent + Hysteresis)
        {
            _given = BatteryAlert.Low;
        }

        if (_given == BatteryAlert.Low && level >= LowPercent + Hysteresis)
        {
            _given = BatteryAlert.None;
        }

        var now = Classify(level);
        if (now > _given)
        {
            _given = now;
            return now;
        }

        return BatteryAlert.None;
    }
}
