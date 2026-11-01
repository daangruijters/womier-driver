# CLAUDE.md

Guidance for Claude Code (and humans) working in this repository.

## What this is

A Windows tray driver and configuration utility for the **Womier SK75 TMR** keyboard, written in C# (.NET 10).
It replaces the vendor's Electron "WOMIER Driver" for per-key actuation / rapid-trigger settings and adds
profiles that switch automatically based on running games. It talks to the keyboard directly over USB HID
feature reports; no vendor binaries are used.

## Layout

```
src/WomierDriver.Core/    protocol, HID transport (feature + input reports), device model, profiles, automation engine,
                          live travel stream (no UI, no Win32, unit tested)
src/WomierDriver.App/     WPF app: tray icon, main window, view models, Raw Input for the live view (Fluent theme,
                          CommunityToolkit.Mvvm)
src/WomierDriver.Tests/   xunit tests incl. FakeRy5088Device, an in-memory firmware for end-to-end tests
tools/HidProbe/           console diagnostics against the real keyboard (info, dump, raw, set, fill, watch)
tools/gen-layout.py       regenerates Core/Device/Sk75TmrLayout.Keys.cs from docs/reverse-engineering/vendor
docs/protocol.md          the HID protocol as used here (verified live) – read before touching Protocol/ or Device/
docs/architecture.md      components, threading model, data files
docs/reverse-engineering/ provenance: prettified vendor sources and notes
```

## Commands

```powershell
dotnet build WomierDriver.slnx                      # build everything (solution is in the .slnx format)
dotnet test src/WomierDriver.Tests                  # unit tests (no hardware needed)
dotnet run --project src/WomierDriver.App           # run the driver (add -- --minimized to start in the tray)
dotnet run --project tools/HidProbe -- dump         # read every key's current settings from the keyboard
dotnet run --project tools/HidProbe -- live 30      # print the firmware's live travel reports for 30 s
dotnet publish src/WomierDriver.App -c Release -r win-x64 --self-contained true -o publish   # portable single exe (releases)
dotnet publish src/WomierDriver.App -c Release -r win-x64 --self-contained false -o publish  # small, needs the runtime
python tools/gen-layout.py                          # regenerate the key table after changing the vendor data
```

Testing against a redirected data folder: `$env:WOMIER_DRIVER_DATA = "C:\temp\womier"` before starting the app. Such
an instance has its own single-instance scope, so it can run next to the normal driver; both talk to the same
keyboard, so do not let them apply different profiles at the same time.

## Rules that matter

* **Hardware safety.** Unit tests must never open the real device; use `FakeRy5088Device`. Anything that writes
  to the keyboard goes through `Sk75Keyboard` and keeps the vendor's sequence: modes table first, travel tables
  next, commit flag only on the last chunk of the last table, 600 ms wait, read-back verification. Do not change
  packet layouts or ordering without re-verifying on the device (`hidprobe set KeyW 1.2` then `hidprobe dump`).
* **Protocol facts live in `docs/protocol.md`.** Update the document when code learns something new about the
  firmware. Travel units depend on the firmware version (`TravelScale.ForFirmware`); never hard-code 100.
* **Only slots with a physical key are patched** when writing; the other 47 table entries are read first and
  written back unchanged.
* **2.4 GHz receiver.** Keyboard commands only reach the keyboard through `ReceiverRelayChannel` (status `F7`, command,
  poll, fetch `FC`; see `docs/protocol.md` §7). Never send `F8` (receiver firmware-update mode). A sleeping keyboard is
  a normal state (`KeyboardAsleepException`), not a device failure: keep the connection, postpone writes, let the
  receiver poll in `DriverEngine.Receiver.cs` pick up the wake-up. Test receiver logic with `FakeReceiver`.
* **Lighting.** `SET_LEDPARAM` (`07`) is sent with the `Bit8` checksum (byte 8; bytes 5..7 hold the colour) and
  verified by reading `87` back with `LightingSettings.LooksLike`. A profile's lighting is optional: `null` means
  "leave the keyboard's lighting alone", never "switch it off". Per-key colour pictures (`0x0C`) are not written.
* **Live view.** Never send the calibration opcodes `0x1C` / `0x1E` (they re-learn the switch calibration; they
  are deliberately absent from `Opcode`). The travel stream (`0x1B`) is owned by the engine: paused around every
  write, switched off on connect and dispose, dropped synchronously under the device gate when the keyboard goes
  away. Raw Input lives in the App project only, is registered only while the live view is on and the window is
  visible, and key events are never logged or stored.
* **Threading.** The engine is the only owner of the device. Mutate profiles via `DriverEngine.MutateLibrary`
  (takes the engine lock) and let the engine re-evaluate; never write to `Profile` objects from a background
  thread. UI updates from engine events must be marshalled with `Dispatcher.BeginInvoke`.
* **UI conventions.** WPF Fluent theme (`ThemeMode="System"`); use the theme's dynamic brushes (`TextFillColor…`,
  `CardBackgroundFillColor…`, `AccentFillColor…`) rather than hard-coded colours so dark mode keeps working.
  Keep the UI simple: one window, no dialogs beyond confirmations and the process picker.
* **Style.** File-scoped namespaces, records for immutable data, `sealed` by default, nullable enabled,
  XML doc comments on public Core APIs. No new third-party UI dependencies.

## Gotchas discovered along the way

* `dotnet new sln` on .NET 10 creates `.slnx`; keep using it (`dotnet sln WomierDriver.slnx add …`).
* `ThemeMode` is still experimental → `WPF0001` is suppressed in the App project.
* HidSharp opens devices in shared mode; the vendor app can run concurrently. The keyboard answers table reads
  without an opcode echo, so do not "validate" `0xE5` responses by their first byte.
* The 2.4 GHz receiver (`PID 0x5038`) never answers a keyboard command sent like over USB: its GET_FEATURE keeps
  returning its last own answer (often the status frame `00 00 00 01 01 01 01` while the keyboard is wired). The
  wired interface is tried first. Hardware checks over 2.4 GHz need the keyboard awake: a key press wakes it, and
  `hidprobe receiver` shows whether it is. The vendor helper (`iot_driver_v*.exe`) was used as a reference by
  attaching a small debugger that logged its `HidD_SetFeature` calls; keep its application closed while testing the
  receiver, since both relay through the same answer buffer.
* Screenshots for UI checks: when the session is locked, `CopyFromScreen` returns the lock screen; use
  `PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT)` instead. UI Automation (`System.Windows.Automation`) can click
  the named buttons ("WASD", "Rapid trigger", …) for scripted checks.
* `Process.GetProcesses()` is polled every 2 s; process names are matched case-insensitively with `.exe` appended.
* WPF popups (`ToolTip`, `ContextMenu`) inherit `FontFamily` and `FontSize` from the element that owns them. Never
  set an icon font on a `Button` itself; `IconButtonStyle` applies it to the glyph through a `ContentTemplate`.
* The vendor input collection (`MI_01&Col05`, usage `0xFFFF/0x0001`) is a separate HID device object whose path
  does not reveal its parent; it is matched to the keyboard by product id. HidSharp read timeouts surface as
  `TimeoutException` (idle), anything else from `Read` means the device is gone. `WM_INPUT` must not be marked
  handled, and Raw Input reports injected input with a zero device handle (ignored).
* Enumerating HID devices with descriptor reads takes about a second; never do it on the UI thread
  (`SetLiveViewAsync` is called through `Task.Run`).
