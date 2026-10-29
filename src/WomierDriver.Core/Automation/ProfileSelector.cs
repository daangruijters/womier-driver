using WomierDriver.Core.Profiles;

namespace WomierDriver.Core.Automation;

public enum ActivationReason
{
    /// <summary>No profile could be chosen (empty library).</summary>
    None,

    /// <summary>The user picked the profile explicitly (tray menu or "Activate" button).</summary>
    Manual,

    /// <summary>A process listed in the profile's games is running.</summary>
    Game,

    /// <summary>No game matched, so the default profile is active.</summary>
    Default,
}

public sealed record ProfileSelection(Profile? Profile, ActivationReason Reason, string? MatchedProcess)
{
    public static ProfileSelection Nothing { get; } = new(null, ActivationReason.None, null);
}

/// <summary>Pure decision logic: which profile should be on the keyboard right now.</summary>
public static class ProfileSelector
{
    public static ProfileSelection Select(IReadOnlyList<Profile> profiles, IReadOnlySet<string> runningProcesses, Guid? manualOverride, bool autoSwitchEnabled)
    {
        if (manualOverride is { } id && profiles.FirstOrDefault(p => p.Id == id) is { } manual)
        {
            return new ProfileSelection(manual, ActivationReason.Manual, null);
        }

        if (autoSwitchEnabled)
        {
            var automatic = SelectAutomatic(profiles, runningProcesses);
            if (automatic.Profile is not null)
            {
                return automatic;
            }
        }

        var fallback = profiles.FirstOrDefault(p => p.IsDefault) ?? profiles.FirstOrDefault();
        return fallback is null ? ProfileSelection.Nothing : new ProfileSelection(fallback, ActivationReason.Default, null);
    }

    /// <summary>First profile (in library order) that lists a running process; <see cref="ProfileSelection.Nothing"/> otherwise.</summary>
    public static ProfileSelection SelectAutomatic(IReadOnlyList<Profile> profiles, IReadOnlySet<string> runningProcesses)
    {
        foreach (var profile in profiles)
        {
            foreach (var game in profile.Games)
            {
                var normalized = GameRule.Normalize(game);
                if (normalized.Length > 0 && runningProcesses.Contains(normalized))
                {
                    return new ProfileSelection(profile, ActivationReason.Game, normalized);
                }
            }
        }

        return ProfileSelection.Nothing;
    }
}
