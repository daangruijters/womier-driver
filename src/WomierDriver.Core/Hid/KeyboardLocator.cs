using HidSharp;

namespace WomierDriver.Core.Hid;

/// <summary>One HID collection that speaks the RY5088 configuration protocol.</summary>
public sealed record HidInterfaceInfo(string Path, int VendorId, int ProductId, bool IsDongle, HidDevice Device);

/// <summary>Finds the SK75 TMR configuration interface among the connected HID devices.</summary>
public static class KeyboardLocator
{
    /// <summary>USB vendor id used by the RY5088 chip family (Womier, Akko, MonsGeek, ...).</summary>
    public const int VendorId = 0x3151;

    /// <summary>Product id of the keyboard on its USB cable. Shared by many RY5088 boards; the model is confirmed by its device id.</summary>
    public const int WiredProductId = 0x5030;

    /// <summary>Product id of the 2.4 GHz receiver.</summary>
    public const int DongleProductId = 0x5038;

    /// <summary>Vendor usage page of the configuration collection.</summary>
    public const int VendorUsagePage = 0xFFFF;

    /// <summary>Usage of the configuration collection (64-byte feature reports). Usage 1 is the 32-byte input-report collection.</summary>
    public const int ConfigurationUsage = 0x0002;

    /// <summary>Lists candidate configuration interfaces, wired ones first.</summary>
    public static IReadOnlyList<HidInterfaceInfo> FindConfigurationInterfaces()
    {
        var result = new List<HidInterfaceInfo>();
        foreach (var device in DeviceList.Local.GetHidDevices(VendorId))
        {
            if (device.ProductID != WiredProductId && device.ProductID != DongleProductId)
            {
                continue;
            }

            if (!IsConfigurationCollection(device))
            {
                continue;
            }

            result.Add(new HidInterfaceInfo(device.DevicePath, device.VendorID, device.ProductID, device.ProductID == DongleProductId, device));
        }

        return result.OrderBy(i => i.IsDongle ? 1 : 0).ThenBy(i => i.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Invokes <paramref name="onChanged"/> whenever the system's HID device list changes (plug / unplug).</summary>
    public static IDisposable Subscribe(Action onChanged)
    {
        EventHandler<DeviceListChangedEventArgs> handler = (_, _) => onChanged();
        DeviceList.Local.Changed += handler;
        return new Subscription(() => DeviceList.Local.Changed -= handler);
    }

    private static bool IsConfigurationCollection(HidDevice device)
    {
        int maxFeature;
        try
        {
            maxFeature = device.GetMaxFeatureReportLength();
        }
        catch
        {
            return false;
        }

        if (maxFeature < 9)
        {
            return false;
        }

        try
        {
            var descriptor = device.GetReportDescriptor();
            foreach (var item in descriptor.DeviceItems)
            {
                foreach (var usage in item.Usages.GetAllValues())
                {
                    if ((usage >> 16) == VendorUsagePage && (usage & 0xFFFF) == ConfigurationUsage)
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
            return device.DevicePath.Contains("mi_02", StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
