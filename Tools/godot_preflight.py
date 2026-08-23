#!/usr/bin/env python3
"""
Preflight-Prüfung für das Godot-4-Projekt (Penner Kombat).

Ersetzt keinen Godot-Export, fängt aber genau die Fehlerklassen ab, die einen
CI-Build erst nach 5-10 Minuten scheitern lassen -- oder, schlimmer, ein APK
erzeugen, das beim Start crasht:

  * `res://`-Referenzen in .tscn/.gd/.cfg/.godot, die ins Leere zeigen
  * `@onready var x = $Pfad/Zum/Node` mit einem Pfad, den die zugehörige
    Szene gar nicht enthält  (klassischer Nil-Crash beim ersten Frame)
  * Autoloads, deren Skript fehlt
  * Nicht-Ressourcen-Dateien (z. B. .json), die per FileAccess von `res://`
    gelesen, aber vom Export-Filter nicht mitverpackt werden
  * Hauptszene fehlt / Export-Preset-Name stimmt nicht

Aufruf:  python3 Tools/godot_preflight.py
Rückgabe: 0 = sauber, 1 = Befunde
"""

from __future__ import annotations

import re
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

problems: list[str] = []
notes: list[str] = []


def fail(msg: str) -> None:
    problems.append(msg)


def note(msg: str) -> None:
    notes.append(msg)


# ------------------------------------------------------------ Hilfsfunktionen

def read(p: Path) -> str:
    return p.read_text(encoding="utf-8", errors="replace")


def project_files(*globs: str) -> list[Path]:
    out: list[Path] = []
    for g in globs:
        out.extend(sorted(ROOT.glob(g)))
    return out


# ------------------------------------------------------- 1. res://-Referenzen

RES_RE = re.compile(r'res://([A-Za-z0-9_./-]+)')


def check_res_refs() -> None:
    scanned = project_files(
        "**/*.tscn", "**/*.gd", "**/*.tres", "**/*.cfg", "project.godot"
    )
    seen: set[tuple[str, str]] = set()
    for f in scanned:
        if ".godot/" in f.as_posix():
            continue
        rel = f.relative_to(ROOT).as_posix()
        for m in RES_RE.finditer(read(f)):
            target = m.group(1)
            if not target or (rel, target) in seen:
                continue
            seen.add((rel, target))
            if not (ROOT / target).exists():
                fail(f"Fehlende Ressource: res://{target}  (referenziert in {rel})")


# --------------------------------------------- 2. @onready-Node-Pfade prüfen

NODE_RE = re.compile(
    r'^\[node name="([^"]+)"(?:\s+type="([^"]+)")?(?:\s+parent="([^"]*)")?',
    re.M,
)
SCRIPT_RE = re.compile(r'ext_resource type="Script" path="res://([^"]+)"')
ONREADY_RE = re.compile(
    r'@onready\s+var\s+(\w+)\s*(?::\s*[\w.]+\s*)?=\s*\$([^\s#]+)'
)


def scene_nodes(scene: Path) -> dict[str, str]:
    nodes: dict[str, str] = {}
    for m in NODE_RE.finditer(read(scene)):
        name, ntype, parent = m.group(1), m.group(2), m.group(3)
        if parent is None:
            path = ""
        elif parent == ".":
            path = name
        else:
            path = f"{parent}/{name}"
        nodes[path] = ntype or "(Instanz)"
    return nodes


def check_onready_paths() -> None:
    scenes = project_files("scenes/**/*.tscn")
    script_to_scene: dict[str, Path] = {}
    nodes_of: dict[Path, dict[str, str]] = {}

    for s in scenes:
        nodes_of[s] = scene_nodes(s)
        for m in SCRIPT_RE.finditer(read(s)):
            script_to_scene.setdefault(m.group(1), s)

    for script, scene in sorted(script_to_scene.items()):
        sp = ROOT / script
        if not sp.exists():
            continue
        nodes = nodes_of[scene]
        for m in ONREADY_RE.finditer(read(sp)):
            var, path = m.group(1), m.group(2).strip().strip('"')
            if path.startswith(("/root", "%")):
                continue  # absolute Pfade / unique names nicht prüfbar
            if path not in nodes:
                fail(
                    f"@onready-Pfad zeigt ins Leere: ${path} (var {var}) in "
                    f"{script} -- Szene {scene.relative_to(ROOT).as_posix()} "
                    f"hat diesen Node nicht"
                )


# ------------------------------------------------------------- 3. Autoloads

def check_autoloads() -> None:
    pg = ROOT / "project.godot"
    if not pg.exists():
        fail("project.godot fehlt -- das ist kein Godot-Projekt.")
        return
    txt = read(pg)

    m = re.search(r'run/main_scene="res://([^"]+)"', txt)
    if not m:
        fail("project.godot: run/main_scene ist nicht gesetzt.")
    elif not (ROOT / m.group(1)).exists():
        fail(f"Hauptszene fehlt: res://{m.group(1)}")

    block = re.search(r'^\[autoload\]\s*$(.*?)(?=^\[|\Z)', txt, re.M | re.S)
    if block:
        for line in block.group(1).splitlines():
            am = re.match(r'\s*(\w+)\s*=\s*"\*?res://([^"]+)"', line)
            if am and not (ROOT / am.group(2)).exists():
                fail(f"Autoload {am.group(1)}: Skript fehlt (res://{am.group(2)})")


# --------------------------- 4. Nicht-Ressourcen im Export-Filter enthalten?

