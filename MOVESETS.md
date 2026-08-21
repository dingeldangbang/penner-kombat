# 🥋 Movesets — Alle 9 Charaktere mit Frame-Daten

Alle Spezialbewegungen sind im Code über `MoveCatalog` (+ `CommandInput`) umgesetzt.
Frame-Werte: `Startup / Active / Recovery` in Frames (1 Frame = 1/60 s).

## 1. Le Binde — „Der Schlachter vom Blauen Eimer"

| Move | Eingabe | Startup | Active | Recovery | Schaden | Effekt |
|------|---------|--------|--------|----------|---------|--------|
| Leichter Angriff | □ | 7 | 4 | 14 | 6 % | – |
| Schwerer Angriff | △ | 12 | 6 | 20 | 13 % | Wallbounce |
| Flaschenhals | ↓↘→ + □ | 14 | 4 | 28 | 11 % + 6 % DoT | Grab, Blutung |
| EX-Flaschenhals | ↓↘→ + □□ | 14 | 4 | 28 | 15 % + 8 % DoT | Splitter am Boden |
| Der große Schwung | ← → + △ | 21 | 6 | 22 | 16 % | Armor 8–20, Wallbounce |
| REIF! | ↓↓ + ○ | 30 | – | – | – | Buff +80 % auf nächsten Treffer |
| Mops-Kommando | ↓↘→ + ○ | 50 | – | – | – | Debuff Gegner, Objekte kippen |
| Aus der Pfanne | →→ + △ | 11 | 4 | 18 | 14 % + Burn | Anti-Air, verbraucht Fett |
| X-Ray (Betriebsunfall) | L1 + R1 | 1 | – | – | 34 % | Stirn, Kiefer, L4/L5 |
| Fatality 1 | ←→←→△ | – | – | – | 100 % | Mise en Place |
| Fatality 2 | ↓↓←→○ | – | – | – | 100 % | Zwei Portionen |
| Brutality | s.o. | – | – | – | 100 % | Hausmannskost |

## 2. Mell — „Die Schnellste Frau von Pankow"

| Move | Eingabe | Startup | Active | Recovery | Schaden | Effekt |
|------|---------|--------|--------|----------|---------|--------|
| Leichter Angriff | □ | 5 | 3 | 10 | 5 % | – |
| Schwerer Angriff | △ | 9 | 5 | 16 | 10 % | – |
| Pampe | ↓↙← + □ | 16 | 6 | 20 | 6 % | Bodensludge (2 s), Pfütze |
| Doppelschicht | →→ + □ | 9 | 4 | 4+14 | 7 % + 9 % | 2. unblockbar wenn 1. traf |
| Erste Hilfe | ↓↓ + △ | 40 | – | – | +12 % HP | +40 bpm, verwundbar |
| Defi | ←↙↓↘→ + ○ | 13 | 4 | 18 | 8 % | Buff-Purge |
| Sechzehn Stunden | ↓↘→↓↘→ + △ | 15 | 500 ms | – | 38 % | Puls ≥160, 11 Treffer |
| X-Ray (Triage) | L1 + R1 | 1 | – | – | 33 % | Sternum, Nieren, Zeitansage |
| Fatality | →←→←□ | – | – | – | 100 % | Stabile Seitenlage |

**Puls-System:** 60–100 normal · 100–160 +15 %/−0,4 %/s · 160–200 +35 %/−1,2 %/s · 200–220 +50 %/kein Block/−3 %/s · 220 = Blackout (3 s, →60).

## 3. Mojo Bob — „Der Glücksbringer"

| Move | Eingabe | Startup | Active | Recovery | Schaden | Effekt |
|------|---------|--------|--------|----------|---------|--------|
| Leichter Angriff | □ | 8 | 5 | 15 | 7 % | – |
| Schwerer Angriff | △ | 14 | 6 | 20 | 12 % | – |
| Riesenschwanz | ↓↘→ + □ | 19 | 6 | 24 | 12 % (Krit 29 %) | Größte Reichweite |
| EX-Riesenschwanz | ↓↘→ + □□ | 19 | 18 | 24 | 12 % ×3 | Jeder Treffer Krit-würfelt |
| Beutelchen | ←↙↓ + ○ | 22 | – | – | variabel | Zufallseffekt (1–6) |
| Löffelsturm | →↘↓↙← + □ | 24 | – | – | 1,5 % pro Löffel | 17 Löffel, jeder Krit-würfelt |
| Fünfzig Cent | ↓↓ + □ | 15 | – | – | – | Münze werfen |
| Dingeneldang! | →→↓↓ + ○○ | 30 | – | – | – | 100 % Krit für 8 s, 7 Mojo nötig |
| X-Ray (Hausgewinn) | L1 + R1 | 1 | – | – | 28 % oder 44 % | Münzwurf |
| Fatality | ←←→→□ | – | – | – | 100 % | Alles oder Nichts (1/3 Überleben) |

**Mojo-System:** Basis-Krit 15 % · ×2,4 · Mojo 0–7 · Wette R1+Richtung +10 %/5 s · Verlust = alle Mojo + 10 % HP.

