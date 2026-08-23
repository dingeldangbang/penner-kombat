#!/usr/bin/env python3
"""Prueft ein APK auf 16-KB-Page-Size-Kompatibilitaet (Android 15/16).

Hintergrund
-----------
Android 15 kann und Android 16 wird auf vielen Geraeten mit 16-KB-Speicherseiten
laufen. Native Bibliotheken (.so) muessen dann LOAD-Segmente haben, die auf
16 KB (0x4000) ausgerichtet sind. Godot < 4.5 baut mit NDK r27 und richtet auf
4 KB (0x1000) aus -> auf einem 16-KB-Geraet gibt es beim Start
"UnsatisfiedLinkError" bzw. beim Installieren
"INSTALL_FAILED_INVALID_APK: unsupported ELF page size".

Nur 64-Bit-ABIs sind betroffen. armeabi-v7a/x86 bleiben bei 0x1000 -- das ist
korrekt so und wird hier nicht als Fehler gewertet.

Aufruf:
    python3 Tools/check_apk_16kb.py build/pennerkombat-debug.apk

Exit-Code 0 = alle 64-Bit-.so sind 16-KB-tauglich, sonst 1.

Reines Python, keine Abhaengigkeiten -- laeuft ohne NDK/llvm-readelf auf jedem
GitHub-Runner.
"""

from __future__ import annotations

import struct
import sys
import zipfile

REQUIRED_ALIGN = 0x4000  # 16 KB
PT_LOAD = 1

# ABIs, die zwingend 16-KB-tauglich sein muessen (64 Bit).
ABIS_64 = ("arm64-v8a", "x86_64")


class ElfError(Exception):
    pass


def load_segment_aligns(data: bytes) -> tuple[list[int], bool]:
    """Gibt (p_align aller PT_LOAD-Segmente, is_64bit) zurueck."""
    if len(data) < 64 or data[:4] != b"\x7fELF":
        raise ElfError("keine ELF-Datei")

    ei_class = data[4]      # 1 = 32 Bit, 2 = 64 Bit
    ei_data = data[5]       # 1 = little endian, 2 = big endian
    endian = "<" if ei_data == 1 else ">"
    is_64 = ei_class == 2

    if is_64:
        # e_phoff @0x20 (8B), e_phentsize @0x36 (2B), e_phnum @0x38 (2B)
        e_phoff = struct.unpack_from(endian + "Q", data, 0x20)[0]
        e_phentsize = struct.unpack_from(endian + "H", data, 0x36)[0]
        e_phnum = struct.unpack_from(endian + "H", data, 0x38)[0]
    else:
        # e_phoff @0x1C (4B), e_phentsize @0x2A (2B), e_phnum @0x2C (2B)
        e_phoff = struct.unpack_from(endian + "I", data, 0x1C)[0]
        e_phentsize = struct.unpack_from(endian + "H", data, 0x2A)[0]
        e_phnum = struct.unpack_from(endian + "H", data, 0x2C)[0]

    aligns: list[int] = []
    for i in range(e_phnum):
        off = e_phoff + i * e_phentsize
        if off + e_phentsize > len(data):
            raise ElfError("Program-Header ausserhalb der Datei")
        p_type = struct.unpack_from(endian + "I", data, off)[0]
        if p_type != PT_LOAD:
            continue
        # p_align ist das letzte Feld: 64 Bit -> Offset 0x30, 32 Bit -> 0x1C
        if is_64:
            p_align = struct.unpack_from(endian + "Q", data, off + 0x30)[0]
        else:
            p_align = struct.unpack_from(endian + "I", data, off + 0x1C)[0]
        aligns.append(p_align)

    return aligns, is_64


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print(f"Aufruf: {argv[0]} <datei.apk>", file=sys.stderr)
        return 2

    apk = argv[1]
    try:
        zf = zipfile.ZipFile(apk)
    except (OSError, zipfile.BadZipFile) as exc:
        print(f"FEHLER: {apk} ist kein lesbares APK ({exc})", file=sys.stderr)
        return 2

    sos = [n for n in zf.namelist() if n.startswith("lib/") and n.endswith(".so")]
    if not sos:
        print("FEHLER: keine nativen Bibliotheken (lib/**/*.so) im APK gefunden.")
        print("        Ein Godot-APK enthaelt immer welche -- Export vermutlich kaputt.")
        return 1

    failures: list[str] = []
    print(f"Pruefe {len(sos)} native Bibliothek(en) in {apk}\n")

    for name in sorted(sos):
        abi = name.split("/")[1] if len(name.split("/")) > 2 else "?"
        try:
            aligns, is_64 = load_segment_aligns(zf.read(name))
        except ElfError as exc:
            print(f"  [SKIP] {name}: {exc}")
            continue

        if not aligns:
            print(f"  [SKIP] {name}: keine LOAD-Segmente")
            continue

        worst = min(aligns)
        pretty = ", ".join(hex(a) for a in aligns)
        must_pass = abi in ABIS_64

        if not must_pass:
            print(f"  [ok  ] {name} ({abi}, 32 Bit) align={pretty} -- nicht relevant")
            continue

        # Google Play prueft JEDES LOAD-Segment, nicht nur das erste.
        if worst >= REQUIRED_ALIGN:
            print(f"  [OK  ] {name} align={pretty}")
        else:
            print(f"  [FAIL] {name} align={pretty} (noetig: {hex(REQUIRED_ALIGN)})")
            failures.append(name)

    print()
    if failures:
        print("=" * 68)
        print("NICHT 16-KB-TAUGLICH -- dieses APK startet auf Android-15/16-Geraeten")
        print("mit 16-KB-Seiten nicht (UnsatisfiedLinkError bzw.")
        print("INSTALL_FAILED_INVALID_APK: unsupported ELF page size).")
        print()
        print("Ursache: Godot < 4.5 baut die .so mit NDK r27 (4-KB-Alignment).")
        print("Loesung: GODOT_VERSION im Workflow auf >= 4.5 setzen.")
        print("=" * 68)
        return 1

    print("Alle 64-Bit-Bibliotheken sind 16-KB-tauglich.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
