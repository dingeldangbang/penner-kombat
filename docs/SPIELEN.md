# Penner Kombat spielen

> Diese Seite beschrieb früher den Unity-Weg (Unity Hub, `Tools → Penner
> Kombat`, `Assets/Scenes/Arena.unity`). Das ausgelieferte Spiel ist ein
> **Godot-4-Projekt**; die Unity-Reste unter `Assets/` sind nicht mehr der
> Bauweg. Der Text ist deshalb vollständig ersetzt.

## Was vorher fehlte

Das Kampfsystem war komplett — Zustandsautomat, Combos, KI mit fünf
Schwierigkeitsstufen, Runden, Trefferzonen, HUD, Effekte. Nur führte kein Weg
hinein:

Die App startete im **Editor**. Der verlangte als Erstes eine GLB-Datei. Ohne
geladenes Modell blieb der „PLAY"-Knopf bei *„Bitte zuerst Asset laden"*
stehen — und auf einem frisch installierten Handy gibt es keine GLB-Datei.
Die Arena war damit unerreichbar.

Selbst wer hineinkam, saß fest: Die Arena verließ man nur mit `ESC`, das
Match-Ende zeigte nur einen Schriftzug ohne Knopf, und Blocken und Springen
gab es zwar im Code, aber ohne Bedienelement.

## Was jetzt da ist

**Startbildschirm** (`res://scenes/ui/MainMenu.tscn` ist die neue Hauptszene):

| Menüpunkt | Wirkung |
|---|---|
| Kampf starten | sofort ein Match gegen einen zufälligen Gegner |
| Arcade-Modus | Leiter gegen alle Kämpfer, Boss zum Schluss, Fortschritt wird gespeichert |
| Kämpfer wählen | Auswahl mit rotierender 3D-Vorschau |
| Training | Gegner steht still, zum Combos-Üben |
| Einstellungen | Schwierigkeit, Runden, Rundenzeit, Touch-Größe, Sound |
| Figur bauen | der bisherige Editor — jetzt optional statt Pflicht |

**Sechs eingebaute Kämpfer** mit eigenen Werten und Combos:

| Kämpfer | Tempo | Kraft | Leben | Eigenart |
|---|---|---|---|---|
| Kalle Kanister | 3,2 | 21 | 130 | schwer und langsam |
| Schnelle Sonja | 6,4 | 10,5 | 82 | drei schnelle Schläge |
| Doktor Dosenbier | 4,5 | 15 | 100 | ausgewogen |
| Ratten-Rudi | 5,2 | 23 | 70 | Glaskinn, Vorschlaghammer |
| Oma Olga | 3,8 | 17,5 | 112 | Handtasche mit Kleingeld |
| Der Grubenkönig | 3,6 | 24 | 150 | Boss |

Die Figuren bestehen aus Godot-Primitiven, brauchen also **keine Asset-Dateien**
und funktionieren direkt nach der Installation. Die Körperteile heißen bewusst
`Head`, `Hand_R`, `Hand_L` und `Hips` — genau diese Namen erkennt der
`DynamicRigger` und hängt Trefferzonen und Effekte daran auf.

**Steuerung auf dem Handy**

- Joystick unten links: laufen
- ATTACK: nächster Combo-Schlag
- BLOCK: halten, reduziert Schaden auf 20 %
- SPRUNG
- „II" oben rechts: Pause → Weiter / Neustart / Hauptmenü
- Zurück-Taste: Pause statt App-Ende

Tastatur (Desktop): Pfeiltasten, `Enter` Angriff, `ESC` Pause.

**Nach dem Match** erscheint immer ein Menü: Revanche oder Hauptmenü, im
Arcade-Modus „Nächster Gegner". Vorher blieb der Bildschirm stehen.

**Der Spielstand** (`user://savegame.cfg`) merkt sich gewählten Kämpfer,
Arcade-Fortschritt, Optionen und Statistik.

## Prüfen ohne Godot

Godot lässt sich in der Build-Umgebung nicht ausführen, deshalb prüfen vier
Skripte die Teile, die sich statisch prüfen lassen. Alle laufen auch im
GitHub-Workflow vor dem Export:

```bash
bash Tools/check_all.sh
```

| Skript | Prüft |
|---|---|
| `godot_preflight.py` | `res://`-Referenzen, `@onready`-Pfade, Autoloads, Export-Filter, Icons, SDK-Werte |
| `gdscript_lint.py` | Einrückung, Klammern, überschriebene Basisklassen-Methoden, nicht registrierte Autoloads |
| `check_roster.py` | ob jeder Combo an einem echten Körperteil hängt statt still auf `Root` zurückzufallen |
| `simulate_match.py` | Rundenlogik, Arcade-Leiter, Options-Knöpfe |

Diese Prüfungen haben beim Bauen drei echte Fehler gefunden: eine Arcade-Leiter,
in der der Boss gegen sich selbst antrat; eine fest verdrahtete „2 Runden"-Regel,
die die Einstellung ignorierte; und einen Options-Knopf, der seinen Wert nie
änderte.

Was sie **nicht** ersetzen: einen echten Testlauf. Godot-Laufzeitfehler,
Bildrate und Bedienbarkeit auf einem realen Gerät sieht man erst im APK.
