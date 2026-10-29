using WomierDriver.Core.Protocol;

namespace WomierDriver.Core.Device;

/// <summary>Identity and capabilities of a connected keyboard, captured when the connection is established.</summary>
public sealed record KeyboardInfo(
    string DevicePath,
    bool IsDongle,
    uint DeviceId,
    int UsbVersion,
    int RfVersion,
    byte? OnboardProfile,
    TravelScale Scale,
    bool SupportsTopDeadZone)
{
    public string ModelName => Sk75TmrLayout.ModelName;

    public string ConnectionLabel => IsDongle ? "2.4 GHz" : "USB";

    public string FirmwareLabel => new DeviceIdentity(DeviceId, UsbVersion).VersionLabel;

    public override string ToString()
        => $"{ModelName} via {ConnectionLabel}, firmware {FirmwareLabel} ({UsbVersion}), rf {RfVersion}, onboard profile {OnboardProfile?.ToString() ?? "?"}, {Scale.UnitsPerMm} units/mm";
}
