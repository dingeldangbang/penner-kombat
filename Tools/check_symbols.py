#!/usr/bin/env python3
"""
Statische Referenzprüfung für Penner Kombat.

Ersetzt keinen Compiler — findet aber genau die Fehlerklasse, die in einer
Umgebung ohne Unity sonst erst beim ersten Play auffällt:

  * Aufrufe wie `Foo.Bar(...)` auf projekteigenen Typen, wo `Bar` nicht existiert
  * `AddComponent<T>()` / `GetComponent<T>()` mit unbekanntem projekteigenem Typ
  * Enum-Werte, die es nicht gibt   (z. B. `HitTier.Ultra`)
  * Vererbung auf nicht vorhandene Basisklassen

Aufruf:  python3 Tools/check_symbols.py
Rückgabe: 0 = sauber, 1 = Befunde
"""

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "Assets"

# ---------------------------------------------------------------- Quelltext säubern

def strip_code(src: str) -> str:
    """Entfernt Kommentare und Zeichenketten, damit Regex nicht darin stolpert."""
    out, i, n = [], 0, len(src)
    while i < n:
        c = src[i]
        if c == '/' and i + 1 < n and src[i + 1] == '/':
            while i < n and src[i] != '\n':
                i += 1
        elif c == '/' and i + 1 < n and src[i + 1] == '*':
            i += 2
            while i + 1 < n and not (src[i] == '*' and src[i + 1] == '/'):
                i += 1
            i += 2
        elif c == '@' and i + 1 < n and src[i + 1] == '"':
            i += 2
            while i < n:
                if src[i] == '"':
                    if i + 1 < n and src[i + 1] == '"':
                        i += 2
                        continue
                    i += 1
                    break
                i += 1
        elif c == '"':
            i += 1
            while i < n and src[i] != '"':
                if src[i] == '\\':
                    i += 1
                i += 1
            i += 1
        elif c == "'":
            i += 1
            while i < n and src[i] != "'":
                if src[i] == '\\':
                    i += 1
                i += 1
            i += 1
        else:
            out.append(c)
            i += 1
    return ''.join(out)


# ---------------------------------------------------------------- Typen einsammeln

TYPE_RE = re.compile(
    r'\b(?:public|internal|protected|private|static|abstract|sealed|partial|\s)*'
    r'\b(class|struct|enum|interface)\s+(\w+)\s*(?::\s*([^\{]+))?\{', re.S)

MEMBER_RE = re.compile(
    r'\b(?:public|internal|protected|private|static|virtual|override|abstract|readonly|const|'
    r'async|event|extern|new|sealed|partial|unsafe|\s)+'
    r'(?:[\w<>\[\],\.\?]+\s+)?(\w+)\s*(?:\(|=|;|\{|=>)')


def collect_types(files):
    """{Typname: {'kind':…, 'bases':[…], 'members':set(), 'file':…}}"""
    types = {}
    for path in files:
        code = strip_code(path.read_text(encoding='utf-8', errors='replace'))
        for match in TYPE_RE.finditer(code):
            kind, name, bases = match.group(1), match.group(2), match.group(3) or ''
            body = extract_block(code, match.end() - 1)
            members = set()
            if kind == 'enum':
                for value in re.findall(r'(\w+)\s*(?:=[^,}]+)?\s*(?:,|$)', body):
                    members.add(value)
            else:
                for m in MEMBER_RE.finditer(body):
                    members.add(m.group(1))
                # Felder ohne Modifikator-Kette (z. B. "int foo;") mitnehmen
                for m in re.finditer(r'\b[\w<>\[\],\.\?]+\s+(\w+)\s*[;=]', body):
                    members.add(m.group(1))
            base_list = [b.strip().split('<')[0] for b in bases.split(',') if b.strip()]
            entry = types.setdefault(name, {'kind': kind, 'bases': [], 'members': set(),
                                            'file': str(path.relative_to(ROOT))})
            entry['members'] |= members
            entry['bases'] += base_list
            entry['kind'] = kind
    return types


