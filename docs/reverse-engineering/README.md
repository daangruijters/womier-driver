# Reverse-engineering notes

This driver does not use any vendor binary. The protocol was recovered by reading the vendor's Electron
application (WOMIER Driver 3.1.0, installed under `%LOCALAPPDATA%\Programs\WOMIER Driver\resources\app`,
which ships unpacked, minified JavaScript) and confirmed with live captures on the SK75 TMR.

## What is in `vendor/`

| File | Origin | Why it is kept |
|---|---|---|
| `commonKBRY5088.pretty.js` | `dist/js/commonKBRY5088.*.js`, prettified | the RY5088 packet builders: every opcode, table sub-index, chunking rule and timing used in `docs/protocol.md` |
| `ry5088_womiersk75he_8k_8k.pretty.js` | `dist/js/ry5088_womiersk75he_8k_8k.*.js` | the SK75 device class with its factory key matrices (`defaultMatrix` etc.) |
| `agk75b_ui_info.js` | `dist/js/keyboard_81_agk75b_keymappings_ui_info.*.js` | key geometry of the "Common81_AGK75B" layout that the SK75 TMR uses in the vendor UI |
| `sk75-layout.json` | decoded from the matrices above | slot → HID usage table consumed by `tools/gen-layout.py` |
| `iot_driver.proto` | recovered from the bundle in an earlier analysis | gRPC schema of the vendor's optional native helper; not used by this driver, kept for reference |

The vendor catalogue entry that identifies the model:

```
{ id: 2518, vid: 12625 (0x3151), pid: 20528 (0x5030), keyLayout: Common81_AGK75B,
  name: "ry5088_womiersk75he_8k_8k", displayName: "SK75 TMR", company: "WOMIER",
  layer: 4, magnetism: true, isSwitchReplaceable: true,
  supportedSwitchTypes: ["高特", "磁玉pro", "磁玉gaming", "万磁王RGB", "万磁王POM", "玄磁轴"] }
```

## How the vendor app talks to the keyboard

The Electron renderer has two transports: WebHID directly from the renderer (used by the web version at
`iotdriver.qmk.top` and selectable in the desktop build), and a small native helper `iot_driver.exe` reached
over gRPC-Web on `127.0.0.1:3814` that merely forwards feature reports. The packet layer
(`commonKBRY5088.js`) is identical for both, which is why a plain HID implementation works.

An earlier analysis (April 2026, see `history/`) concluded that the SK75 could only be reached through
`iot_driver.exe` because its product id was missing from the WebHID filter list it looked at. That list was the
wrong one: the vendor bundle has a second filter table (`Op`) that contains both `0x3151:0x5030` and
`0x3151:0x5038` on usage page `0xFFFF`, usage 2.

## Live findings that corrected the static analysis

* The model id is **not** obtained with `0xF1` on a wired keyboard (that command only works on the receiver);
  it is in bytes 1..4 of the `GET_USB_VERSION` (`0x8F`) answer. The vendor's `getCommonDeviceId()` does exactly
  this.
* Per-key table reads answer with raw data and no opcode echo.
* The firmware of the test unit is 776 (3.08): 0.01 mm units, no top dead zone support.
* Top dead zone (sub-index 251) is a `u8` table, not `u16` as the earlier notes assumed.
