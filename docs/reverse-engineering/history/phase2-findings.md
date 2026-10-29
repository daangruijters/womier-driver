# Phase 2 — Protobuf schema recovered and validated

## Deliverable

`womier.proto` (colocated in this directory) — the complete
`driver.DriverGrpc` service definition: **28 messages, 5 enums, 21 RPCs.**
Extracted mechanically from `index.dd282f03.js` by
[`extract_proto.py`](./extract_proto.py).

## Extraction method

`@protobuf-ts`'s code-gen template survives minification almost fully
because the protobuf runtime reflects on the descriptor at runtime, so the
descriptor fields (`no`, `name`, `kind`, `T`, `repeat`, `oneof`) keep their
names verbatim.

- **Messages** → each is a class of the shape
  `class <Name> extends <Base> { constructor() { super("driver.<Type>", [ { no, name, kind, T } ... ]) } }`.
  The extractor slices each `[...]` with a proper bracket-and-string-aware
  walker (naïve regex would choke on strings containing `]`).
- **Instance vars** → each registered class has a
  `const <var> = new <Class>;` immediately after the class body; the
  service definition references messages by these instance vars. Resolving
  `var → class → typeName` is a two-step lookup.
- **Enums** → `@protobuf-ts` emits
  `(e=>(e[e.Foo=0]="Foo",e[e.Bar=1]="Bar",e))(<alias>||{})`. We find the
  `["driver.EnumName", <alias>]` references inside message field
  descriptors, then locate the matching IIFE by alias name.

Result: **0 unresolved references.**

## Live validation #1 — `getVersion(Empty) → Version`

```
POST /driver.DriverGrpc/getVersion   (body: 00 00 00 00 00  [5-byte empty frame])

response body (hex):
  00 00 00 00 14                             -- uncompressed, length=20
  0a 03 "210"                                -- baseVersion = "210"
  12 0d "1757928579865"                      -- timeStamp   = 2025-09-15 ms since epoch
  80 00 00 00 0f "grpc-status:0\r\n"         -- trailer
```

- `baseVersion = "210"` — matches `APPVersion.json IOTVersion: "210"` ✓
- `timeStamp` parses to **2025-09-15 12:09:39 UTC** — within days of the
  `iot_driver.exe` file mtime (Sep 19 2025) ✓

## Live validation #2 — `watchDevList(Empty) → stream DeviceList`

```
POST /driver.DriverGrpc/watchDevList  (body: 00 00 00 00 00)

first streamed frame (hex):
  00 00 00 00 d1                             -- uncompressed, length=209
  0a 6b                                      -- devList[0], DJDev msg, len=107
    0a 69                                    -- oneof.dev -> Device, len=105
      08 02                                  -- devType = YZWVender (2)
      1a 5e "\\?\HID#VID_3151&PID_5038&MI_01&Col05#9&14acf981&0&0004#{4d1e55b2-f16f-11cf-88cb-001111000030}"
      38 d1 62                               -- vid = 0x3151
      40 b8 a0 01                            -- pid = 0x5038
  0a 62                                      -- devList[1], DJDev msg, len=98
    12 60                                    -- oneof.dangleCommonDev -> DangleCommon, len=96
      1a 57 "...VID_3151&PID_5038&MI_02..."
      38 d1 62
      40 b8 a0 01
```

**This is your WOMIER SK75 HE on the wire.** VID `0x3151`, PID `0x5038`,
enumerated as two HID interfaces:

- `MI_01&Col05` → `Device { devType: YZWVender }` — the configuration
  interface (what `sendRawFeature` will target for keymap / lighting /
  rapid-trigger writes).
- `MI_02` → `DangleCommon` — secondary collection, probably for the
  media-keys / consumer-control report (confusingly named "DangleCommon"
  even though the keyboard is wired; the protocol is shared with dongles).

> **Correction (2026-10-29):** the roles are the other way round. `MI_02` (usage page `0xFFFF`, usage 2) is the
> configuration collection that takes the 64-byte feature reports, and `MI_01&Col05` (usage 1) is the 32-byte
> **input** collection that carries device notifications and the live travel stream. The paths above are the
> 2.4 GHz receiver's; the wired keyboard (`PID_5030`) has the same pair. See `docs/protocol.md` §1 and §6.

Neither `0x3151:0x5038` appears in the WebHID-fallback `PR` VID/PID table
from `autoConnect`. **This kills any plan to bypass `iot_driver.exe`
for this keyboard.** Reusing the native daemon is the only path.

## MVP-relevant RPCs (subset of the 21)

All the RPCs we actually need for the Tauri UI, in priority order:

| RPC                        | Input       | Output           | Use in MVP |
|----------------------------|-------------|------------------|------------|
| `watchDevList`             | Empty       | stream DeviceList | Device list on launch + hot-plug |
| `readRawFeature`           | ReadMsg     | ResRead          | Read current keymap/lighting/RT state |
| `sendRawFeature`           | SendMsg     | ResSend          | Write new keymap/lighting/RT state |
| `sendMsg`                  | SendMsg     | ResSend          | Same shape, high-level variant — used by the renderer's `e.sendMsg` wrapper. Pick one after observing which the UI flows use. |
| `readMsg`                  | ReadMsg     | ResRead          | Symmetric to `sendMsg`. |
| `getVersion`               | Empty       | Version          | Hello-world probe, display backend version in title bar |

**Out of scope for MVP** (stub or skip): `upgradeOTAGATT`, `muteMicrophone`,
`toggleMicrophoneMute`, `getMicrophoneMute`, `changeWirelessLoopStatus`,
`insertDb`, `deleteItemFromDb`, `getItemFromDb`, `getAllKeysFromDb`,
`getAllValuesFromDb`, `cleanDev`, `setLightType` (that one is *not* the
lighting configuration RPC — it sets a high-level mode on devices that
support display/screen/music visualisers; actual RGB config goes through
`sendRawFeature`), `getWeather`, `watchVender`, `watchSystemInfo` (telemetry).

## Key contract: `SendMsg`/`ReadMsg` is a thin transport

```proto
message SendMsg {
  string devicePath = 1;        // the HID path from Device.path
  bytes msg = 2;                // opaque HID feature report payload
  CheckSumType checkSumType = 3; // Bit7, Bit8, or None
  DangleDevType dangleDevType = 4; // DangleDevTypeNone, Keyboard, Mouse
}

message ReadMsg {
  string devicePath = 1;        // only the path is needed to read
}

message ResSend { string err = 1; }
message ResRead { string err = 1; bytes msg = 2; }
```

- `msg` is an **opaque HID feature report**. Its layout is defined in
  `commonKBRY5088.11676525.js` (the ry5088 family packet builders). For
  the Tauri UI we call `sendRawFeature(devicePath, msg_bytes_we_built,
  Bit8, DangleDevTypeNone)` — meaning **we still need to reverse-engineer
  the packet format**, even though we're reusing the transport.
- **This is the moment where "UI-only replacement" partially overlaps
  with "clean-room driver".** We don't have to re-implement USB HID I/O
  (iot_driver handles that), but we do have to build the byte-level
  opcodes for keymap / lighting / rapid-trigger. Phase 3 is where we
  lift those directly from `commonKBRY5088.js`.

## Next

Phase 3: open `commonKBRY5088.11676525.js` + the three KF112 screen files
and document which bytes each feature sends. Also lift the SK75 HE layout
arrays out of `ry5088_womiersk75he_8k_8k.js`.
