using WomierDriver.Core.Automation;
using Xunit;

namespace WomierDriver.Tests;

public sealed class BatteryAlertTests
{
    [Theory]
    [InlineData(100, BatteryAlert.None)]
    [InlineData(21, BatteryAlert.None)]
    [InlineData(20, BatteryAlert.Low)]
    [InlineData(11, BatteryAlert.Low)]
    [InlineData(10, BatteryAlert.Critical)]
    [InlineData(1, BatteryAlert.Critical)]
    public void Classify_uses_the_thresholds(int percent, BatteryAlert expected)
        => Assert.Equal(expected, BatteryAlerts.Classify(percent));

    [Fact]
    public void Draining_battery_warns_once_at_low_and_once_at_critical()
    {
        var alerts = new BatteryAlerts();
        var given = Enumerable.Range(1, 99).Reverse().Select(p => (Percent: p, Alert: alerts.Update(p)))
            .Where(x => x.Alert != BatteryAlert.None).ToList();

        Assert.Equal([(20, BatteryAlert.Low), (10, BatteryAlert.Critical)], given);
    }

    [Fact]
    public void Starting_below_a_threshold_warns_at_once_with_the_matching_level()
    {
        Assert.Equal(BatteryAlert.Low, new BatteryAlerts().Update(15));
        Assert.Equal(BatteryAlert.Critical, new BatteryAlerts().Update(5));
    }

    [Fact]
    public void Wobbling_around_a_threshold_does_not_repeat_the_warning()
    {
        var alerts = new BatteryAlerts();
        Assert.Equal(BatteryAlert.Low, alerts.Update(20));
        foreach (var percent in new[] { 21, 20, 22, 19, 24, 20 })
        {
            Assert.Equal(BatteryAlert.None, alerts.Update(percent));
        }
    }

    [Fact]
    public void Charging_past_the_hysteresis_rearms_the_warnings()
    {
        var alerts = new BatteryAlerts();
        Assert.Equal(BatteryAlert.Critical, alerts.Update(8));
        Assert.Equal(BatteryAlert.None, alerts.Update(15)); // critical re-armed, low still given
        Assert.Equal(BatteryAlert.Critical, alerts.Update(10));
        Assert.Equal(BatteryAlert.None, alerts.Update(25)); // both re-armed
        Assert.Equal(BatteryAlert.Low, alerts.Update(20));
    }

    [Fact]
    public void Unknown_level_keeps_the_state()
    {
        var alerts = new BatteryAlerts();
        Assert.Equal(BatteryAlert.Low, alerts.Update(18));
        Assert.Equal(BatteryAlert.None, alerts.Update(null));
        Assert.Equal(BatteryAlert.None, alerts.Update(18));
    }
}
