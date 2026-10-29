# SK75 TMR HID protocol (as used by this driver)

The Womier SK75 TMR is built on the RY5088 controller family and is configured over USB HID feature
reports. Everything below was recovered from the vendor's Electron application (WOMIER Driver 3.1.0,
`commonKBRY5088.js` and the main bundle) and verified live against the keyboard on 2026-10-28.
See [reverse-engineering/README.md](reverse-engineering/README.md) for the provenance.

## 1. Device enumeration

| Property | Value |
|---|---|
| USB vendor id | `0x3151` |
| Product id, wired | `0x5030` (shared by many RY5088 boards, so the model is confirmed by its device id) |
| Product id, 2.4 GHz receiver | `0x5038` |
| Configuration collection | usage page `0xFFFF`, usage `0x0002`, interface `MI_02` |
| Report | feature report, report id `0`, 64 data bytes (65 with the id byte) |
| Vendor input collection | usage page `0xFFFF`, usage `0x0001`, interface `MI_01` (path `…&mi_01&col05…`), report id 5, 32 data bytes per input report: device notifications and the live travel stream, see §6 |
| Other collections | `MI_00` keyboard, `MI_01` consumer/system/keyboard/mouse |

The driver opens the configuration collection in shared mode (`HidD_SetFeature` / `HidD_GetFeature`), so the
vendor software can stay installed and even run at the same time.

## 2. Packet format

```
byte 0      command (opcode)
byte 1..6   arguments
byte 7      checksum = 0xFF - (sum(byte 0..6) & 0xFF)      ("Bit7" scheme; lighting commands use byte 8 instead)
byte 8..63  payload (bulk data chunks are 56 bytes)
```

Every request is a `SET_FEATURE`. Queries are followed by a `GET_FEATURE` about 10 ms later which returns the
device's 64-byte answer. The vendor software sends exactly this sequence (`sendMsg` then `readMsg`).

Observed response behaviour:

* Query commands (`0x8x`) echo the opcode in byte 0 and fill their data; byte 7 is **not** a valid checksum of
  the filled response (the firmware computes it on the echoed request first), so responses are not validated by
  checksum.
* Per-key table reads (`0xE5`) return raw table bytes starting at byte 0, without an opcode echo.
* Unknown commands are echoed back with a checksum (e.g. `F1 00 00 00 00 00 00 0E`).

## 3. Commands used

| Opcode | Name | Direction | Notes |
|---|---|---|---|
| `0x8F` | GET_USB_VERSION | query | bytes 1..4 = device id (u32 LE), bytes 7..8 = firmware version (u16 LE). SK75 TMR: id **2518**, version **776** (`0x0308`, shown as 3.08) |
| `0x80` | GET_RF_VERSION | query | bytes 1..2 = RF firmware version, 0 when wired |
| `0x84` | GET_PROFILE | query | byte 1 = active on-board profile (0..3) |
| `0x04` | SET_PROFILE | command | byte 1 = profile index (not used by this driver) |
| `0x83` | GET_REPORT | query | byte 2 = polling rate code (0 = 8000 Hz … 6 = 125 Hz) |
| `0xE5` | GET_MULTI_MAGNETISM | query | read one 64-byte chunk of a per-key table, see §4 |
| `0x65` | SET_MULTI_MAGNETISM | command | write per-key tables, see §4 |
| `0x1B` | SET_MAGNETISM_REPORT | command | byte 1 = 1 starts / 0 stops the live travel input reports, see §6; no answer |
| `0xF1` | (receiver) | query | on the 2.4 GHz receiver: bytes 1..2 = id of the paired keyboard; all zeros / `00 00 00 01 01 01 01` when nothing is paired or the keyboard is wired |

The vendor application also implements key remapping (`0x0A`/`0x8A`, `0x10`/`0x90`), lighting (`0x07`, `0x0C`),
macros, sleep timers and firmware upgrade. Those are documented in the vendor source kept under
`reverse-engineering/vendor/` and are intentionally **not** touched by this driver. The calibration commands
`0x1C` and `0x1E` are deliberately not implemented at all (§6).

## 4. Per-key analog tables

All analog settings live in tables of 128 entries, one per key-matrix slot (the SK75 TMR uses 81 of them, see
`Sk75TmrLayout.Keys.cs`). The table is selected with byte 1 of `0x65`/`0xE5`:

| Sub-index | Table | Element | Meaning |
|---|---|---|---|
| 0 | Travel | u16 LE | actuation point ("press travel") |
| 1 | LiftTravel | u16 LE | reset point ("release travel") |
| 2 | FirePressTravel | u16 LE | rapid-trigger press sensitivity |
| 3 | FireLiftTravel | u16 LE | rapid-trigger release sensitivity |
| 4 | DynamicTravel | u16 LE | DKS travel (unused) |
| 5 | ModTapTime | u8 | mod-tap time / 10 ms (unused) |
| 6 | DeadZoneTravel | u16 LE | bottom dead zone |
| 7 | KeyModes | u8 | bit 7 = rapid trigger on, bits 0..6 = mode (0 normal, 2 DKS, 3 mod-tap, 4/5 toggle, 7 snap) |
| 8 | TriggerModes | 4 × u8 | DKS actions (unused) |
| 9 | SnapBindKey | u8 | snap-tap partner slot (unused) |
| 10 | DksData | bulk | DKS data (unused) |
| 251 | TopDeadZoneTravel | u8 | top dead zone, firmware ≥ 1024 only (the SK75 TMR at 776 ignores it) |
| 252 | SwitchBody | u8 | installed switch type code (69 = "玄磁轴" on the test unit); read-only here |

