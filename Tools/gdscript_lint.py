#!/usr/bin/env python3
"""Statische Plausibilitaetspruefung fuer GDScript.

Kein Ersatz fuer den Godot-Parser -- aber der ist in vielen CI-Umgebungen
nicht verfuegbar, und die haeufigsten Fehler beim Schreiben von GDScript ohne
laufenden Editor sind mechanischer Natur:

1. Einrueckung mit Leerzeichen statt Tabs (Godot mischt beides nicht gern)
2. unbalancierte Klammern
3. Aufruf einer Methode am eigenen Skript, die es gar nicht gibt
4. Zugriff auf ein Autoload, das in project.godot nicht registriert ist
5. Methoden, die eine Basisklassen-Methode ueberschreiben (z.B. `tr`)

Aufruf:
    python3 Tools/gdscript_lint.py
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

# Methoden aus Object/Node/CanvasItem, die man nicht versehentlich
# ueberschreiben darf, weil Godot dann mit
# "The function signature doesn't match the parent" abbricht.
RESERVED_METHODS = {
    "tr", "tr_n", "free", "queue_free", "connect", "disconnect", "emit_signal",
    "get", "set", "call", "callv", "has_method", "get_class", "is_class",
    "duplicate", "add_child", "remove_child", "get_parent", "get_node",
    "get_children", "print", "notification", "to_string",
}

# Diese Namen sind Godot-Builtins, keine Autoloads.
BUILTIN_SINGLETONS = {
    "Input", "InputMap", "OS", "Engine", "Time", "JSON", "ResourceLoader",
    "ResourceSaver", "ProjectSettings", "DisplayServer", "RenderingServer",
    "PhysicsServer3D", "PhysicsServer2D", "AudioServer", "NavigationServer3D",
    "NavigationServer2D", "TranslationServer", "ClassDB", "Performance",
    "Geometry2D", "Geometry3D", "Marshalls", "IP", "JavaScriptBridge",
    "WorkerThreadPool", "ThemeDB", "EditorInterface", "GDScript",
}

TRIPLE_RE = re.compile(r'("""|\'\'\')(?:.|\n)*?\1')

problems: list[str] = []
notes: list[str] = []


def read(p: Path) -> str:
    return p.read_text(encoding="utf-8", errors="replace")


def strip_strings_and_comments(text: str) -> str:
    """Ersetzt String-Inhalte und Kommentare durch Platzhalter gleicher Laenge,
    damit Klammern darin nicht mitgezaehlt werden."""
    text = TRIPLE_RE.sub(lambda m: "\n" * m.group(0).count("\n"), text)
    out: list[str] = []
    for line in text.split("\n"):
        res: list[str] = []
        quote: str | None = None
        i = 0
        while i < len(line):
            ch = line[i]
            if quote:
                if ch == "\\":
                    i += 2
                    continue
                if ch == quote:
                    quote = None
                res.append(" ")
            else:
                if ch in "\"'":
                    quote = ch
                    res.append(" ")
                elif ch == "#":
                    break
                else:
                    res.append(ch)
            i += 1
        out.append("".join(res))
    return "\n".join(out)


def gd_files() -> list[Path]:
    return sorted(p for p in ROOT.rglob("*.gd") if ".git" not in p.parts)


def autoload_names() -> set[str]:
    proj = ROOT / "project.godot"
    if not proj.exists():
        return set()
    txt = read(proj)
    m = re.search(r"^\[autoload\]\s*$(.*?)(?=^\[|\Z)", txt, re.M | re.S)
    if not m:
        return set()
    return set(re.findall(r"^(\w+)\s*=", m.group(1), re.M))


def class_names() -> set[str]:
    out: set[str] = set()
    for f in gd_files():
        out.update(re.findall(r"^class_name\s+(\w+)", read(f), re.M))
    return out


