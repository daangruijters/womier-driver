using WomierDriver.Core.Automation;
using WomierDriver.Core.Profiles;
using Xunit;

namespace WomierDriver.Tests;

public class SelectorTests
{
    private static (List<Profile> Profiles, Profile Default, Profile Gaming, Profile Racing) Library()
    {
        var @default = Profile.CreateUniform("Default", KeySettings.VendorDefault);
        @default.IsDefault = true;
        var gaming = Profile.CreateUniform("Gaming", KeySettings.Gaming);
        gaming.AddGame("bf6.exe");
        gaming.AddGame("cs2.exe");
        var racing = Profile.CreateUniform("Racing", KeySettings.Gaming);
        racing.AddGame("forza.exe");
        return ([@default, gaming, racing], @default, gaming, racing);
    }

    private static HashSet<string> Running(params string[] names) => new(names, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void FallsBackToDefaultWhenNoGameRuns()
    {
        var (profiles, @default, _, _) = Library();
        var selection = ProfileSelector.Select(profiles, Running("explorer.exe", "code.exe"), null, true);
        Assert.Same(@default, selection.Profile);
        Assert.Equal(ActivationReason.Default, selection.Reason);
    }

    [Fact]
    public void PicksTheProfileWhoseGameIsRunning()
    {
        var (profiles, _, _, racing) = Library();
        var selection = ProfileSelector.Select(profiles, Running("explorer.exe", "FORZA.EXE"), null, true);
        Assert.Same(racing, selection.Profile);
        Assert.Equal(ActivationReason.Game, selection.Reason);
        Assert.Equal("forza.exe", selection.MatchedProcess);
    }

    [Fact]
    public void LibraryOrderBreaksTies()
    {
        var (profiles, _, gaming, _) = Library();
        var selection = ProfileSelector.Select(profiles, Running("forza.exe", "cs2.exe"), null, true);
        Assert.Same(gaming, selection.Profile);
    }

    [Fact]
    public void ManualOverrideWins()
    {
        var (profiles, _, _, racing) = Library();
        var selection = ProfileSelector.Select(profiles, Running("cs2.exe"), racing.Id, true);
        Assert.Same(racing, selection.Profile);
        Assert.Equal(ActivationReason.Manual, selection.Reason);
    }

    [Fact]
    public void UnknownManualOverrideIsIgnored()
    {
        var (profiles, _, gaming, _) = Library();
        var selection = ProfileSelector.Select(profiles, Running("cs2.exe"), Guid.NewGuid(), true);
        Assert.Same(gaming, selection.Profile);
    }

    [Fact]
    public void DisabledAutoSwitchUsesDefault()
    {
        var (profiles, @default, _, _) = Library();
        var selection = ProfileSelector.Select(profiles, Running("cs2.exe"), null, false);
        Assert.Same(@default, selection.Profile);
        Assert.Equal(ActivationReason.Default, selection.Reason);
    }

    [Fact]
    public void EmptyLibrarySelectsNothing()
    {
        var selection = ProfileSelector.Select([], Running("cs2.exe"), null, true);
        Assert.Null(selection.Profile);
        Assert.Equal(ActivationReason.None, selection.Reason);
    }
}
