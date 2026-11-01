using System.Text.Json.Serialization;

namespace WomierDriver.Core.Profiles;

/// <summary>All profiles of the user. Serialized as a whole to profiles.json.</summary>
public sealed class ProfileLibrary
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public List<Profile> Profiles { get; set; } = [];

    [JsonIgnore]
    public Profile? Default => Profiles.FirstOrDefault(p => p.IsDefault) ?? Profiles.FirstOrDefault();

    public Profile? Find(Guid id) => Profiles.FirstOrDefault(p => p.Id == id);

    public void Add(Profile profile)
    {
        if (Profiles.Count == 0)
        {
            profile.IsDefault = true;
        }

        Profiles.Add(profile);
        EnsureInvariants();
    }

    public bool Remove(Guid id)
    {
        var removed = Profiles.RemoveAll(p => p.Id == id) > 0;
        EnsureInvariants();
        return removed;
    }

    public void SetDefault(Guid id)
    {
        foreach (var profile in Profiles)
        {
            profile.IsDefault = profile.Id == id;
        }

        EnsureInvariants();
    }

    public void Move(Guid id, int newIndex)
    {
        var index = Profiles.FindIndex(p => p.Id == id);
        if (index < 0)
        {
            return;
        }

        var profile = Profiles[index];
        Profiles.RemoveAt(index);
        Profiles.Insert(Math.Clamp(newIndex, 0, Profiles.Count), profile);
    }

    /// <summary>Makes sure exactly one profile is the default (when there is at least one), ids are unique and key values are sane.</summary>
    public void EnsureInvariants()
    {
        var seen = new HashSet<Guid>();
        foreach (var profile in Profiles)
        {
            if (!seen.Add(profile.Id))
            {
                profile.Id = Guid.NewGuid();
                seen.Add(profile.Id);
            }

            foreach (var slot in profile.Keys.Keys.ToList())
            {
                profile.Keys[slot] = profile.Keys[slot].Sanitized();
            }

            profile.Lighting = profile.Lighting?.Sanitized();
        }

        var defaults = Profiles.Where(p => p.IsDefault).ToList();
        if (defaults.Count == 1)
        {
            return;
        }

        foreach (var profile in Profiles)
        {
            profile.IsDefault = false;
        }

        var first = defaults.FirstOrDefault() ?? Profiles.FirstOrDefault();
        if (first is not null)
        {
            first.IsDefault = true;
        }
    }

    /// <summary>Returns <paramref name="baseName"/>, or "baseName (2)", "(3)", ... when the name is taken.</summary>
    public string UniqueName(string baseName)
    {
        var name = baseName.Trim();
        if (name.Length == 0)
        {
            name = "Profile";
        }

        if (!Profiles.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return name;
        }

        for (var i = 2; ; i++)
        {
            var candidate = $"{name} ({i})";
            if (!Profiles.Any(p => string.Equals(p.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }
    }
}
