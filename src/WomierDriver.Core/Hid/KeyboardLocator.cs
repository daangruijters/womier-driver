using HidSharp;

namespace WomierDriver.Core.Hid;

/// <summary>One HID collection of the keyboard: the configuration collection or the vendor input collection.</summary>
public sealed record HidInterfaceInfo(string Path, int VendorId, int ProductId, bool IsDongle, HidDevice Device);

/// <summary>Finds the SK75 TMR's HID collections among the connected devices.</summary>
public static class KeyboardLocator
{
    /// <summary>USB vendor id used by the RY5088 chip family (Womier, Akko, MonsGeek, ...).</summary>
    public const int VendorId = 0x3151;

    /// <summary>Product id of the keyboard on its USB cable. Shared by many RY5088 boards; the model is confirmed by its device id.</summary>
    public const int WiredProductId = 0x5030;

    /// <summary>Product id of the 2.4 GHz receiver.</summary>
    public const int DongleProductId = 0x5038;

    /// <summary>Vendor usage page of both vendor collections.</summary>
    public const int VendorUsagePage = 0xFFFF;

    /// <summary>Usage of the configuration collection (64-byte feature reports, interface MI_02).</summary>
    public const int ConfigurationUsage = 0x0002;

    /// <summary>
    /// Usage of the vendor input collection (32-byte input reports, interface MI_01): device notifications and,
    /// when enabled, live travel reports. See <see cref="Protocol.VendorInputReports"/>.
    /// </summary>
    public const int InputUsage = 0x0001;

    /// <summary>Lists candidate configuration interfaces, wired ones first.</summary>
    public static IReadOnlyList<HidInterfaceInfo> FindConfigurationInterfaces() => Find(IsConfigurationCollection, productId: null);

    /// <summary>
    /// Lists the vendor input collections, wired ones first. Windows exposes the collection as its own device whose
    /// path does not reveal the parent keyboard, so the collection with the same product id as the configuration
    /// interface is taken to belong to the same keyboard; with one keyboard attached that is unambiguous.
    /// </summary>
    public static IReadOnlyList<HidInterfaceInfo> FindVendorInputInterfaces(int? productId = null) => Find(IsVendorInputCollection, productId);

    /// <summary>Invokes <paramref name="onChanged"/> whenever the system's HID device list changes (plug / unplug).</summary>
    public static IDisposable Subscribe(Action onChanged)
    {
        EventHandler<DeviceListChangedEventArgs> handler = (_, _) => onChanged();
        DeviceList.Local.Changed += handler;
        return new Subscription(() => DeviceList.Local.Changed -= handler);
    }

    private static IReadOnlyList<HidInterfaceInfo> Find(Func<HidDevice, bool> matches, int? productId)
    {
        var result = new List<HidInterfaceInfo>();
        foreach (var device in DeviceList.Local.GetHidDevices(VendorId))
        {
            if (device.ProductID != WiredProductId && device.ProductID != DongleProductId)
            {
                continue;
            }

            if (productId is not null && device.ProductID != productId)
            {
                continue;
            }

            if (!matches(device))
            {
                continue;
            }

            result.Add(new HidInterfaceInfo(device.DevicePath, device.VendorID, device.ProductID, device.ProductID == DongleProductId, device));
        }

        return result.OrderBy(i => i.IsDongle ? 1 : 0).ThenBy(i => i.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool IsConfigurationCollection(HidDevice device)
        => SafeLength(device.GetMaxFeatureReportLength) >= 9 && HasVendorUsage(device, ConfigurationUsage, "mi_02");

    private static bool IsVendorInputCollection(HidDevice device)
        => SafeLength(device.GetMaxInputReportLength) >= 2 && HasVendorUsage(device, InputUsage, "mi_01&col05");

    private static int SafeLength(Func<int> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return 0;
        }
    }

    private static bool HasVendorUsage(HidDevice device, int usage, string fallbackPathFragment)
    {
        try
        {
            var descriptor = device.GetReportDescriptor();
            foreach (var item in descriptor.DeviceItems)
            {
                foreach (var value in item.Usages.GetAllValues())
                {
                    if ((value >> 16) == VendorUsagePage && (value & 0xFFFF) == usage)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            // Fall back to the interface number if the descriptor cannot be read.
            return device.DevicePath.Contains(fallbackPathFragment, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
