using System.Diagnostics;
using System.Globalization;
using WomierDriver.Core.Device;
using WomierDriver.Core.Hid;
using WomierDriver.Core.Logging;
using WomierDriver.Core.Profiles;
using WomierDriver.Core.Protocol;

// Command-line diagnostics for the SK75 TMR. Read-only unless you use "set", "fill" or "light <effect>".
//
//   hidprobe                     list candidate interfaces and identify the keyboard
//   hidprobe dump                print every key's current settings
//   hidprobe raw <stream>        hex dump of one per-key table (0..10, 251, 252, 254)
//   hidprobe set <code> <mm>     set one key's actuation point (e.g. "set KeyW 1.2"), then read it back
//   hidprobe fill <stream> <raw> overwrite a whole per-key table with one raw value (recovery tool, use with care)
//   hidprobe watch               print plug / unplug events until Enter is pressed
//   hidprobe receiver            status of the 2.4 GHz receiver and its keyboard (works while the keyboard sleeps)
//   hidprobe live                switch on the firmware's live travel reports and print them until Enter is pressed
//   hidprobe travel [seconds]    poll the live travel table (254) and print keys whose value changes
//   hidprobe light               print the backlight setting (raw answer and decoded)
//   hidprobe light <effect> [brightness] [speed] [#RRGGBB|rainbow] [option]
//                                set the backlight (effect by name, e.g. Wave, or number), then read it back
//   --fast                       with any command: 1 ms instead of 10 ms between a request and reading its answer
//   --trace                      with any command over the receiver: log every receiver status while waiting

var log = new ConsoleLogger();
var fast = args.Any(a => a.Equals("--fast", StringComparison.OrdinalIgnoreCase));
var trace = args.Any(a => a.Equals("--trace", StringComparison.OrdinalIgnoreCase));
args = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
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

if (command == "receiver")
{
    // Receiver-local queries only: they work while the keyboard sleeps and never reach the keyboard.
    var receiver = interfaces.FirstOrDefault(i => i.IsDongle);
    if (receiver is null)
    {
        Console.WriteLine("No 2.4 GHz receiver found.");
        return 1;
    }

    using var raw = HidSharpFeatureChannel.Open(receiver.Device, ReceiverRelayChannel.ReceiverReadDelayMs);
    var version = await raw.ExchangeAsync(Ry5088Packets.Command(Opcode.ReceiverGetVersion));
    Console.WriteLine($"Receiver firmware: {version[1] | (version[2] << 8)} (answer {Convert.ToHexString(version, 0, 8)})");
    using var relay = new ReceiverRelayChannel(raw, log);
    var status = await relay.GetStatusAsync();
    Console.WriteLine($"Keyboard {(status.KeyboardConnected ? "connected" : "asleep or not connected")}, battery {status.BatteryPercent} %, answer ready {status.AnswerReady}, command delivered {status.CommandDelivered}");
    return 0;
}

