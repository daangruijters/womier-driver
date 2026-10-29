# Phase 1 — Backend assumptions verified

All facts below were recovered with read-only inspection of the installed
WOMIER Driver on 2026-04-15.

## iot_driver lifecycle

- Running process: `iot_driver_v210.exe` (PID 29780 at time of check),
  listening on **`127.0.0.1:3814` TCP**. Confirmed via `netstat`.
- Binary location: **`%APPDATA%\WOMIER Driver\iot_driver_v210.exe`** (NOT
  the one under Program Files). It's a 1:1 copy of
  `resources\app\iot_driver.exe`, renamed with the version suffix.
- **No Windows service, no scheduled task, no startup-folder entry, no Run
  registry key.** It's spawned on demand and detached.

## Version handshake

`resources\app\APPVersion.json`:

```json
{
  "APPVersion": "3.1.0",
  "IOTVersion": "210",          // <-- this is Jd
  "IOTMacVersion": "204",       // <-- this is ec
  "WebIOTVersion": "210",
  "WebIOTMacVersion": "188"
}
```

The renderer reads this at startup, derives the filename
`iot_driver_v${IOTVersion}.exe`, and enforces that the file in userData
matches. Any mismatched version in userData is `unlinkSync`'d.

## Launch sequence (reverse-engineered from index.dd282f03.js)

Function names in the minified source, for reference: `autoConnect = a => { if (Rt) { killIotProcess(); await n(); await t(a); receiveData(); } else return i(); }`.

1. **`killIotProcess()`** — `tasklist` → split by whitespace → for any
   token containing `iot_driver`, `taskkill /f /t /im <token>`. (Mac:
   `lsof -i :3814` → kill PIDs.)
2. **`n()`** (install): `app.getPath("userData")` → `%APPDATA%\WOMIER Driver\`.
   Scan directory; delete any `iot_driver_*` whose version != current.
   If the current-version file doesn't exist, copy
   `app.getAppPath()/iot_driver.exe` to the userData path. Return the
   resolved path.
3. **`r(path)`** (spawn) — Windows uses `shell.openPath(path)` (Electron's
   `shell.openPath`, equivalent to a double-click; the child detaches at
   the OS level and survives parent exit). Mac uses
   `child_process.spawn(path, [], { detached: true, windowsHide: true, stdio: "ignore" })` then waits 500 ms.
4. **`receiveData()`** — start consuming the gRPC stream (probably
   `watchSystemInfo`).

**`Rt` is a build flag:** when false, the app falls back to WebHID
(`navigator.hid.requestDevice({filters: PR ∪ something})`). This tells us
WOMIER also ships a web version that talks to the keyboard directly from
the browser, bypassing iot_driver.exe entirely. **Out of scope for us** —
we're reusing iot_driver.

## gRPC endpoint confirmed live

```
$ curl -v -X POST http://127.0.0.1:3814/driver.DriverGrpc/getVersion \
    -H "Content-Type: application/grpc-web+proto" -H "X-Grpc-Web: 1"
< HTTP/1.1 200 OK
< content-type: application/grpc-web+proto
< grpc-status: 13       # INTERNAL ("Missing request message") — route exists
< access-control-allow-credentials: true
< vary: origin,access-control-request-method,access-control-request-headers
```

- ✅ Service name is **`driver.DriverGrpc`**
- ✅ gRPC-Web transport with **`application/grpc-web+proto`** content type
  (NOT `grpc-web-text`, so framing is raw bytes, not base64)
- ✅ CORS open — a Tauri webview can call directly from JS
- ✅ Framing is the standard gRPC-Web prefix (`0x00` uncompressed +
  4-byte big-endian length + protobuf bytes)

## gRPC client construction (from the bundle)

```js
// namespace `Ie`
const i = new FR({ baseUrl: "http://127.0.0.1:3814" });   // FR = GrpcWebFetchTransport
e.client = new IR(i);                                      // IR = DriverGrpcClient
e.changeWirelessLoopStatus = _ => { ... e.client.changeWirelessLoopStatus(g) ... }
e.sendMsg = (msg, devicePath, checkSumType, dangleDevType) => {
    const x = Jc.create();        // Jc = sendRawFeature request message type
    x.msg = _;
    x.devicePath = h;
    x.checkSumType = g;
    x.dangleDevType = k;
    e.client.sendMsg(x).then(...)
};
```

**Key find**: the high-level wrapper used for every device command is
`sendMsg(msg, devicePath, checkSumType, dangleDevType)`. That narrows the
protobuf hunt enormously — we really only need to decode the request type
with those four fields plus the response type.

## WebHID fallback VID/PID table (PR array)

For reference / future work, not used in MVP:

| VID    | PID    | Hex             |
|--------|--------|-----------------|
| 12625  | 20487  | 0x3151 / 0x5007 |
| 12625  | 20512  | 0x3151 / 0x5020 |
| 12625  | 20543  | 0x3151 / 0x503F |
| 12625  | 20544  | 0x3151 / 0x5040 |
| 13434  | 41730  | 0x347A / 0xA302 |
| 12625  | 20555  | 0x3151 / 0x504B |

VID `0x3151` is WOMIER's primary USB vendor (multiple PIDs for different
chip families). `0x347A` is probably a dongle / alternate family.

## Implications for Tauri app (Phase 5)

- **Don't ship iot_driver.exe.** We'll locate the installed copy at
  `%LOCALAPPDATA%\Programs\WOMIER Driver\resources\app\iot_driver.exe`
  and require the user to have the official driver installed. License-safe.
- **On launch**: check if `127.0.0.1:3814` is already listening. If yes,
  connect directly and don't kill anything (be a good citizen — the user
  may be running the official app too). If no, copy our own
  `iot_driver.exe` discovery logic: look in `%APPDATA%\WOMIER Driver\`
  for an existing `iot_driver_v*.exe`, launch it via the OS shell-open
  equivalent (Rust: `std::process::Command::new(path).spawn()` with
  `CREATE_NEW_PROCESS_GROUP` flag for detach).
- **On shutdown**: do NOT kill iot_driver. Leave the running daemon in
  place — it's shared state. The official app's kill-on-launch behaviour
  is hostile and is part of what we're fixing.
- **Check `APPVersion.json`** at startup to warn if the user has a
  different `IOTVersion` than we've tested against. Use it as a telemetry
  key in our own local profile format's `firmware` field.
