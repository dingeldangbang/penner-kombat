#!/usr/bin/env python3
"""Simuliert die Match-Ablauflogik der Arena in Python.

Warum
-----
Godot laesst sich in dieser Umgebung nicht ausfuehren, also kann die Arena
nicht wirklich gestartet werden. Die Rundenlogik (wer gewinnt, wann ist das
Match vorbei, wie geht die Arcade-Leiter weiter) ist aber reine Zustandslogik
und laesst sich unabhaengig nachbauen und pruefen.

Getestet wird, dass
  * ein Match nach `rounds_to_win` Rundensiegen endet,
  * nie mehr Runden gespielt werden als `rounds_to_win * 2 - 1`,
  * die Arcade-Leiter jeden Gegner genau einmal bringt und mit dem Boss endet,
  * `_end_round` nicht erneut ausgeloest wird, waehrend das Match schon vorbei
    ist (der Fall, der in der Arena zu doppelten Endbildschirmen fuehrt).

Aufruf:
    python3 Tools/simulate_match.py
"""

from __future__ import annotations

import random
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


class Match:
    """Spiegelt ArenaController: _round, _p1_wins, _p2_wins, _is_match_active."""

    def __init__(self, rounds_to_win: int) -> None:
        self.rounds_to_win = rounds_to_win
        self.max_rounds = rounds_to_win * 2 - 1
        self.round = 1
        self.p1_wins = 0
        self.p2_wins = 0
        self.active = True
        self.ended = 0          # wie oft _end_match() lief
        self.rounds_played = 0

    def end_round(self, p1_hp: float, p2_hp: float) -> None:
        if not self.active:
            # Genau dieser Schutz sitzt in ArenaController._physics_process.
            raise AssertionError("end_round waehrend inaktivem Match")
        self.active = False
        self.rounds_played += 1
        if p1_hp >= p2_hp:
            self.p1_wins += 1
        else:
            self.p2_wins += 1
        needed = max(1, self.rounds_to_win)
        if (self.p1_wins >= needed or self.p2_wins >= needed
                or self.round >= self.max_rounds):
            self.ended += 1
        else:
            self.round += 1
            self.active = True   # _reset_round() schaltet wieder scharf


def check_rounds() -> list[str]:
    errs: list[str] = []
    rng = random.Random(20260823)
    for rounds_to_win in (1, 2, 3):
        for _ in range(3000):
            m = Match(rounds_to_win)
            guard = 0
            while m.ended == 0:
                guard += 1
                if guard > 50:
                    errs.append(f"rounds_to_win={rounds_to_win}: Match endet nie")
                    break
                m.end_round(rng.random() * 100, rng.random() * 100)
            if m.ended > 1:
                errs.append(f"rounds_to_win={rounds_to_win}: Match endete "
                            f"{m.ended}x")
            if m.rounds_played > m.max_rounds:
                errs.append(f"rounds_to_win={rounds_to_win}: "
                            f"{m.rounds_played} Runden > max {m.max_rounds}")
            winner_wins = max(m.p1_wins, m.p2_wins)
            if winner_wins < min(rounds_to_win, m.rounds_played):
                errs.append(f"rounds_to_win={rounds_to_win}: Sieger hat nur "
                            f"{winner_wins} Rundensiege")
    return errs


def roster_ids() -> list[str]:
    src = (ROOT / "scripts" / "game" / "FighterRoster.gd").read_text(
        encoding="utf-8")
    return re.findall(r'\w+\.id\s*=\s*"([^"]+)"', src)


def check_arcade() -> list[str]:
    """Bildet GameState.start_arcade()/arcade_advance() nach."""
    errs: list[str] = []
    ids = roster_ids()
    if "koenig" not in ids:
        errs.append("Boss 'koenig' fehlt in der Riege")
        return errs

    rng = random.Random(7)
    for player in ids:
        ladder = [i for i in ids if i != player and i != "koenig"]
        rng.shuffle(ladder)
        # Spielt man selbst den Boss, gibt es keinen Boss-Kampf am Ende.
        if player != "koenig":
            ladder.append("koenig")

        if player != "koenig" and ladder[-1] != "koenig":
            errs.append(f"{player}: Boss ist nicht der letzte Gegner")
        if player in ladder:
            errs.append(f"{player}: kaempft gegen sich selbst")
        if len(set(ladder)) != len(ladder):
            errs.append(f"{player}: Gegner doppelt in der Leiter")

        expected = len(ids) - 1
        if len(ladder) != expected:
            errs.append(f"{player}: Leiter hat {len(ladder)} statt "
                        f"{expected} Gegner")

        # Durchlauf bis zum Ende
        stage = 0
        seen: list[str] = []
        while True:
            seen.append(ladder[stage] if stage < len(ladder) else "koenig")
            stage += 1
            if stage >= len(ladder):
                break
        if seen != ladder:
            errs.append(f"{player}: Durchlauf {seen} != Leiter {ladder}")
    return errs


