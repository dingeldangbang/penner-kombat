#!/usr/bin/env python3
"""Prueft die eingebaute Kaempfer-Riege gegen die Rigging-Logik.

Warum das noetig ist
--------------------
DynamicRigger haengt Hitboxen und VFX an "Knochen" auf. Findet es den in einem
Combo genannten `attach_bone` nicht, faellt es stillschweigend auf "Root"
zurueck -- der Treffer kommt dann aus der Huefte statt aus der Faust, ohne
Fehlermeldung. Genau solche Fehler sieht man im Spiel nur als "fuehlt sich
komisch an".

Dieses Skript liest FighterRoster.gd und DynamicRigger.gd, bildet die
Klassifizierung nach und prueft fuer jeden Combo jedes Kaempfers, ob der
Aufhaengepunkt wirklich existiert.

Aufruf:
    python3 Tools/check_roster.py
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ROSTER = ROOT / "scripts" / "game" / "FighterRoster.gd"
RIGGER = ROOT / "scripts" / "core" / "DynamicRigger.gd"


# --- Nachbau von DynamicRigger._classify_name ------------------------------

def classify_name(name: str) -> str:
    low = name.lower().replace(".", "_").replace("-", "_")
    if "head" in low:
        return "Head"
    if "hand" in low or "wrist" in low or "fist" in low:
        if low.endswith("_r") or "right" in low or low.endswith(".r"):
            return "Hand_R"
        if low.endswith("_l") or "left" in low or low.endswith(".l"):
            return "Hand_L"
    if "root" in low or "hips" in low or "pelvis" in low:
        return "Root"
    return ""


# --- Nachbau von DynamicRigger._normalize_attach_bone ----------------------

def normalize_attach_bone(name: str, bone_map: set[str]) -> str:
    low = name.lower()
    if low in ("right", "righthand", "right_hand", "hand_r", "hand.r"):
        return "Hand_R"
    if low in ("left", "lefthand", "left_hand", "hand_l", "hand.l"):
        return "Hand_L"
    if low == "head":
        return "Head"
    return name if name in bone_map else "Root"


# --- FighterRoster.gd auslesen ---------------------------------------------

def parse_parts(src: str) -> list[str]:
    """Namen aller _part(...)-Aufrufe -- das sind die Mesh-Knoten der Figur."""
    return re.findall(r'_part\(\s*root\s*,\s*"([^"]+)"', src)


def parse_fighters(src: str) -> list[tuple[str, list[tuple[str, str]]]]:
    """[(fighter_id, [(combo_name, attach_bone), ...]), ...]"""
    fighters: list[tuple[str, list[tuple[str, str]]]] = []
    # Bloecke: <var>.id = "..."  ... <var>.combos = [ ... ]
    for m in re.finditer(r'(\w+)\.id\s*=\s*"([^"]+)"', src):
        var, fid = m.group(1), m.group(2)
        cm = re.search(
            re.escape(var) + r"\.combos\s*=\s*\[(.*?)\n\t\]", src[m.end():], re.S)
        if not cm:
            continue
        combos = re.findall(
            r'_combo\(\s*"([^"]+)"[^)]*?"(?:#[0-9A-Fa-f]{6})"\s*,\s*"([^"]+)"\s*\)',
            cm.group(1))
        fighters.append((fid, combos))
    return fighters


def main() -> int:
    if not ROSTER.exists():
        print(f"FEHLER: {ROSTER} fehlt.", file=sys.stderr)
        return 2
    src = ROSTER.read_text(encoding="utf-8")

    parts = parse_parts(src)
    if not parts:
        print("FEHLER: keine Koerperteile in FighterRoster.build_body gefunden.")
        return 1

    # Bone-Map wie _build_bone_map: erster Treffer je Schluessel gewinnt.
    bone_map: dict[str, str] = {}
    for p in parts:
        key = classify_name(p)
        if key and key not in bone_map:
            bone_map[key] = p
    bone_map.setdefault("Root", "<Figur-Wurzel>")

    print("Koerperteile:", ", ".join(parts))
    print("Erkannte Aufhaengepunkte:")
    for k in sorted(bone_map):
        print(f"  {k:8s} <- {bone_map[k]}")
    print()

    for needed in ("Head", "Hand_R", "Hand_L", "Root"):
        if needed not in bone_map:
            print(f"FEHLER: Aufhaengepunkt '{needed}' wird von keinem "
                  f"Koerperteil erzeugt.")
            return 1

    fighters = parse_fighters(src)
    if not fighters:
        print("FEHLER: keine Kaempfer erkannt.")
        return 1

    problems: list[str] = []
    print(f"Pruefe {len(fighters)} Kaempfer:\n")
    for fid, combos in fighters:
        if not combos:
            problems.append(f"{fid}: keine Combos -- SkillData.is_valid() "
                            f"waere false und der Kaempfer koennte nicht antreten.")
            continue
        print(f"  {fid} ({len(combos)} Combos)")
        for cname, bone in combos:
            resolved = normalize_attach_bone(bone, set(bone_map))
            target = bone_map.get(resolved, "?")
            flag = "ok"
            # Ein Combo, der auf Root zurueckfaellt, obwohl er an die Hand
            # sollte, ist ein echter Fehler.
            if resolved == "Root" and bone.lower() not in (
                    "root", "hips", "pelvis"):
                flag = "FALLBACK"
                problems.append(
                    f"{fid}/{cname}: attach_bone \"{bone}\" nicht gefunden, "
                    f"faellt auf Root zurueck.")
            print(f"      {cname:22s} {bone:8s} -> {resolved:8s} ({target}) [{flag}]")
        print()

    if problems:
        print("== Befunde ==")
        for p in problems:
            print(f"  x {p}")
        return 1

    print("Alle Combos haengen an einem echten Koerperteil.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
