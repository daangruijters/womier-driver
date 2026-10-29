using WomierDriver.Core.Automation;
using WomierDriver.Core.Device;
using WomierDriver.Core.Profiles;
using WomierDriver.Core.Protocol;
using Xunit;

namespace WomierDriver.Tests;

public class ProfileTests
{
    [Fact]
    public void Library_RoundTripsThroughJson()
    {
        var library = new ProfileLibrary();
        var gaming = Profile.CreateUniform("Gaming", KeySettings.Gaming);
        gaming.AddGame("BF6.exe");
        gaming.AddGame(@"C:\Games\cs2");
        library.Add(Profile.CreateUniform("Default", KeySettings.VendorDefault));
        library.Add(gaming);

        var json = JsonStore.Serialize(library);
        var restored = JsonStore.Deserialize<ProfileLibrary>(json);

        Assert.NotNull(restored);
        Assert.Equal(2, restored.Profiles.Count);
        Assert.Equal("Default", restored.Default?.Name);
        var restoredGaming = restored.Profiles[1];
        Assert.Equal(["bf6.exe", "cs2.exe"], restoredGaming.Games);
        Assert.Equal(81, restoredGaming.Keys.Count);
        Assert.True(restoredGaming.Keys[14].RapidTrigger);
        Assert.Equal(1.0, restoredGaming.Keys[14].ActuationMm);
        Assert.Equal(gaming.KeySignature(), restoredGaming.KeySignature());
    }

    [Fact]
    public void Library_KeepsExactlyOneDefault()
    {
        var library = new ProfileLibrary();
        var a = Profile.CreateUniform("A", KeySettings.VendorDefault);
        var b = Profile.CreateUniform("B", KeySettings.VendorDefault);
        library.Add(a);
        library.Add(b);
        Assert.True(a.IsDefault);
        Assert.False(b.IsDefault);

        library.SetDefault(b.Id);
        Assert.False(a.IsDefault);
        Assert.True(b.IsDefault);

        library.Remove(b.Id);
        Assert.True(a.IsDefault);

        Assert.Equal("A (2)", library.UniqueName("A"));
        Assert.Equal("C", library.UniqueName("C"));
    }

    [Fact]
    public void Signature_ChangesWithKeyEditsOnly()
    {
        var profile = Profile.CreateUniform("P", KeySettings.VendorDefault);
        var before = profile.KeySignature();
        profile.Name = "Renamed";
        profile.AddGame("game.exe");
        Assert.Equal(before, profile.KeySignature());

        profile.SetKey(9, profile.GetKey(9) with { ActuationMm = 1.2 });
        Assert.NotEqual(before, profile.KeySignature());
    }

    [Fact]
    public void Clamp_EnforcesVendorLimits()
    {
        var limits = KeyLimits.ForFirmware(776, TravelScale.Centi);
        var wild = new KeySettings
        {
            ActuationMm = 9,
            ReleaseMm = 9,
            RtPressMm = 0,
            RtReleaseMm = 5,
            DeadZoneMm = 0.5,
            TopDeadZoneMm = -1,
        };
        var clamped = wild.Clamp(limits);
        Assert.Equal(3.3, clamped.ActuationMm);
        Assert.Equal(2.8, clamped.ReleaseMm); // 3.3 - 0.5 dead zone
        Assert.Equal(0.01, clamped.RtPressMm);
        Assert.Equal(2.0, clamped.RtReleaseMm);
        Assert.Equal(0.5, clamped.DeadZoneMm);
        Assert.Equal(0.0, clamped.TopDeadZoneMm);

        var oldFirmware = KeyLimits.ForFirmware(500, TravelScale.ForFirmware(500));
        Assert.Equal(4.0, oldFirmware.TravelMax);
        Assert.Equal(0.1, oldFirmware.StepMm);
    }

    [Fact]
    public void Sanitized_RepairsZeroRapidTriggerValues()
    {
        var zero = new KeySettings { RtPressMm = 0, RtReleaseMm = 0 };
        var fixedUp = zero.Sanitized();
        Assert.Equal(0.2, fixedUp.RtPressMm);
        Assert.Equal(0.2, fixedUp.RtReleaseMm);
        Assert.Same(KeySettings.Gaming, KeySettings.Gaming.Sanitized());

        var library = new ProfileLibrary();
        var profile = Profile.CreateUniform("P", zero);
        library.Add(profile);
        library.EnsureInvariants();
        Assert.All(profile.Keys.Values, k => Assert.Equal(0.2, k.RtPressMm));
    }

    [Fact]
    public void Clamp_QuantizesToTheFirmwareGrid()
    {
        var limits = KeyLimits.ForFirmware(776, TravelScale.Centi);
        var clamped = new KeySettings { ActuationMm = 1.234567 }.Clamp(limits);
        Assert.Equal(1.23, clamped.ActuationMm);
    }

    [Fact]
    public void GameRules_AreNormalized()
    {
        Assert.Equal("bf6.exe", GameRule.Normalize("  BF6 "));
        Assert.Equal("bf6.exe", GameRule.Normalize("bf6.exe"));
        Assert.Equal("cs2.exe", GameRule.Normalize(@"""C:\Program Files\Steam\cs2.exe"""));
        Assert.Equal("game.bin", GameRule.Normalize("Game.BIN"));
        Assert.Equal(string.Empty, GameRule.Normalize("   "));

        var profile = new Profile();
        Assert.True(profile.AddGame("bf6"));
        Assert.False(profile.AddGame("BF6.EXE"));
        Assert.True(profile.RemoveGame("bf6.exe"));
        Assert.Empty(profile.Games);
    }

    [Fact]
    public void CreateUniform_CoversEveryPhysicalKey()
    {
        var profile = Profile.CreateUniform("P", KeySettings.Gaming);
        Assert.Equal(Sk75TmrLayout.Keys.Count, profile.Keys.Count);
        Assert.All(Sk75TmrLayout.Keys, k => Assert.True(profile.Keys.ContainsKey(k.Slot)));
    }
}
