using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using WomierDriver.Core.Automation;
using WomierDriver.Core.Device;

namespace WomierDriver.Core.Profiles;

/// <summary>A named set of per-key settings plus the games (process names) that activate it.</summary>
public sealed class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "New profile";

    /// <summary>The profile used whenever no game rule matches. Exactly one profile is the default.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Process names (e.g. "bf6.exe") whose presence activates this profile.</summary>
    public List<string> Games { get; set; } = [];

    /// <summary>Settings per key-matrix slot. Keys without an entry fall back to <see cref="KeySettings.VendorDefault"/>.</summary>
    public Dictionary<int, KeySettings> Keys { get; set; } = [];

    /// <summary>Backlight written together with the keys; <c>null</c> leaves the keyboard's lighting as it is.</summary>
    public LightingSettings? Lighting { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonIgnore]
    public int RapidTriggerKeyCount => Keys.Values.Count(k => k.RapidTrigger);

    public static Profile CreateUniform(string name, KeySettings settings)
    {
        var profile = new Profile { Name = name };
        foreach (var key in Sk75TmrLayout.Keys)
        {
            profile.Keys[key.Slot] = settings;
        }

        return profile;
    }

    public static Profile FromSnapshot(string name, IReadOnlyDictionary<int, KeySettings> keys)
    {
        var profile = new Profile { Name = name };
        foreach (var (slot, settings) in keys)
        {
            profile.Keys[slot] = settings;
        }

        return profile;
    }

    public KeySettings GetKey(int slot) => Keys.TryGetValue(slot, out var settings) ? settings : KeySettings.VendorDefault;

    public void SetKey(int slot, KeySettings settings)
    {
        Keys[slot] = settings;
        Touch();
    }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    /// <summary>Adds a game rule; returns false when the (normalized) rule already exists.</summary>
    public bool AddGame(string processName)
    {
        var normalized = GameRule.Normalize(processName);
        if (normalized.Length == 0 || Games.Any(g => GameRule.Normalize(g) == normalized))
        {
            return false;
        }

        Games.Add(normalized);
        Touch();
        return true;
    }

    public bool RemoveGame(string processName)
    {
        var normalized = GameRule.Normalize(processName);
        var removed = Games.RemoveAll(g => GameRule.Normalize(g) == normalized) > 0;
        if (removed)
        {
            Touch();
        }

        return removed;
    }

    public Profile Clone(string name) => new()
    {
        Name = name,
        Games = [.. Games],
        Keys = new Dictionary<int, KeySettings>(Keys),
        Lighting = Lighting,
    };

    /// <summary>Stable fingerprint of the key settings, used to skip redundant writes to the keyboard.</summary>
    public string KeySignature()
    {
        var builder = new StringBuilder();
        foreach (var (slot, k) in Keys.OrderBy(p => p.Key))
        {
            builder.Append(slot).Append(':')
                .Append(k.ActuationMm.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(k.ReleaseMm.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(k.RapidTrigger ? '1' : '0').Append(',')
                .Append(k.RtPressMm.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(k.RtReleaseMm.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(k.DeadZoneMm.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(k.TopDeadZoneMm.ToString("R", CultureInfo.InvariantCulture)).Append(';');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())))[..16];
    }
}
