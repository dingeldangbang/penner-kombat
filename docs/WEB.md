# 🌐 BROWSER-FASSUNG — die Alternative ohne Unity

`web/` enthält eine vollständige, **sofort spielbare** Fassung von Penner Kombat,
die im Browser läuft: Handy, Tablet, alter Mac, egal. Kein Unity, keine Lizenz,
kein APK, keine Installation.

---

## 1. Starten

**Lokal:**

```bash
cd web && python3 -m http.server 8080
# dann http://<rechner-ip>:8080 im Handy-Browser öffnen
```

**Öffentlich (GitHub Pages, ohne Unity-Lizenz):**
Settings → Pages → Source *Deploy from a branch* → Branch `main`, Ordner `/ (root)`.
Danach liegt das Spiel unter `https://<benutzer>.github.io/penner-kombat/web/`.

---

## 2. Wie nah ist das an der Unity-Fassung?

**Die Kampfwerte sind identisch** — sie stammen Zahl für Zahl aus dem C#-Code
(`web/src/data.js` nennt zu jedem Block die Quelldatei):

| Bereich | Übernommen |
|---|---|
| Roster | alle 9 Charaktere mit HP, Tempo, Schaden, Reichweite aus `FighterDatabase.EnsureDefaultRoster()` |
| Statur | Höhe, Radius, Masse, Hitbox-Faktor aus `StatureTable` |
| Blocken | 78 % Reduktion (`BlockDamageReduction 0.22`) |
| Combo-Fenster | 2,0 s |
| Rolle | 3,2 m in 0,32 s, 0,2 s i-Frames, 0,8 s Abklingzeit |
| Sprung/Schwerkraft | 9,5 / 26 |
| Fatal Blow | Leiste 100, +12 pro Treffer, +8 beim Einstecken, 28–44 Schaden |
| Med-Kapsel | 2 Ladungen, 25 HP, 1,2 s, 8 s Abklingzeit |
| KI | alle 6 Stufen mit exakt den Werten aus `AIController.ApplyDifficulty` |
| Runden | Best-of-1/3/5, 99 s, Sieg nach HP bei Zeitablauf |
| Beat | Combo treibt das Tempo (140 → max 180 BPM) wie in `MusicSync` |

**Was abweicht:**

| Punkt | Browser | Unity |
|---|---|---|
| Darstellung | 2,5D-Canvas, Kapseln | 3D, Kapseln (bis Modelle da sind) |
| Spezials | je 2 pro Charakter, vereinfacht | 5 pro Charakter über `MoveCatalog` |
| Kommandoeingaben | Buttons statt ↓↘→ | volle Numpad-Notation mit Puffer |
| Fatalities, Krypta, Story | fehlen | vorhanden |
| Waffen, Power-Ups, Zuschauer | fehlen | vorhanden |
| Online | fehlt | Relay in `server/` |

Kurz: **Kern identisch, Beiwerk fehlt.**

---

## 3. Was getestet ist

```bash
node --test web/test/sim.test.mjs
```

11 Prüfungen, alle grün — Roster-Werte, Statur-Tabelle, KI-Stufen, 78-%-Block,
i-Frames der Rolle, Trefferauflösung samt Fatal-Blow-Ladung, Med-Heilung,
Rundenende nach Zeit, komplettes Match gegen die Boss-KI, Arena-Grenzen und
Kollisionstrennung.

Das ist mehr, als vom C#-Teil behauptet werden kann: **der wurde nie kompiliert.**

---

## 4. Steuerung

| Aktion | Handy | Tastatur P1 | Tastatur P2 |
|---|---|---|---|
| Bewegen | Joystick links | W A S D | Pfeiltasten |
| Leicht / Schwer | □ / △ | J / K | Numpad 1 / 2 |
| Blocken | BLOCK halten | Shift | Numpad 3 |
| Springen | ✕ | Leertaste | Numpad 0 |
| Spezial 1 / 2 | ○ / S2 | U / I | Numpad 4 / 5 |
| Rolle | ROLLE | O | Numpad 6 |
| Fatal Blow | X-RAY | Y | Numpad + |
| Med | MED | H | Numpad Enter |
| Menü | MENÜ oben | Esc / F1 | — |

**Zwei Spieler an einem Gerät** gehen hier tatsächlich — über zwei Tastaturhälften.
Auf dem Handy bedient der Touch nur Spieler 1.

---

## 5. Grenzen, ohne Beschönigung

- **Ton ist synthetisiert** (WebAudio), keine Musik im Sinne des Soundtrack-Dokuments.
- **Keine Modelle**: Kapseln mit Kopf, Armen und Blickrichtung.
- **Kein Online-Modus.**
- **Von mir nicht im Browser gesehen.** Die Logik ist getestet, die Darstellung nicht:
  Ich habe hier keinen Browser. Wenn etwas nicht zeichnet, sag Bescheid.
- Die Browser-Fassung ist **kein Ersatz** für die Unity-Version, sondern der Weg,
  jetzt schon zu spielen, während die Unity-Fassung auf einen tauglichen Rechner
  oder einen CI-Build wartet.
