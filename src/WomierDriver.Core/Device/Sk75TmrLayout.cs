using System.Collections.Frozen;

namespace WomierDriver.Core.Device;

/// <summary>Static description of the Womier SK75 TMR: identity, geometry and key-matrix slots.</summary>
public static partial class Sk75TmrLayout
{
    public const string ModelName = "Womier SK75 TMR";

    /// <summary>Model id reported in the firmware-version response (bytes 1..4). Matches the vendor catalogue entry "SK75 TMR".</summary>
    public const uint DeviceId = 2518;

    /// <summary>Width of the key canvas in layout units.</summary>
    public const double CanvasWidth = 732;

    /// <summary>Height of the key canvas in layout units.</summary>
    public const double CanvasHeight = 281;

    private static readonly KeyDefinition[] KeyArray = BuildKeys();

    private static readonly FrozenDictionary<int, KeyDefinition> BySlotMap = KeyArray.ToFrozenDictionary(k => k.Slot);

    private static readonly FrozenDictionary<string, KeyDefinition> ByCodeMap = KeyArray.ToFrozenDictionary(k => k.Code, StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<byte, KeyDefinition> ByUsageMap = KeyArray.Where(k => k.HidUsage != 0).ToFrozenDictionary(k => k.HidUsage);

    /// <summary>All 81 keys, ordered by matrix slot.</summary>
    public static IReadOnlyList<KeyDefinition> Keys => KeyArray;

    /// <summary>Matrix slots that have a physical key.</summary>
    public static IReadOnlySet<int> Slots { get; } = KeyArray.Select(k => k.Slot).ToFrozenSet();

    public static KeyDefinition? BySlot(int slot) => BySlotMap.GetValueOrDefault(slot);

    public static KeyDefinition? ByCode(string code) => ByCodeMap.GetValueOrDefault(code);

    /// <summary>Key that sends <paramref name="usage"/> in the factory keymap; <c>null</c> for usage 0 (Fn) and usages the board does not have.</summary>
    public static KeyDefinition? ByHidUsage(byte usage) => ByUsageMap.GetValueOrDefault(usage);

    public static IEnumerable<KeyDefinition> InGroup(KeyGroup group) => KeyArray.Where(k => k.IsIn(group));
}