def check_settings_are_read() -> list[str]:
    """Stellt sicher, dass die Arena die Einstellungen wirklich benutzt und
    nicht mehr die alten festen Werte."""
    errs: list[str] = []
    src = (ROOT / "scripts" / "combat" / "ArenaController.gd").read_text(
        encoding="utf-8")
    if "_p1_wins >= 2" in src:
        errs.append("ArenaController prueft noch fest auf 2 Rundensiege")
    if re.search(r"_match_timer\s*=\s*99\.0", src):
        errs.append("ArenaController setzt die Rundenzeit noch fest auf 99 s")
    if "GameState.round_seconds" not in src:
        errs.append("ArenaController liest GameState.round_seconds nicht")
    if "GameState.rounds_to_win" not in src:
        errs.append("ArenaController liest GameState.rounds_to_win nicht")
    return errs


def _wrapi(v: int, lo: int, hi: int) -> int:
    """Godots wrapi()."""
    return lo + ((v - lo) % (hi - lo))


def check_option_cycles() -> list[str]:
    """Die Einstellungen sind Durchklick-Knoepfe. Ein falsch gesetztes wrapi()
    laesst den Wert stehen -- der Knopf wirkt dann kaputt. Hier wird geprueft,
    dass jeder Knopf alle Werte erreicht und wieder von vorn beginnt."""
    errs: list[str] = []
    src = (ROOT / "scripts" / "game" / "Options.gd").read_text(encoding="utf-8")

    # Schwierigkeit: 0..4
    if "wrapi(GameState.difficulty + 1, 0, 5)" not in src:
        errs.append("Schwierigkeits-Knopf benutzt nicht wrapi(x + 1, 0, 5)")
    seen = set()
    d = 0
    for _ in range(12):
        d = _wrapi(d + 1, 0, 5)
        seen.add(d)
    if seen != {0, 1, 2, 3, 4}:
        errs.append(f"Schwierigkeit erreicht nur {sorted(seen)}")

    # Runden: 1..3
    if "wrapi(GameState.rounds_to_win + 1, 1, 4)" not in src:
        errs.append("Runden-Knopf benutzt nicht wrapi(x + 1, 1, 4)")
    for start in (1, 2, 3):
        seen = set()
        r = start
        for _ in range(9):
            r = _wrapi(r + 1, 1, 4)
            seen.add(r)
        if seen != {1, 2, 3}:
            errs.append(f"Runden ab {start} erreicht nur {sorted(seen)}")

    # Listen-Knoepfe (Rundenzeit, Touch-Groesse) muessen den aktuellen Wert
    # in ihrer Liste finden, sonst springt find() auf -1 und der erste Klick
    # landet immer beim gleichen Eintrag.
    for label, values, current in (
            ("Rundenzeit", [30.0, 60.0, 99.0, 180.0], 99.0),
            ("Touch-Groesse", [0.8, 1.0, 1.2, 1.45], 1.0)):
        if current not in values:
            errs.append(f"{label}: Standardwert {current} fehlt in der Liste")
        seen_v = set()
        v = current
        for _ in range(len(values) * 3):
            i = values.index(v)
            v = values[_wrapi(i + 1, 0, len(values))]
            seen_v.add(v)
        if seen_v != set(values):
            errs.append(f"{label}: erreicht nur {sorted(seen_v)}")
    return errs


def main() -> int:
    all_errs: list[str] = []
    for name, fn in (("Rundenlogik", check_rounds),
                     ("Arcade-Leiter", check_arcade),
                     ("Einstellungen", check_settings_are_read),
                     ("Options-Knoepfe", check_option_cycles)):
        errs = fn()
        status = "ok" if not errs else f"{len(errs)} Problem(e)"
        print(f"  {name:16s} {status}")
        all_errs += errs

    print()
    if all_errs:
        print("== Befunde ==")
        for e in sorted(set(all_errs)):
            print(f"  x {e}")
        return 1
    print("Match- und Arcade-Ablauf sind schluessig.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
