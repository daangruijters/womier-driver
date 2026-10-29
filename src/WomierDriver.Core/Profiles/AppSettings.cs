namespace WomierDriver.Core.Profiles;

/// <summary>User preferences of the driver. Serialized to settings.json.</summary>
public sealed class AppSettings
{
    /// <summary>Switch profiles automatically based on running games.</summary>
    public bool AutoSwitchEnabled { get; set; } = true;

    /// <summary>Launch minimized to the tray when Windows starts (mirrors the HKCU Run entry).</summary>
    public bool StartWithWindows { get; set; }

    /// <summary>Show a tray notification whenever the active profile changes because of a game.</summary>
    public bool NotifyOnProfileSwitch { get; set; } = true;

    /// <summary>How often the running processes are sampled.</summary>
    public int ProcessPollIntervalMs { get; set; } = 2000;
}