def check_file(path: Path, autoloads: set[str], classes: set[str]) -> None:
    rel = path.relative_to(ROOT).as_posix()
    raw = read(path)
    code = strip_strings_and_comments(raw)
    # Lokale Enums sind gueltige Namensraeume (z.B. Difficulty.MEDIUM).
    local_enums = set(re.findall(r"^\s*enum\s+(\w+)", code, re.M))

    # 1. Einrueckung
    for n, line in enumerate(code.split("\n"), 1):
        if line.startswith(" ") and line.strip():
            # Fortsetzungszeilen in Klammern duerfen mit Leerzeichen anfangen,
            # aber ein Statement auf Spaltenebene 0 nicht.
            problems.append(f"{rel}:{n}: Einrueckung mit Leerzeichen "
                            f"(Godot erwartet Tabs).")
            break

    # 2. Klammern
    for name, (op, cl) in {"()": ("(", ")"), "[]": ("[", "]"),
                           "{}": ("{", "}")}.items():
        if code.count(op) != code.count(cl):
            problems.append(f"{rel}: unbalancierte Klammern {name} "
                            f"({code.count(op)}x{op} / {code.count(cl)}x{cl}).")

    # 3. reservierte Methodennamen
    for m in re.finditer(r"^\s*(?:static\s+)?func\s+(\w+)\s*\(", code, re.M):
        if m.group(1) in RESERVED_METHODS:
            line = code[:m.start()].count("\n") + 1
            problems.append(
                f"{rel}:{line}: `func {m.group(1)}()` ueberschreibt eine "
                f"Methode der Basisklasse -- in neueren Godot-Versionen ein "
                f"Parse-Fehler. Umbenennen.")

    # 4. Selbstaufrufe pruefen
    defined = set(re.findall(r"^\s*(?:static\s+)?func\s+(\w+)\s*\(", code, re.M))
    called = set(re.findall(r"(?<![\w.])_(\w+)\s*\(", code))
    for name in sorted(called):
        full = "_" + name
        if full in defined:
            continue
        # Godot-Callbacks und bekannte Engine-Methoden ignorieren
        if full.startswith(("_on_", "_ready", "_process", "_physics_process",
                            "_input", "_unhandled_input", "_draw", "_init",
                            "_enter_tree", "_exit_tree", "_gui_input",
                            "_notification", "_to_string", "_get", "_set")):
            continue
        notes.append(f"{rel}: ruft `{full}()` auf, das in dieser Datei nicht "
                     f"definiert ist (evtl. geerbt).")

    # 5. Autoload-Zugriffe
    for m in re.finditer(r"(?<![\w.])([A-Z]\w+)\.", code):
        name = m.group(1)
        if name in autoloads or name in classes or name in BUILTIN_SINGLETONS:
            continue
        if name in local_enums:
            continue
        # Godot-Typen sind CamelCase und tauchen hier nur als Namensraum auf
        # (Vector3.UP, SphereMesh.new(), Tween.TRANS_BACK ...). Sie lassen sich
        # offline nicht vollstaendig aufzaehlen, deshalb wird alles
        # durchgelassen, was diesem Muster entspricht. Die Pruefung zielt auf
        # den Fall "Autoload benutzt, aber nicht in project.godot registriert" --
        # und dafuer ist die Liste der bekannten Projekt-Autoloads massgeblich.
        # Praezise Pruefung statt Raterei: Ein Name ist nur dann ein Befund,
        # wenn es unter scripts/autoload/ eine gleichnamige Datei gibt, der
        # Name aber nicht in [autoload] von project.godot steht. Genau dieser
        # Fall knallt zur Laufzeit mit "Identifier not found".
        if (ROOT / "scripts" / "autoload" / f"{name}.gd").exists() \
                and name not in autoloads:
            line = code[:m.start()].count("\n") + 1
            problems.append(
                f"{rel}:{line}: `{name}` wird benutzt und es gibt "
                f"scripts/autoload/{name}.gd -- aber der Name ist in "
                f"project.godot nicht als Autoload registriert.")
        continue
        line = code[:m.start()].count("\n") + 1
        notes.append(f"{rel}:{line}: `{name}` sieht aus wie ein Autoload/Typ, "
                     f"ist aber weder registriert noch als class_name bekannt.")


def main() -> int:
    autoloads = autoload_names()
    classes = class_names()
    files = gd_files()
    print(f"Pruefe {len(files)} GDScript-Dateien "
          f"({len(autoloads)} Autoloads, {len(classes)} class_name)\n")

    for f in files:
        check_file(f, autoloads, classes)

    # Hinweise nur kompakt zeigen
    if notes:
        uniq = sorted(set(notes))
        print(f"== Hinweise ({len(uniq)}) ==")
        for n in uniq[:25]:
            print(f"  - {n}")
        if len(uniq) > 25:
            print(f"  ... und {len(uniq) - 25} weitere")
        print()

    if problems:
        print("== Befunde ==")
        for p in problems:
            print(f"  x {p}")
        print(f"\n{len(problems)} Problem(e).")
        return 1

    print("Keine strukturellen Fehler gefunden.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