### 4.1 Units

Travel values are integers; the unit depends on the firmware version, exactly as the vendor software decides:

| Firmware (`rf` if non-zero, else `usb`) | Units per mm | Step |
|---|---|---|
| ≥ 1280 | 200 | 0.005 mm |
| 768 … 1279 (SK75 TMR, 776) | 100 | 0.01 mm |
| < 768 | 10 | 0.1 mm |

### 4.2 Value ranges (vendor UI limits)

| Setting | Min | Max | Default |
|---|---|---|---|
| actuation / release | 0.1 mm | 3.3 mm (4.0 mm on firmware < 768) | 2.0 / 2.8 mm |
| release point | 0.1 mm | 3.3 mm − bottom dead zone | |
| RT press / release sensitivity | 0.01 mm | 2.0 mm | 0.2 mm |
| bottom dead zone | 0 mm | 1.0 mm | 0.3 mm |

Factory state observed on the test unit: every key at 2.80 / 2.80 mm, dead zone 0.30 mm, RT off, RT tables 0.

### 4.3 Reading a table

```
E5 <sub> 01 <chunk> 00 00 00 <ck>      -> 64 bytes of table data
```

Read `ceil(tableBytes / 64)` chunks and concatenate: 4 chunks for u16 tables (256 bytes), 2 for u8 tables.

### 4.4 Writing a table

```
65 <sub> 01 <chunk> <last> 00 00 <ck>  + 56 data bytes
```

`chunk` counts 56-byte pieces (5 for a u16 table, 3 for a u8 table). `last` is 1 **only on the final chunk of
the final table** of a write sequence; it makes the firmware apply and persist everything written since the
previous commit. The vendor software writes tables in the order modes (7) → travel (0) → lift (1) → RT press (2)
→ RT lift (3) → dead zone (6) → others, waits 500 ms after the commit chunk and another 100 ms before the next
command. This driver does the same and then reads tables 0 and 7 back to verify.

A single key can also be written with `65 <sub> 00 <slot> <last> 00 00 <ck> + value`.

Per-key tables are not addressed per on-board profile; writes affect the profile that is active on the keyboard
(`GET_PROFILE`), which is what the vendor software does as well.

## 5. Timing (measured)

* Identify + read all managed tables: about 250 ms.
* Full profile write (6 tables, 28 packets) including commit wait and verification: about 950 ms.
* Live travel stream: reports 2–3 ms apart while a key moves (up to about 1 kHz); a table-254 sweep takes about
  42 ms with the 10 ms read delay (23 Hz).

## 6. Live travel reporting (verified on the keyboard, 2026-10-29)

Besides the configuration collection the keyboard has a **vendor input collection** (usage page `0xFFFF`, usage
`0x0001`, interface `MI_01`, Windows path `…&mi_01&col05…`, report id 5, 32 data bytes). It carries device
notifications (`0F 01/00` busy start/end, `01` profile changed, `02` report rate, `03` fn layer / OS, `04..0B`
lighting, `0D` reset, `13` sleep, `1D` magnetism mode; names from the vendor source) and, when enabled, the live
travel stream. Observed so far: `05 0F 01 00 …` when a profile write is committed and `05 0F 00 00 …` about
200 ms later, i.e. the firmware is busy for the duration of the flash write.

| Packet (feature report on the configuration collection) | Meaning |
|---|---|
| `1B 01 00 00 00 00 00 E3` | start the live travel reports |
| `1B 00 00 00 00 00 00 E4` | stop them (idempotent) |

While enabled the keyboard sends one input report per key and per change of that key's travel:

```
byte 0     report id (5)
byte 1     0x1B
byte 2..3  travel, u16 LE, same units as the per-key tables (0.01 mm on firmware 776)
byte 4     matrix slot of the key (same numbering as the per-key tables)
byte 5..31 always 0
```

Measured on the SK75 TMR with firmware 776: reports arrive only while a key moves (nothing at rest, nothing after
`1B 00`), 2–3 ms apart while a key travels, several keys interleaved, every key returns to 0 on release, and
bottom-out reads 355 (3.55 mm). Normal typing keeps working while the stream is on. The vendor's "simulation test"
uses this stream but ignores byte 4 and shows the value against the last key that produced a keydown.

Table 254 (`E5 FE 01 <chunk>`, 4 chunks of 64 bytes = 128 × u16 LE, one per slot) holds the same current travel
per key and is live outside calibration as well (`hidprobe travel`); a full sweep takes about 42 ms with the 10 ms
read delay. The driver uses the stream.

**Never send `0x1C` (SET_MAGNETISM_CAL) or `0x1E` (SET_MAGNETISM_MAXIMUM_CALIBRATION).** The vendor's calibration
dialog wraps its table-254 polling in them; they make the firmware re-learn the rest and bottom-out positions of
every switch, and the vendor tells the user to bottom out every key while they are active.
