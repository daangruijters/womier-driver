using System.Globalization;
using WomierDriver.Core.Device;
using WomierDriver.Core.Hid;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Profiles;
using WomierDriver.Core.Protocol;

// Command-line diagnostics for the SK75 TMR. Read-only unless you use "set".
//
//   hidprobe                     list candidate interfaces and identify the keyboard
//   hidprobe dump                print every key's current settings
//   hidprobe raw <stream>        hex dump of one per-key table (0..10, 251, 252)
//   hidprobe set <code> <mm>     set one key's actuation point (e.g. "set KeyW 1.2"), then read it back
//   hidprobe fill <stream> <raw> overwrite a whole per-key table with one raw value (recovery tool, use with care)
//   hidprobe watch               print plug / unplug events until Enter is pressed

var log = new ConsoleLogger();
var command = args.Length > 0 ? args[0].ToLowerInvariant() : "info";

var interfaces = KeyboardLocator.FindConfigurationInterfaces();
Console.WriteLine($"Candidate configuration interfaces: {interfaces.Count}");
foreach (var iface in interfaces)
{
    Console.WriteLine($"  VID 0x{iface.VendorId:X4} PID 0x{iface.ProductId:X4} {(iface.IsDongle ? "(2.4 GHz receiver)" : "(USB)")}  {iface.Path}");
}

if (command == "watch")
{
    using var subscription = KeyboardLocator.Subscribe(() =>
    {
        var now = KeyboardLocator.FindConfigurationInterfaces();
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} HID list changed; {now.Count} candidate interface(s) present");
    });
    Console.WriteLine("Watching for plug / unplug events. Press Enter to stop.");
    Console.ReadLine();
    return 0;
}

Sk75Keyboard? keyboard = null;
foreach (var iface in interfaces)
{
    var channel = HidSharpFeatureChannel.Open(iface.Device);
    keyboard = await Sk75Keyboard.TryConnectAsync(channel, iface.IsDongle, log);
    if (keyboard is not null)
    {
        break;
    }

    channel.Dispose();
}

if (keyboard is null)
{
    Console.WriteLine("No SK75 TMR answered.");
    return 1;
}

using (keyboard)
{
    Console.WriteLine();
    Console.WriteLine($"Connected: {keyboard.Info}");
    Console.WriteLine($"Limits: travel {keyboard.Limits.TravelMin}..{keyboard.Limits.TravelMax} mm, step {keyboard.Limits.StepMm} mm, top dead zone {(keyboard.Info.SupportsTopDeadZone ? "supported" : "not supported")}");

    switch (command)
    {
        case "info":
            return 0;

        case "dump":
        {
            var snapshot = await keyboard.ReadKeySettingsAsync();
            Console.WriteLine();
            Console.WriteLine($"{"slot",4} {"key",-12} {"press",6} {"release",8} {"RT",3} {"RT+",6} {"RT-",6} {"dz",5} {"switch",6}");
            foreach (var key in Sk75TmrLayout.Keys)
            {
                var s = snapshot.Keys[key.Slot];
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{key.Slot,4} {key.Code,-12} {s.ActuationMm,6:0.00} {s.ReleaseMm,8:0.00} {(s.RapidTrigger ? "on" : "off"),3} {s.RtPressMm,6:0.00} {s.RtReleaseMm,6:0.00} {s.DeadZoneMm,5:0.00} {snapshot.RawSwitchCodes[key.Slot],6}"));
            }

            return 0;
        }

        case "raw":
        {
            if (args.Length < 2 || !byte.TryParse(args[1], out var streamByte))
            {
                Console.WriteLine("usage: hidprobe raw <stream>");
                return 2;
            }

            var stream = (MagnetismStream)streamByte;
            var table = await keyboard.ReadTableAsync(stream);
            Console.WriteLine($"{stream} ({table.Length} bytes):");
            for (var i = 0; i < table.Length; i += 32)
            {
                Console.WriteLine($"  {i,4}: {Convert.ToHexString(table, i, Math.Min(32, table.Length - i))}");
            }

            return 0;
        }

        case "set":
        {
            if (args.Length < 3 || Sk75TmrLayout.ByCode(args[1]) is not { } key || !double.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var mm))
            {
                Console.WriteLine("usage: hidprobe set <KeyCode> <millimetres>   e.g. hidprobe set KeyW 1.2");
                return 2;
            }

            var before = await keyboard.ReadKeySettingsAsync();
            var current = before.Keys[key.Slot];
            Console.WriteLine($"{key.Code}: actuation {current.ActuationMm:0.00} mm -> {mm:0.00} mm");
            var result = await keyboard.WriteKeySettingsAsync(new Dictionary<int, KeySettings> { [key.Slot] = current with { ActuationMm = mm } });
            Console.WriteLine($"Write: {(result.Success ? "ok" : "FAILED")} - {result.Message} ({result.Duration.TotalMilliseconds:0} ms)");
            var after = await keyboard.ReadKeySettingsAsync();
            Console.WriteLine($"Read back: actuation {after.Keys[key.Slot].ActuationMm:0.00} mm, release {after.Keys[key.Slot].ReleaseMm:0.00} mm, RT {(after.Keys[key.Slot].RapidTrigger ? "on" : "off")}");
            return result.Success ? 0 : 1;
        }

        case "fill":
        {
            if (args.Length < 3 || !byte.TryParse(args[1], out var streamByte) || !ushort.TryParse(args[2], out var raw))
            {
                Console.WriteLine("usage: hidprobe fill <stream> <rawValue>   e.g. hidprobe fill 2 0");
                return 2;
            }

            var stream = (MagnetismStream)streamByte;
            var payload = stream.IsU16()
                ? Ry5088Packets.EncodeU16Table(Enumerable.Repeat(raw, MagnetismStreams.SlotCount).ToArray())
                : Enumerable.Repeat((byte)raw, stream.PayloadLength()).ToArray();
            var before = await keyboard.ReadTableAsync(stream);
            Console.WriteLine($"{stream}: before {Convert.ToHexString(before, 0, 16)}…");
            await keyboard.WriteTableAsync(stream, payload);
            var after = await keyboard.ReadTableAsync(stream);
            Console.WriteLine($"{stream}: after  {Convert.ToHexString(after, 0, 16)}…  {(after.AsSpan().SequenceEqual(payload) ? "ok" : "MISMATCH")}");
            return after.AsSpan().SequenceEqual(payload) ? 0 : 1;
        }

        default:
            Console.WriteLine($"Unknown command '{command}'.");
            return 2;
    }
}

internal sealed class ConsoleLogger : ILogger
{
    public void Info(string message) => Console.WriteLine($"[info] {message}");

    public void Warn(string message) => Console.WriteLine($"[warn] {message}");

    public void Error(string message, Exception? exception = null) => Console.WriteLine($"[error] {message} {exception}");
}
