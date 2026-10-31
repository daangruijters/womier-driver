using WomierDriver.Core.Device;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Profiles;
using WomierDriver.Core.Protocol;
using Xunit;

namespace WomierDriver.Tests;

public class LightingTests
{
    [Fact]
    public void SetLighting_MatchesTheVendorLayout()
    {
        var packet = Ry5088Packets.SetLighting(new LightingSettings
        {
            Effect = LightingEffect.Wave,
            Brightness = 3,
            Speed = 4,
            Color = new RgbColor(0x12, 0x34, 0x56),
            Option = 2,
        });

        Assert.Equal(new byte[] { 0x07, 0x04, 0x00, 0x03, 0x27, 0x12, 0x34, 0x56 }, packet[..8]);
        Assert.All(packet[8..], b => Assert.Equal(0, b));
    }

    [Fact]
    public void SetLighting_EncodesRainbowWhiteAndCustomPatternsLikeTheVendor()
    {
        var rainbow = Ry5088Packets.SetLighting(new LightingSettings { Effect = LightingEffect.Ripple, Speed = 0, Rainbow = true });
        Assert.Equal(0x08, rainbow[4]);
        Assert.Equal(0x04, rainbow[2]);

        var white = Ry5088Packets.SetLighting(new LightingSettings { Color = RgbColor.White });
        Assert.Equal(new byte[] { 0xFA, 0xFF, 0xFA }, white[5..8]);

        var custom = Ry5088Packets.SetLighting(new LightingSettings { Effect = LightingEffect.CustomPattern, Option = 4, Rainbow = true });
        Assert.Equal(0x40, custom[4]);
        Assert.Equal(new byte[] { 0x00, 0xC8, 0xC8 }, custom[5..8]);
    }

    [Fact]
    public void SetLighting_ClampsOutOfRangeValues()
    {
        var packet = Ry5088Packets.SetLighting(new LightingSettings { Effect = LightingEffect.Stream, Brightness = 9, Speed = -1, Option = 7 });

        Assert.Equal(LightingSettings.MaxLevel, packet[3]);
        Assert.Equal(LightingSettings.MaxLevel, packet[2]);
        Assert.Equal(0x17, packet[4]); // option 1 (the last of two), single colour
    }

    [Fact]
    public void Checksum_Bit8CoversTheColour()
    {
        var packet = Ry5088Packets.SetLighting(new LightingSettings { Color = new RgbColor(0xFF, 0x00, 0x80) });
        Checksum.Apply(packet, ChecksumKind.Bit8);

        Assert.True(Checksum.IsValid(packet, ChecksumKind.Bit8));
        Assert.Equal(0x80, packet[7]); // byte 7 is colour data, not a checksum
    }

    [Fact]
    public void TryParseLighting_DecodesTheAnswer()
    {
        var answer = new byte[64];
        new byte[] { 0x87, 0x0F, 0x01, 0x02, 0x18, 0x00, 0x00, 0x00 }.CopyTo(answer, 0);

        Assert.True(Ry5088Packets.TryParseLighting(answer, out var lighting));
        Assert.Equal(LightingEffect.CircleWave, lighting.Effect);
        Assert.Equal(3, lighting.Speed);
        Assert.Equal(2, lighting.Brightness);
        Assert.Equal(1, lighting.Option);
        Assert.True(lighting.Rainbow);
    }

    [Fact]
    public void TryParseLighting_MapsVendorWhiteAndPresetColours()
    {
        var white = new byte[64];
        new byte[] { 0x87, 0x01, 0x00, 0x04, 0x07, 0xFA, 0xFF, 0xFA }.CopyTo(white, 0);
        Assert.True(Ry5088Packets.TryParseLighting(white, out var a));
        Assert.Equal(RgbColor.White, a.Color);
        Assert.False(a.Rainbow);

        // Low nibble 5 = preset blue, chosen on the keyboard with an Fn shortcut; the colour bytes are stale then.
        var preset = new byte[64];
        new byte[] { 0x87, 0x01, 0x00, 0x04, 0x05, 0x11, 0x22, 0x33 }.CopyTo(preset, 0);
        Assert.True(Ry5088Packets.TryParseLighting(preset, out var b));
        Assert.Equal(new RgbColor(0x00, 0x00, 0xFF), b.Color);
    }

