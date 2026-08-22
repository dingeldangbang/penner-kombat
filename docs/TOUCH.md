# 📱 TOUCH-STEUERUNG

Vollständige On-Screen-Bedienung für Android/iOS — **prozedural aufgebaut**,
kein Prefab und kein Sprite nötig. Alle Teile liegen in
`Assets/Scripts/UI/Touch/` (plus `Core/VirtualInput.cs` und `UI/TouchControls.cs`).

---

## Abgearbeitete Liste

| # | Komponente | Datei | Status |
|---|---|---|---|
| 1 | Virtueller Joystick | `Touch/VirtualJoystick.cs` | ✅ fest / dynamisch / Snap, Totzone, Empfindlichkeit, Events |
| 2 | Aktions-Buttons | `Touch/TouchButton.cs` | ✅ Tap / Halten / Toggle, Vibration, Farbfeedback |
| 3 | Zentrale Steuerung | `Touch/TouchInputManager.cs` | ✅ bündelt alles, EX-Combo (Block + Spezial), Sichtbarkeit |
| 4 | Gesten-Erkennung | `Touch/TouchGestureDetector.cs` | ✅ 8 Richtungen, 8 Patterns, Sequenz-Wiedergabe |
| 5 | Layout-Editor | `Touch/TouchLayoutEditor.cs` | ✅ Drag & Drop, Größe/Deckkraft, 4 Vorlagen, Speichern |
| 6 | Anti-Ghosting | `Touch/AntiGhosting.cs` | ✅ ein Finger = ein Element |
| 7 | Eingabepuffer | `Touch/InputBuffer.cs` | ✅ 0,15 s Fenster, `Has` / `Consume` |
| 8 | Tutorial | `Touch/TouchTutorial.cs` | ✅ 7 Schritte, baut eigenes Panel, „einmalig" |
| 9 | Einstellungen | `Touch/TouchSettings.cs` + `UI/OptionsMenu.cs` | ✅ JSON in PlayerPrefs, live angewendet |
| 10 | Debug-Overlay | `Touch/TouchDebug.cs` | ✅ F3, Finger/Achse/Numpad/Buttons/Puffer |

---

## Belegung (Gamepad → Touch)

| Gamepad | Touch | Funktion |
|---|---|---|
| Linker Stick | Joystick links | Bewegung |
| □ | `□` | Leichter Angriff |
| △ | `△` | Schwerer Angriff |
| ○ | `○` | Spezial 1 |
| ✕ | `✕` | Sprung |
| L1 | `BLOCK` (halten) | Blocken (78 % Reduktion) |
| R1 | `S2` | Spezial 2 |
| L2+R2 | `X-RAY` | Fatal Blow |
| D-Pad | Wischgesten | Spezialbewegungen |

**Layouts:** `Standard` · `Fighting` (enger Diamant) · `Simple` (nur 4 Tasten) ·
`LeftHanded` (gespiegelt). Umschaltbar in den Optionen oder im Layout-Editor.

---

## Gesten → echte Kommandoeingaben

Der entscheidende Punkt: Gesten erzeugen **keine** zweite Moveset-Logik.
`TouchGestureDetector` erkennt die Wischfolge, übersetzt sie in die
Numpad-Sequenz und spielt sie über `VirtualInput` Frame für Frame nach —
der vorhandene `CommandInput` wertet sie dann ganz normal gegen den
`MoveCatalog` aus. Ein Move, eine Wahrheit.

| Wischfolge | Numpad | typischer Move |
|---|---|---|
| ↓ ↘ → | 2 3 6 | Flaschenhals, Pampe, Riesenschwanz |
| ↓ ↙ ← | 2 1 4 | gespiegelte Version |
| → ↘ ↓ ↙ ← | 6 3 2 1 4 | Halbkreis-Moves |
| ← ↙ ↓ ↘ → | 4 1 2 3 6 | Halbkreis vorwärts |
| → → / ← ← | 6 5 6 / 4 5 4 | Dash / Doppelschicht |
| ↓ ↓ | 2 5 2 | REIF!, Kater, Ventil |
| ↑ ↑ | 8 5 8 | Sprung-Spezials |

Erkannte Geste wird als Text eingeblendet (`gestureFeedback` in den Optionen).

---

## Eingabekette

```
Finger
  → VirtualJoystick / TouchButton      (UI, prozedural)
      → AntiGhosting                   (ein Finger = ein Element)
      → VirtualInput                   (Achse + Tasten, wasPressed/isHeld)
      → InputBuffer                    (0,15 s Nachsicht)
  → TouchGestureDetector               (Wischfolge → Numpad-Sequenz)
      → VirtualInput (Replay)
  → FighterInput                       (verodert mit Tastatur + Gamepad)
      → FighterController / CommandInput / AIController
```

---

## Einstellungen (Optionen → Touch)

| Regler | Bereich | Wirkung |
|---|---|---|
| Joystick-Größe | 0,6–1,8× | Radius und Grafik |
| Button-Größe | 0,6–1,8× | alle Aktionsbuttons |
| Deckkraft | 0,15–1,0 | Sichtbarkeit über dem Kampf |
| Empfindlichkeit | 0,25–2,0 | Achsenverstärkung |
| Vibration | an/aus | haptisches Feedback pro Tastendruck |
| Gesten-Feedback | an/aus | Einblendung der erkannten Wischfolge |
| Linkshänder | an/aus | spiegelt das komplette Layout |
| Layout | 4 Vorlagen | siehe oben |
| Tutorial | Button | zeigt die 7 Schritte erneut |

Gespeichert wird als JSON unter dem PlayerPrefs-Key `pk_touch_settings`;
Positionen aus dem Layout-Editor kommen als `Element|x|y`-Liste dazu.

---

## Aktivierung

Der `Bootstrapper` erzeugt die Steuerung automatisch auf Handhelds.
Zum Testen am PC: am `Bootstrapper` **`touchControlsOnDesktop = true`** setzen.

```csharp
// manuell, z. B. aus einem eigenen Menü:
TouchControls.Ensure(force: true);
TouchSettings.Current.layout = "Fighting";
TouchSettings.Current.Save();      // wendet sofort an
```

---

## Bekannte Grenzen

- **Nur Spieler 1.** Zwei Touch-Spieler auf einem Gerät sind nicht vorgesehen;
  `VirtualInput` hat zwar zwei Slots, es wird aber nur ein Layout gebaut.
- **Vibration** nutzt `Handheld.Vibrate()` — das ist ein kurzer Standardimpuls,
  die eingestellte Dauer wird von Android ignoriert. Für feine Haptik bräuchte
  es ein Plugin.
- **Kein Editor-Multitouch:** Im Unity-Editor lässt sich mit der Maus immer nur
  ein Element gleichzeitig bedienen — Block + Angriff also erst auf dem Gerät testbar.
- Getestet ist bisher **nichts am Gerät**: Der Code ist nicht kompiliert worden
  (kein Unity in der Build-Umgebung), siehe `docs/STATUS.md`.