# Godot exportiert bei export_filter="all_resources" nur echte Ressourcen.
# Dateien wie .json werden nur mitverpackt, wenn sie im include_filter stehen.
NON_RESOURCE_SUFFIXES = {".json", ".txt", ".csv", ".md", ".cfg"}


def check_export_filters() -> None:
    presets = ROOT / "export_presets.cfg"
    if not presets.exists():
        fail("export_presets.cfg fehlt -- ohne Preset kein Export.")
        return
    txt = read(presets)

    if 'name="Android"' not in txt:
        fail('export_presets.cfg enthält kein Preset mit name="Android".')

    inc = re.search(r'include_filter="([^"]*)"', txt)
    patterns = [p.strip() for p in (inc.group(1) if inc else "").split(",") if p.strip()]

    # Alle res://-Pfade einsammeln, die per FileAccess/Dateipfad gelesen werden
    referenced: set[str] = set()
    for f in project_files("scripts/**/*.gd", "scenes/**/*.tscn"):
        for m in RES_RE.finditer(read(f)):
            referenced.add(m.group(1))

    for target in sorted(referenced):
        suffix = Path(target).suffix.lower()
        if suffix not in NON_RESOURCE_SUFFIXES:
            continue
        if not (ROOT / target).exists():
            continue  # schon von check_res_refs gemeldet
        covered = any(
            pat == f"*{suffix}" or pat.endswith(f"*{suffix}") or pat == target
            for pat in patterns
        )
        if not covered:
            fail(
                f"res://{target} wird zur Laufzeit gelesen, ist aber keine "
                f"Godot-Ressource und steht nicht im include_filter der "
                f"export_presets.cfg -- im APK fehlt die Datei."
            )


# ------------------------------------------------------------- 5. Sonstiges

TRIPLE_RE = re.compile(r'"""(?:.|\n)*?"""|\'\'\'(?:.|\n)*?\'\'\'')


def check_launcher_icons() -> None:
    """Leere launcher_icons-Felder ergeben ein APK mit dem Godot-Standardicon."""
    presets = ROOT / "export_presets.cfg"
    if not presets.exists():
        return
    txt = read(presets)

    fields = {
        "launcher_icons/main_192x192": (192, "Launcher-Icon"),
        "launcher_icons/adaptive_foreground_432x432": (432, "Adaptive Vordergrund"),
        "launcher_icons/adaptive_background_432x432": (432, "Adaptive Hintergrund"),
    }
    for key, (expected, label) in fields.items():
        m = re.search(re.escape(key) + r'\s*=\s*"([^"]*)"', txt)
        if not m:
            continue
        val = m.group(1).strip()
        if not val:
            note(f"{label} nicht gesetzt ({key}) -- das APK bekommt das "
                 f"Godot-Standardicon. `python3 Tools/make_icons.py` erzeugt es.")
            continue
        if not val.startswith("res://"):
            fail(f"{key} muss ein res://-Pfad sein, ist aber \"{val}\".")
            continue
        p = ROOT / val[len("res://"):]
        if not p.exists():
            fail(f"{label}: {val} fehlt im Repo (aus export_presets.cfg).")
            continue
        # PNG-Header und Kantenlaenge pruefen.
        data = p.read_bytes()
        if data[:8] != b"\x89PNG\r\n\x1a\n":
            fail(f"{label}: {val} ist keine PNG-Datei.")
            continue
        w, h = struct.unpack(">II", data[16:24])
        if (w, h) != (expected, expected):
            fail(f"{label}: {val} ist {w}x{h}, erwartet {expected}x{expected}.")


def check_sdk_consistency() -> None:
    """min/target SDK stehen in project.godot UND export_presets.cfg.
    Laufen sie auseinander, gewinnt eine der beiden Stellen stillschweigend."""
    proj = ROOT / "project.godot"
    presets = ROOT / "export_presets.cfg"
    if not (proj.exists() and presets.exists()):
        return
    ptxt, etxt = read(proj), read(presets)

    for label, pkey, ekey in (
        ("min SDK", r"min_sdk_version\s*=\s*(\d+)", r"gradle_build/min_sdk\s*=\s*(\d+)"),
        ("target SDK", r"target_sdk_version\s*=\s*(\d+)", r"gradle_build/target_sdk\s*=\s*(\d+)"),
    ):
        pm, em = re.search(pkey, ptxt), re.search(ekey, etxt)
        if pm and em and pm.group(1) != em.group(1):
            fail(f"{label} weicht ab: project.godot={pm.group(1)}, "
                 f"export_presets.cfg={em.group(1)}.")


def check_misc() -> None:
    for gd in project_files("scripts/**/*.gd"):
        rel = gd.relative_to(ROOT).as_posix()
        # Mehrzeilige Strings ausblenden: deren Einrückung ist Inhalt, kein Code.
        txt = TRIPLE_RE.sub(lambda m: "\n" * m.group(0).count("\n"), read(gd))
        if "\t" in txt and re.search(r'^ {2,}\S', txt, re.M):
            note(f"{rel}: mischt Tabs und Leerzeichen als Einrückung.")


# ------------------------------------------------------------------- main

def main() -> int:
    check_autoloads()
    check_res_refs()
    check_onready_paths()
    check_export_filters()
    check_launcher_icons()
    check_sdk_consistency()
    check_misc()

    if notes:
        print("== Hinweise ==")
        for n in notes:
            print(f"  - {n}")
        print()

    if problems:
        print("== Befunde ==")
        for p in problems:
            print(f"  ✗ {p}")
        print(f"\n{len(problems)} Problem(e) gefunden.")
        return 1

    print("✓ Preflight sauber -- Szenen, Autoloads, Ressourcen und "
          "Export-Filter sind konsistent.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
