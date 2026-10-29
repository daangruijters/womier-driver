using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using WomierDriver.Core.Device;
using WomierDriver.Core.Protocol;

namespace WomierDriver.App.Services;

/// <summary>
/// Listens to Windows Raw Input for the SK75's own keyboard collections and reports per-slot down/up transitions
/// on the UI thread. It is registered only while the live view is on and the window is visible, and nothing it
/// sees is stored or logged.
/// </summary>
internal sealed class RawKeyboardInput : IDisposable
{
    private const int WmInput = 0x00FF;
    private const uint RimTypeKeyboard = 1;
    private const uint RidInput = 0x10000003;
    private const uint RidiDeviceName = 0x20000007;
    private const uint RidevInputSink = 0x00000100;
    private const uint RidevRemove = 0x00000001;
    private const ushort RiKeyBreak = 0x01;
    private const ushort RiKeyE0 = 0x02;
    private const ushort RiKeyE1 = 0x04;
    private const ushort KeyboardUsagePage = 0x01;
    private const ushort KeyboardUsage = 0x06;

    private static readonly string[] OwnDeviceFragments = ["VID_3151&PID_5030", "VID_3151&PID_5038"];

    private readonly HwndSource _source;
    private readonly Dictionary<IntPtr, bool> _deviceIsOurs = new();
    private readonly bool[] _down = new bool[MagnetismStreams.SlotCount];
    private readonly byte[] _buffer = new byte[64];
    private readonly uint _headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
    private bool _registered;
    private bool _disposed;

    public RawKeyboardInput(HwndSource source)
    {
        _source = source;
        _source.AddHook(WndProc);
    }

    /// <summary>Matrix slot and whether the key went down. Raised on the UI thread, for transitions only.</summary>
    public event Action<int, bool>? KeyChanged;

    public bool IsRegistered => _registered;

    public void Register()
    {
        if (_disposed || _registered)
        {
            return;
        }

        var devices = new[] { new RawInputDevice { UsagePage = KeyboardUsagePage, Usage = KeyboardUsage, Flags = RidevInputSink, Target = _source.Handle } };
        if (!RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf<RawInputDevice>()))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "RegisterRawInputDevices failed");
        }

        _registered = true;
    }

    public void Unregister()
    {
        if (!_registered)
        {
            return;
        }

        var devices = new[] { new RawInputDevice { UsagePage = KeyboardUsagePage, Usage = KeyboardUsage, Flags = RidevRemove, Target = IntPtr.Zero } };
        RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf<RawInputDevice>());
        _registered = false;
        ReleaseAll();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Unregister();
        _disposed = true;
        _source.RemoveHook(WndProc);
    }

    private void ReleaseAll()
    {
        for (var slot = 0; slot < _down.Length; slot++)
        {
            if (_down[slot])
            {
                _down[slot] = false;
                KeyChanged?.Invoke(slot, false);
            }
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmInput && _registered)
        {
            HandleInput(lParam);
        }

        // Never mark WM_INPUT as handled: DefWindowProc must still see it.
        return IntPtr.Zero;
    }

    private void HandleInput(IntPtr rawInput)
    {
        var size = (uint)_buffer.Length;
        var copied = GetRawInputData(rawInput, RidInput, _buffer, ref size, _headerSize);
        if (copied == uint.MaxValue || copied < Marshal.SizeOf<RawInputKeyboard>())
        {
            return;
        }

        var input = MemoryMarshal.Read<RawInputKeyboard>(_buffer);
        if (input.Header.Type != RimTypeKeyboard || input.Header.Device == IntPtr.Zero || !IsOurKeyboard(input.Header.Device))
        {
            return;
        }

        var key = input.Keyboard;
        if (key.VKey == 0xFF || (key.Flags & RiKeyE1) != 0 || key.MakeCode == 0)
        {
            return;
        }

        var extended = (key.Flags & RiKeyE0) != 0;
        if (extended && key.MakeCode is 0x2A or 0x36)
        {
            return; // fake shifts Windows adds around navigation keys
        }

        if (!ScanCodeMap.TryGetSlot(key.MakeCode, extended, out var slot))
        {
            return;
        }

        var isDown = (key.Flags & RiKeyBreak) == 0;
        if (_down[slot] == isDown)
        {
            return; // typematic repeat
        }

        _down[slot] = isDown;
        KeyChanged?.Invoke(slot, isDown);
    }

    private bool IsOurKeyboard(IntPtr device)
    {
        if (_deviceIsOurs.TryGetValue(device, out var ours))
        {
            return ours;
        }

        var name = DeviceName(device);
        ours = name is not null && OwnDeviceFragments.Any(f => name.Contains(f, StringComparison.OrdinalIgnoreCase));
        _deviceIsOurs[device] = ours;
        return ours;
    }

    private static string? DeviceName(IntPtr device)
    {
        uint size = 0;
        if (GetRawInputDeviceInfoW(device, RidiDeviceName, null, ref size) == uint.MaxValue || size == 0)
        {
            return null;
        }

        var chars = new char[size];
        var copied = GetRawInputDeviceInfoW(device, RidiDeviceName, chars, ref size);
        if (copied == uint.MaxValue || copied == 0)
        {
            return null;
        }

        return new string(chars, 0, (int)Math.Min(copied, chars.Length)).TrimEnd('\0');
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawKeyboard
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputKeyboard
    {
        public RawInputHeader Header;
        public RawKeyboard Keyboard;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterRawInputDevices([In] RawInputDevice[] devices, uint count, uint size);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(IntPtr rawInput, uint command, [Out] byte[]? data, ref uint size, uint headerSize);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetRawInputDeviceInfoW(IntPtr device, uint command, [Out] char[]? data, ref uint size);
}