    [Fact]
    public void TryParseLighting_RejectsOtherAnswers()
    {
        Assert.False(Ry5088Packets.TryParseLighting(new byte[64], out _));
        Assert.False(Ry5088Packets.TryParseLighting(new byte[] { 0x87, 0x01 }, out _));
    }

    [Fact]
    public void LooksLike_IgnoresFieldsTheEffectDoesNotUse()
    {
        var on = new LightingSettings { Effect = LightingEffect.AlwaysOn, Speed = 1, Color = new RgbColor(1, 2, 3) };
        Assert.True(on.LooksLike(on with { Speed = 4, Option = 3 }));
        Assert.False(on.LooksLike(on with { Color = new RgbColor(1, 2, 4) }));
        Assert.False(on.LooksLike(on with { Brightness = 1 }));

        var rainbow = new LightingSettings { Effect = LightingEffect.Meteor, Rainbow = true };
        Assert.True(rainbow.LooksLike(rainbow with { Color = new RgbColor(9, 9, 9) }));
        Assert.False(rainbow.LooksLike(rainbow with { Speed = 0 }));

        var off = new LightingSettings { Effect = LightingEffect.Off };
        Assert.True(off.LooksLike(off with { Brightness = 0, Rainbow = true }));
        Assert.False(off.LooksLike(on));
    }

    [Fact]
    public async Task WriteLighting_SendsBit8AndVerifies()
    {
        var fake = new FakeRy5088Device();
        var keyboard = await Sk75Keyboard.TryConnectAsync(fake, false, NullLogger.Instance);
        var wanted = new LightingSettings { Effect = LightingEffect.Breathing, Brightness = 2, Speed = 3, Color = new RgbColor(0, 0x80, 0xFF) };

        var result = await keyboard!.WriteLightingAsync(wanted);

        Assert.True(result.Success, result.Message);
        Assert.Equal(1, fake.LightingWrites);
        var sent = fake.SentPackets.Single(p => p[0] == Opcode.SetLighting);
        Assert.True(Checksum.IsValid(sent, ChecksumKind.Bit8));
        Assert.True((await keyboard.ReadLightingAsync())!.LooksLike(wanted));
    }

    [Fact]
    public async Task WriteLighting_SkipsWhenTheKeyboardAlreadyShowsIt()
    {
        var fake = new FakeRy5088Device();
        var keyboard = await Sk75Keyboard.TryConnectAsync(fake, false, NullLogger.Instance);
        var current = await keyboard!.ReadLightingAsync();

        var result = await keyboard.WriteLightingAsync(current!);

        Assert.True(result.Success);
        Assert.Equal(0, fake.LightingWrites);
    }

    [Fact]
    public async Task WriteLighting_ReportsAFirmwareThatIgnoresIt()
    {
        var fake = new FakeRy5088Device { IgnoreWrites = true };
        var keyboard = await Sk75Keyboard.TryConnectAsync(fake, false, NullLogger.Instance);

        var result = await keyboard!.WriteLightingAsync(new LightingSettings { Effect = LightingEffect.Off });

        Assert.False(result.Success);
    }

    [Fact]
    public void Catalogue_CoversEveryOfferedEffectOnce()
    {
        Assert.Equal(Sk75Lighting.Effects.Count, Sk75Lighting.Effects.Select(e => e.Effect).Distinct().Count());
        Assert.Equal(Enum.GetValues<LightingEffect>().Length, Sk75Lighting.Effects.Count);
        Assert.All(Sk75Lighting.Effects, e => Assert.True(e.Options.Count <= 16)); // the option is a nibble
    }

    [Fact]
    public void Profile_LightingRoundTripsThroughJsonAndClones()
    {
        var profile = new Profile
        {
            Name = "Night",
            Lighting = new LightingSettings { Effect = LightingEffect.Wave, Color = new RgbColor(0xFF, 0x80, 0x00), Option = 1 },
        };

        var json = JsonStore.Serialize(profile);
        Assert.Contains("\"#FF8000\"", json);
        Assert.Contains("\"Wave\"", json);
        var back = JsonStore.Deserialize<Profile>(json)!;
        Assert.Equal(profile.Lighting, back.Lighting);
        Assert.Equal(profile.Lighting, profile.Clone("copy").Lighting);

        var withoutLighting = JsonStore.Serialize(new Profile());
        Assert.DoesNotContain("lighting", withoutLighting);
    }
}
