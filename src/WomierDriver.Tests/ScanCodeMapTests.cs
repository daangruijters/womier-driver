using WomierDriver.Core.Device;
using Xunit;

namespace WomierDriver.Tests;

public class ScanCodeMapTests
{
    [Fact]
    public void EveryKeyExceptFnHasAScanCodeThatMapsBackToIt()
    {
        foreach (var key in Sk75TmrLayout.Keys)
        {
            if (key.Code == "Fn")
            {
                Assert.False(ScanCodeMap.TryGetScanCode(key.HidUsage, out _, out _));
                continue;
            }

            Assert.True(ScanCodeMap.TryGetScanCode(key.HidUsage, out var makeCode, out var extended), key.Code);
            Assert.True(ScanCodeMap.TryGetSlot(makeCode, extended, out var slot), key.Code);
            Assert.Equal(key.Slot, slot);
        }
    }

    [Theory]
    [InlineData(0x11, false, "KeyW")]
    [InlineData(0x48, true, "ArrowUp")]
    [InlineData(0x1D, true, "ControlRight")]
    [InlineData(0x1D, false, "ControlLeft")]
    [InlineData(0x5B, true, "MetaLeft")]
    [InlineData(0x39, false, "Space")]
    [InlineData(0x53, true, "Delete")]
    [InlineData(0x58, false, "F12")]
    public void RawInputMakeCodes_MapToTheRightKey(int makeCode, bool extended, string code)
    {
        Assert.True(ScanCodeMap.TryGetSlot((ushort)makeCode, extended, out var slot));
        Assert.Equal(code, Sk75TmrLayout.BySlot(slot)!.Code);
    }

    [Theory]
    [InlineData(0x2A, true)]  // fake shift Windows adds around navigation keys
    [InlineData(0x36, true)]
    [InlineData(0x00, false)]
    [InlineData(0x52, true)]  // Insert: valid scan code, but the SK75 has no Insert key
    public void UnknownOrForeignCodes_DoNotMap(int makeCode, bool extended)
    {
        Assert.False(ScanCodeMap.TryGetSlot((ushort)makeCode, extended, out _));
    }

    [Fact]
    public void ByHidUsage_IsConsistentWithTheLayout()
    {
        Assert.Equal("KeyW", Sk75TmrLayout.ByHidUsage(0x1A)!.Code);
        Assert.Equal("ShiftRight", Sk75TmrLayout.ByHidUsage(0xE5)!.Code);
        Assert.Null(Sk75TmrLayout.ByHidUsage(0x00));
        Assert.Null(Sk75TmrLayout.ByHidUsage(0x49));
    }
}
