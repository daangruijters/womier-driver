#!/usr/bin/env python3
"""Copies the vendor sources this driver was reverse-engineered from into docs/reverse-engineering/vendor/.

They are Womier's code, so they are not part of the repository (the folder is git-ignored apart from our own
sk75-layout.json); this script restores the local reference from an installed "WOMIER Driver". The bundles are
formatted with Prettier (through npx) when Node.js is available, otherwise copied as they are (minified).

Usage:  python tools/extract-vendor.py [path to the vendor app's resources\\app folder]
"""
from __future__ import annotations

import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "docs" / "reverse-engineering" / "vendor"
DEFAULT_APP = Path(os.environ.get("LOCALAPPDATA", "")) / "Programs" / "WOMIER Driver" / "resources" / "app"
PRETTIER = ["npx", "--yes", "prettier@3.3.3", "--print-width", "120", "--write"]

# (glob under dist/js, target name or None to keep the bundle's own name, what it is for)
BUNDLES = [
    ("commonKBRY5088.*.js", "commonKBRY5088.pretty.js", "RY5088 packet builders: every opcode, table and timing"),
    ("ry5088_womiersk75he_8k_8k.*.js", "ry5088_womiersk75he_8k_8k.pretty.js", "SK75 device class, factory key matrices"),
    ("keyboard_81_agk75b_keymappings_ui_info.*.js", "agk75b_ui_info.js", "key geometry, read by tools/gen-layout.py"),
    ("Keyboard_81_AGK75B_KeyMappings.*.js", None, "key remapping screen of the SK75's layout"),
]


def main() -> int:
    app = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_APP
    js = app / "dist" / "js"
    if not js.is_dir():
        print(f"Vendor application not found: {js}\nInstall WOMIER Driver or pass its resources\\app folder.")
        return 1

    OUTPUT.mkdir(parents=True, exist_ok=True)
    written: list[Path] = []
    for pattern, target, purpose in BUNDLES:
        matches = sorted(js.glob(pattern))
        if not matches:
            print(f"  missing  {pattern}")
            continue
        if target is not None and len(matches) > 1:
            print(f"  ambiguous {pattern}: {', '.join(m.name for m in matches)}")
            return 1
        for source in matches:
            name = target or source.name.removesuffix(".js") + ".pretty.js"
            destination = OUTPUT / name
            shutil.copyfile(source, destination)
            written.append(destination)
            print(f"  {source.name} -> {name}  ({purpose})")

    if written and shutil.which("npx"):
        subprocess.run(PRETTIER + [str(p) for p in written], check=True, shell=os.name == "nt",
                       stdout=subprocess.DEVNULL)
        print("Formatted with Prettier.")
    elif written:
        print("npx not found: the bundles stay minified.")

    print(f"Done: {OUTPUT}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
