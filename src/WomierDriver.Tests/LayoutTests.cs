using WomierDriver.Core.Device;
using Xunit;

namespace WomierDriver.Tests;

public class LayoutTests
{
    [Fact]
    public void HasEightyOneKeysWithUniqueSlotsAndCodes()
    {
        Assert.Equal(81, Sk75TmrLayout.Keys.Count);
        Assert.Equal(81, Sk75TmrLayout.Keys.Select(k => k.Slot).Distinct().Count());
        Assert.Equal(81, Sk75TmrLayout.Keys.Select(k => k.Code).Distinct().Count());
        Assert.All(Sk75TmrLayout.Keys, k => Assert.InRange(k.Slot, 0, 127));
    }

    [Fact]
    public void KeysFitInsideTheCanvas()
    {
        Assert.All(Sk75TmrLayout.Keys, k =>
        {
            Assert.True(k.X >= 0 && k.X + k.Width <= Sk75TmrLayout.CanvasWidth, k.Code);
            Assert.True(k.Y >= 0 && k.Y + k.Height <= Sk75TmrLayout.CanvasHeight, k.Code);
        });
    }

    [Fact]
    public void KeysDoNotOverlap()
    {
        var keys = Sk75TmrLayout.Keys;
        for (var i = 0; i < keys.Count; i++)
        {
            for (var j = i + 1; j < keys.Count; j++)
            {
                var a = keys[i];
                var b = keys[j];
                var overlap = a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
                Assert.False(overlap, $"{a.Code} overlaps {b.Code}");
            }
        }
    }

    [Theory]
    [InlineData(0, "Escape", 0x29)]
    [InlineData(9, "KeyA", 0x04)]
    [InlineData(14, "KeyW", 0x1A)]
    [InlineData(41, "Space", 0x2C)]
    [InlineData(65, "Fn", 0x00)]
    [InlineData(81, "Enter", 0x28)]
    [InlineData(89, "ArrowRight", 0x4F)]
    public void WellKnownSlotsMatchTheFactoryMatrix(int slot, string code, int hid)
    {
        var key = Sk75TmrLayout.BySlot(slot);
        Assert.NotNull(key);
        Assert.Equal(code, key.Code);
        Assert.Equal(hid, key.HidUsage);
        Assert.Same(key, Sk75TmrLayout.ByCode(code));
    }

    [Fact]
    public void GroupsCoverTheExpectedKeys()
    {
        Assert.Equal(26, Sk75TmrLayout.InGroup(KeyGroup.Letters).Count());
        Assert.Equal(10, Sk75TmrLayout.InGroup(KeyGroup.Digits).Count());
        Assert.Equal(12, Sk75TmrLayout.InGroup(KeyGroup.FunctionRow).Count());
        Assert.Equal(4, Sk75TmrLayout.InGroup(KeyGroup.Wasd).Count());
        Assert.Equal(4, Sk75TmrLayout.InGroup(KeyGroup.Arrows).Count());
        Assert.Equal(6, Sk75TmrLayout.InGroup(KeyGroup.Modifiers).Count());
        Assert.Equal(5, Sk75TmrLayout.InGroup(KeyGroup.Navigation).Count());
        Assert.Equal(11, Sk75TmrLayout.InGroup(KeyGroup.Symbols).Count());
    }
}