def extract_block(code: str, brace_index: int) -> str:
    depth, i, n = 0, brace_index, len(code)
    start = brace_index + 1
    while i < n:
        if code[i] == '{':
            depth += 1
        elif code[i] == '}':
            depth -= 1
            if depth == 0:
                return code[start:i]
        i += 1
    return code[start:]


def all_members(name, types, seen=None):
    seen = seen or set()
    if name in seen or name not in types:
        return set()
    seen.add(name)
    result = set(types[name]['members'])
    for base in types[name]['bases']:
        result |= all_members(base, types, seen)
    return result


# ---------------------------------------------------------------- Prüfungen

STATIC_CALL_RE = re.compile(r'\b([A-Z]\w+)\.(\w+)\s*(?:\(|<)')
ENUM_USE_RE = re.compile(r'\b([A-Z]\w+)\.(\w+)\b')
GENERIC_RE = re.compile(r'\b(?:AddComponent|GetComponent|GetComponentInChildren|'
                        r'FindObjectOfType|FindObjectsOfType|LoadAssetAtPath|GetComponents)<(\w+)>')

# Unity-/BCL-Typen, die wir nicht kennen müssen
IGNORED_PREFIX = ('Unity', 'System', 'TMPro', 'GLTFast')


def main():
    files = sorted(SRC.rglob('*.cs'))
    types = collect_types(files)
    own = {n for n, t in types.items()}
    findings = []

    for path in files:
        rel = str(path.relative_to(ROOT))
        code = strip_code(path.read_text(encoding='utf-8', errors='replace'))
        lines = code.split('\n')

        for lineno, line in enumerate(lines, 1):
            # 1. statische Aufrufe / Enum-Werte auf eigenen Typen
            for regex, kinds in ((STATIC_CALL_RE, ('class', 'struct', 'enum', 'interface')),
                                 (ENUM_USE_RE, ('enum',))):
                for m in regex.finditer(line):
                    tname, member = m.group(1), m.group(2)
                    if tname not in own:
                        continue
                    if types[tname]['kind'] not in kinds:
                        continue
                    if member in all_members(tname, types):
                        continue
                    if member in ('Instance', 'this', 'base'):
                        continue
                    findings.append(f"{rel}:{lineno}  {tname}.{member} — Mitglied nicht gefunden")

            # 2. Generische Typparameter
            for m in GENERIC_RE.finditer(line):
                tname = m.group(1)
                if tname in own or tname.startswith(IGNORED_PREFIX):
                    continue
                if tname[0].islower():
                    continue
                # Unity-Typen sind uns unbekannt → nur melden, wenn es "unser" Namensschema ist
                if tname.startswith(('Fighter', 'Penner', 'Pk', 'Arena', 'Combo', 'Touch')):
                    findings.append(f"{rel}:{lineno}  <{tname}> — Typ im Projekt nicht gefunden")

    # 3. Basisklassen
    for name, entry in sorted(types.items()):
        for base in entry['bases']:
            if base in own or base.startswith(IGNORED_PREFIX):
                continue
            if base[0] == 'I' and len(base) > 1 and base[1].isupper():
                continue  # Interface aus Unity/BCL
            known_unity = {
                'MonoBehaviour', 'ScriptableObject', 'Editor', 'EditorWindow', 'AssetPostprocessor',
                'PropertyDrawer', 'StateMachineBehaviour', 'Attribute', 'Exception', 'EventArgs',
            }
            if base in known_unity:
                continue
            findings.append(f"{entry['file']}  class {name} : {base} — Basisklasse unbekannt")

    print(f"Geprüft: {len(files)} Dateien, {len(types)} Typen")
    if findings:
        print(f"\n⚠ {len(findings)} Befund(e):\n")
        for f in findings:
            print("  " + f)
        return 1
    print("✅ Keine offensichtlichen Referenzfehler gefunden.")
    return 0


if __name__ == '__main__':
    sys.exit(main())