Sk75Keyboard? keyboard = null;
IFeatureChannel? keyboardChannel = null;
foreach (var iface in interfaces)
{
    var raw = HidSharpFeatureChannel.Open(iface.Device, iface.IsDongle ? ReceiverRelayChannel.ReceiverReadDelayMs : fast ? 1 : 10);
    IFeatureChannel channel = iface.IsDongle ? new ReceiverRelayChannel(raw, log) { Trace = trace } : raw;
    try
    {
        keyboard = await Sk75Keyboard.TryConnectAsync(channel, iface.IsDongle, log);
    }
    catch (KeyboardAsleepException ex)
    {
        Console.WriteLine($"Receiver: {ex.Message}");
    }

    if (keyboard is not null)
    {
        keyboardChannel = channel;
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

        case "live":
        {
            var productId = keyboard.Info.IsDongle ? KeyboardLocator.DongleProductId : KeyboardLocator.WiredProductId;
            var inputs = KeyboardLocator.FindVendorInputInterfaces(productId);
            Console.WriteLine();
            Console.WriteLine($"Vendor input collections for PID 0x{productId:X4}: {inputs.Count}");
            foreach (var input in inputs)
            {
                Console.WriteLine($"  {input.Path}");
            }

            if (inputs.Count == 0)
            {
                Console.WriteLine("No vendor input collection found.");
                return 1;
            }

            string usesIds;
            try
            {
                usesIds = inputs[0].Device.GetReportDescriptor().ReportsUseID.ToString();
            }
            catch
            {
                usesIds = "?";
            }

            using var channel = HidSharpInputReportChannel.Open(inputs[0].Device);
            Console.WriteLine($"Opened {channel.DevicePath}; max input report {channel.ReportLength} bytes incl. id; reports use ids: {usesIds}");
            var clock = Stopwatch.StartNew();
            long last = 0;
            var count = 0;
            channel.ReportReceived += report =>
            {
                var now = clock.ElapsedMilliseconds;
                var delta = now - last;
                last = now;
                count++;
                var decoded = VendorInputReports.TryParseTravel(report, out var raw, out var slot)
                    ? string.Create(CultureInfo.InvariantCulture, $"  slot {slot,3} {Sk75TmrLayout.BySlot(slot)?.Code ?? "(no key)",-12} raw {raw,4} = {keyboard.Info.Scale.ToMm(raw):0.00} mm")
                    : string.Empty;
                Console.WriteLine($"{now,7} ms (+{delta,4})  {Convert.ToHexString(report)}  {VendorInputReports.Describe(report)}{decoded}");
            };
            channel.Stopped += ex => Console.WriteLine($"Input channel stopped: {(ex is null ? "disposed" : ex.GetType().Name + ": " + ex.Message)}");
            channel.Start();

            Console.WriteLine("Listening 3 s before enabling anything (reports arriving now were already switched on)...");
            await Task.Delay(3000);
            Console.WriteLine("Sending 1B 01 (live travel on). Press keys; Enter stops and sends 1B 00.");
            Console.WriteLine("Script: one key slowly to the bottom and back; hold W and press A; release both; type a sentence;");
            Console.WriteLine("        from another console run 'hidprobe set KeyW 2.8' to see whether the stream survives a commit.");
            await keyboard.SetLiveReportingAsync(true);
            var listenSeconds = args.Length > 1 && int.TryParse(args[1], out var parsedSeconds) ? parsedSeconds : 0;
            if (listenSeconds > 0)
            {
                Console.WriteLine($"Listening for {listenSeconds} s...");
                await Task.Delay(listenSeconds * 1000);
            }
            else
            {
                Console.ReadLine();
            }

            await keyboard.SetLiveReportingAsync(false);
            await keyboard.SetLiveReportingAsync(false);
            var before = count;
            Console.WriteLine($"Stream off after {count} reports. Listening 2 s more to confirm silence...");
            await Task.Delay(2000);
            Console.WriteLine($"Reports after switching off: {count - before}. Total: {count}.");
            return 0;
        }

        case "travel":
        {
            var seconds = args.Length > 1 && int.TryParse(args[1], out var parsed) ? parsed : 10;
            Console.WriteLine();
            Console.WriteLine($"Polling table 254 for {seconds} s with a {(fast ? 1 : 10)} ms read delay. No calibration commands are sent. Press keys.");
            var baseline = await keyboard.ReadLiveTravelAsync();
            Console.WriteLine("Baseline raw values: " + string.Join(" ", Sk75TmrLayout.Keys.Select(k => $"{k.Code}={baseline[k.Slot]}")));
            var previous = baseline;
            var clock = Stopwatch.StartNew();
            var sweeps = 0;
            var changes = 0;
            long slowest = 0;
            while (clock.Elapsed.TotalSeconds < seconds)
            {
                var sweep = Stopwatch.StartNew();
                var current = await keyboard.ReadLiveTravelAsync();
                sweep.Stop();
                sweeps++;
                slowest = Math.Max(slowest, sweep.ElapsedMilliseconds);
                for (var slot = 0; slot < current.Length; slot++)
                {
                    if (current[slot] == previous[slot])
                    {
                        continue;
                    }

                    changes++;
                    var code = Sk75TmrLayout.BySlot(slot)?.Code ?? "(no key)";
                    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                        $"{clock.ElapsedMilliseconds,7} ms  slot {slot,3} {code,-12} raw {current[slot],4} = {keyboard.Info.Scale.ToMm(current[slot]):0.00} mm  (sweep {sweep.ElapsedMilliseconds} ms)"));
                }

                previous = current;
            }

            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{sweeps} sweeps in {clock.Elapsed.TotalSeconds:0.0} s = {sweeps / clock.Elapsed.TotalSeconds:0.0} Hz (slowest {slowest} ms); {changes} changes."));
            return 0;
        }

        case "light":
        {
            var answer = await keyboardChannel!.ExchangeAsync(Ry5088Packets.GetLighting());
            Console.WriteLine();
            Console.WriteLine($"Raw answer: {Convert.ToHexString(answer, 0, 16)}");
            var current = await keyboard.ReadLightingAsync();
            Console.WriteLine($"Lighting:   {(current is null ? "not recognised" : FormatLighting(current))}");
            if (args.Length < 2)
            {
                return current is null ? 1 : 0;
            }

            if (!TryParseEffect(args[1], out var effect))
            {
                Console.WriteLine("usage: hidprobe light <effect> [brightness 0-4] [speed 0-4] [#RRGGBB|rainbow] [option]");
                Console.WriteLine("effects: " + string.Join(", ", Sk75Lighting.Effects.Select(e => $"{e.Effect} ({(int)e.Effect})")));
                return 2;
            }

            var wanted = (current ?? new LightingSettings()) with { Effect = effect };
            if (args.Length > 2)
            {
                wanted = wanted with { Brightness = int.Parse(args[2], CultureInfo.InvariantCulture) };
            }

            if (args.Length > 3)
            {
                wanted = wanted with { Speed = int.Parse(args[3], CultureInfo.InvariantCulture) };
            }

            if (args.Length > 4)
            {
                if (args[4].Equals("rainbow", StringComparison.OrdinalIgnoreCase))
                {
                    wanted = wanted with { Rainbow = true };
                }
                else if (RgbColor.TryParse(args[4], out var color))
                {
                    wanted = wanted with { Rainbow = false, Color = color };
                }
            }

            if (args.Length > 5)
            {
                wanted = wanted with { Option = int.Parse(args[5], CultureInfo.InvariantCulture) };
            }

            Console.WriteLine($"Setting:    {FormatLighting(wanted.Sanitized())}  packet {Convert.ToHexString(Ry5088Packets.SetLighting(wanted), 0, 9)} (+ checksum)");
            var result = await keyboard.WriteLightingAsync(wanted);
            Console.WriteLine($"Write: {(result.Success ? "ok" : "FAILED")} - {result.Message} ({result.Duration.TotalMilliseconds:0} ms)");
            answer = await keyboardChannel.ExchangeAsync(Ry5088Packets.GetLighting());
            Console.WriteLine($"Raw answer: {Convert.ToHexString(answer, 0, 16)}");
            return result.Success ? 0 : 1;
        }

        default:
            Console.WriteLine($"Unknown command '{command}'.");
            return 2;
    }
}

static string FormatLighting(LightingSettings s)
    => $"{s.Effect} ({(int)s.Effect}), brightness {s.Brightness}, speed {s.Speed}, {(s.Rainbow ? "rainbow" : s.Color.ToString())}, option {s.Option}";

static bool TryParseEffect(string text, out LightingEffect effect)
{
    if (byte.TryParse(text, out var value))
    {
        effect = (LightingEffect)value;
        return true;
    }

    return Enum.TryParse(text, ignoreCase: true, out effect);
}

internal sealed class ConsoleLogger : ILogger
{
    public void Info(string message) => Console.WriteLine($"[info] {message}");

    public void Warn(string message) => Console.WriteLine($"[warn] {message}");

    public void Error(string message, Exception? exception = null) => Console.WriteLine($"[error] {message} {exception}");
}