## 4. Dieter — „Der Kanal-König"

| Move | Eingabe | Startup | Active | Recovery | Schaden | Effekt |
|------|---------|--------|--------|----------|---------|--------|
| Schraubenschlüssel | ↓↘→ + □ | 15 | 4 | 20 | 14 % | Projektil |
| Abflussreiniger | →↘↓↙← + △ | 25 | – | – | – | Buff +30 % für 10 s |
| Rohrbruch | ↓↓ + △ | 30 | – | – | – | Zone (5 s), Gegner rutscht |
| Kanalisation | ←↙↓↘→ + △ | 20 | 4 | 20 | 12 % | Grab |
| Wasserrohr | →→ + △ | 10 | 6 | 18 | 18 % | Anti-Air |

## 5. Uschi — „Die gute Seele vom Kiez"

| Move | Eingabe | Startup | Active | Recovery | Schaden | Effekt |
|------|---------|--------|--------|----------|---------|--------|
| Handtasche | ↓↘→ + □ | 14 | 4 | 22 | 12 % | Projektil, 3 Charges |
| Gurke | ↓↓ + △ | 35 | – | – | +10 % HP | Heilen, 12 s CD |
| Kochlöffel | ←↙↓ + □ | 10 | 4 | 16 | 9 % | +0,3 s Stun |
| Topfdeckel | →↘↓↙← + △ | 25 | – | – | – | Block-Buff (+50 %) für 8 s |
| Suppe | ↓↘→↓↘→ + △ | 40 | – | – | +15 % HP | Heilen, 20 s CD |

## 6. TetraPak — „Der Schnapsdrossel"

| Move | Eingabe | Startup | Active | Recovery | Schaden | Effekt |
|------|---------|--------|--------|----------|---------|--------|
| Fusel-Atem | ↓↘→ + □ | 18 | 6 | 24 | 6 % + 2 %/s DoT | 5 s DoT |
| Leergut | ←↙↓ + △ | 20 | 4 | 20 | 10 % | Zone, Objekte umkippen |
| Pfandflasche | →→ + □ | 14 | 4 | 18 | 15 % | Projektil |
| Kater | ↓↓ + △ | 25 | – | – | – | Gegner langsamer für 4 s |
| Trinken | (Taste) | 20 | – | – | +3 % HP | Erhöht Betrunkenheit, +Schaden |
| Zweiter Wind | passiv | – | – | – | +20 % HP | Bei HP ≤ 20 %, +20 % Schaden |

## 7. Sigi — „Der Hacker"

| Move | Eingabe | Startup | Active | Recovery | Schaden | Effekt |
|------|---------|--------|--------|----------|---------|--------|
| Laptop | ↓↘→ + □ | 16 | 4 | 20 | 8 % | Projektil |
| Root-Zugriff | ←↙↓↘→ + △ | 28 | – | – | – | Hack: Gegner 6 s geschwächt |
| Firewall | ↓↓ + △ | 20 | – | – | – | Block-Buff für 5 s |
| SQL-Injection | →↘↓↙← + □ | 12 | 4 | 16 | 12 % | Eingaben invertiert (2 s) |
| DDoS | →→ + △ | 30 | – | – | – | +4 Frames Eingabeverzögerung |

## 8. Rolf — „Der Rattenkönig"

| Move | Eingabe | Startup | Active | Recovery | Schaden | Effekt |
|------|---------|--------|--------|----------|---------|--------|
| Ratten | ↓↘→ + □ | 22 | – | – | – | 5 Ratten, 10 s CD |
| Rattenschwanz | ←↙↓ + △ | 15 | 4 | 18 | 13 % | Peitsche, große Reichweite |
| Rattenkönig | →↘↓↙← + △ | 30 | – | – | 20 % | Beschwörung, 25 s CD |
| Ratengift | ↓↓ + △ | 20 | – | – | 3 %/s DoT | Zone, 6 s Dauer |
| Kanalisation | →→ + △ | 20 | 4 | 20 | 12 % | Grab |

## 9. Kalle — „Der Klempner"

| Move | Eingabe | Startup | Active | Recovery | Schaden | Effekt |
|------|---------|--------|--------|----------|---------|--------|
| Arbeitshandschuh | ↓↘→ + □ | 14 | 4 | 20 | 12 % | Grab (zieht Gegner) |
| Feuerzeuggas | ←↙↓ + △ | 18 | 6 | 22 | 4 %/s DoT | 4 s DoT |
| Rohrzange | →↘↓↙← + △ | 10 | 4 | 18 | 16 % | Anti-Air |
| Ventil | ↓↓ + △ | 25 | – | – | – | Buff +10 % für 8 s |
| Klempner | →→ + △ | 30 | – | – | +10 % HP | Heilen, 10 s CD |

---

Implementierung: `MoveCatalog` (Frame-Daten) + `CommandInput` (Eingabe-Erkennung)
+ `ExecuteMove` in jedem Charakter-Skript.
