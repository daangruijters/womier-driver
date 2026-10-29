# Phase 3 — RY5088 protocol spec + SK75 HE layout extracted

## Deliverables

- **[`ry5088-hid.md`](./ry5088-hid.md)** — full protocol spec for the
  MVP surface (transport, checksum, keymap, RGB, rapid trigger),
  cross-referenced to the JS source.
- **[`sk75he-layout.json`](./sk75he-layout.json)** — SK75 HE default
  matrices lifted verbatim from `ry5088_womiersk75he_8k_8k.js` plus a
  decoded human-readable slot list.
- **[`extract_sk75he.py`](./extract_sk75he.py)** — reproducible
  extractor for the layout JSON.
- **[`pretty/commonKBRY5088.js`](./pretty/commonKBRY5088.js)** —
  prettified copy of the packet builder source (2050 lines), kept for
  reference during implementation.

## Key facts established

- Feature reports use **HID report ID 0**. Minimum 8 bytes, padded with
  zeros.
- Checksum is a **byte-sum 1's complement**: `byte[n] = 255 - (sum(0..n-1) & 0xff)`.
  Slot 7 for `Bit7` scheme, slot 8 for `Bit8` (only lighting uses Bit8).
  `iot_driver.exe` computes it server-side — our `sendRawFeature` call
  leaves the slot zero.
- 128 key slots × 4 bytes = 512-byte keymap. Bulk writes paginate as
  9 × 56-byte chunks + header.
- Opcode table: SET commands `< 128`, GET commands `≥ 128`. Seven
  opcodes cover the MVP: 0x01 (reset), 0x04 (profile), 0x07 (lighting),
  0x0A (base keymap), 0x10 (Fn keymap), 0x65 (multi-magnetism),
  0x8F (USB version — needed for the travel-step multiplier).
- Travel values for rapid trigger are scaled by a **firmware-version
  gate**: `usbVer ≥ 1280 → ×200`, `768..1279 → ×100`, else `×10`. SK75
  HE firmware ships at the newest gate, so actuation 1.5 mm = raw u16
  `300`.
- Rapid-trigger configuration uses a single opcode (0x65 / 0xE5) with a
  sub-index byte selecting which parameter stream. 13 sub-indices cover
  everything the MVP needs; only five (0, 1, 6, 7, 251) are load-
  bearing for the basic remap-and-RT flow.
- Key encoding is a tagged union with byte 0 as category. For the MVP,
  three shapes matter: `[0,0,hid,0]` (plain key), `[0,mod,hid,0]`
  (modifier combo), `[0,0,0,0]` (empty/blocked). Anything more exotic
  (media keys, brightness, macros) uses lookup tables (`fa`, `sm`,
  `om`, `TD`) that we **lift from the main bundle as data, not code**.

## Deliberately not covered (documented as out-of-scope in the spec)

Macros (SET_MACRO 0x0B), OLED / TFT / user-GIF, firmware upgrade
(`upgradeOTAGATT`), calibration, Windows-key lock, sleep timers, side-
LED. These are all implementable with the same primitives if ever
needed but have no place in the MVP.

## What the Tauri app inherits from this spec

1. A tonic-generated client from `womier.proto` (Phase 2).
2. A constants module with the opcode table and sub-index table.
3. A `ry5088` packet builder module that takes a typed config and emits
   64-byte arrays for `sendRawFeature`.
4. A `devices/sk75he.json` embedded as a `static &str` (or baked at
   compile time) — the default matrices.
5. Lookup tables (`fa`, `sm`, `om`, `TD`) — to be lifted from
   `index.dd282f03.js` as JSON in Phase 5 when the need arises for
   anything beyond plain key remaps. The MVP can ship without them if
   we restrict remap targets to plain HID keys and modifier combos;
   adding media-key support is a later pass.
