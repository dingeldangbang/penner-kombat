# 🎮 SPIELEN — vom Repo zum laufenden Kampf

Diese Seite beantwortet genau eine Frage: **Was muss ich tun, um das Spiel zu spielen?**
Nicht „was fehlt für ein fertiges Produkt" — das steht in [`STATUS.md`](STATUS.md).

---

## 1. Kurzfassung (5 Minuten, ohne einen einzigen Klick in der Hierarchie)

1. **Unity Hub → Add → Projekt-Ordner auswählen** (dieses Repo).
   Es liegen jetzt `Packages/manifest.json` und `ProjectSettings/` bei, das Repo öffnet also
   direkt als Unity-Projekt (Zielversion **2022.3 LTS**, neuere Versionen migrieren automatisch).
2. Unity lädt die Pakete (URP, Input System, TextMeshPro). Beim ersten Start richtet sich das
   Projekt selbst ein (Tags, Layer, `PK_URP`, TMP-Grundressourcen) — Skript `PkProjectSetup`.
   Fragt Unity nach *Active Input Handling* / verlangt einen Neustart: **zulassen und neu starten**.
3. Menü **`Tools → Penner Kombat → ▶ Alles einrichten und spielen`**.
   Das erzeugt Szene, Boden, Wände, Licht, FighterDatabase, 9 Platzhalter-Prefabs und drückt Play.
4. **Kämpfen.** Spieler 1 = Tastatur, Gegner = KI.

Wenn du lieber selbst Play drückst: `Tools → Penner Kombat → Alles einrichten (ohne Play)`,
danach `Assets/Scenes/Arena.unity` öffnen und ▶ drücken.

---

## 2. Steuerung im MVP

| Aktion | Spieler 1 | Spieler 2 |
|---|---|---|
| Bewegen | W A S D | Pfeiltasten |
| Leichter Angriff | J | Numpad 1 |
| Schwerer Angriff | K | Numpad 2 |
| Blocken | Linke Shift | Numpad 3 |
| Springen | Leertaste | Numpad 0 |
| Spezial 1 / 2 | U / I | Numpad 4 / 5 |
| Ausweichrolle | O | Numpad 6 |
| Interagieren (Waffe) | E | Numpad . |
| Fatal Blow | Y | Numpad + |
| Med-Kapsel | H | Numpad Enter |
| Frame-Daten-Overlay | **F4** | — |
| Modell-Menü (GLB) | **F7** | — |
| Touch-Debug | F3 | — |

Vollständige Liste inklusive Umbelegung: [`CONTROLS.md`](CONTROLS.md).

---

## 3. Was das Spiel sich selbst baut (keine Assets nötig)

| Element | Wer erzeugt es | Datei |
|---|---|---|
| Kämpfer (Kapsel + Kopf + Arme + Blickrichtung) | `FighterFactory.CreatePlaceholder` | `Assets/Scripts/Core/FighterFactory.cs` |
| HUD (HP, Fatal Blow, Timer, Runde, Combo, Ergebnis, Revanche) | `HudBuilder.Ensure` | `Assets/Scripts/UI/HudBuilder.cs` |
| Boden 30×30 m + 4 Wände | `Bootstrapper.EnsureGround` | `Assets/Scripts/Core/Bootstrapper.cs` |
| Kamera, Arena, Audio, VFX, Combo, Musik-Sync, Crowd, Power-Ups | `Bootstrapper.Awake` | dito |
| FighterDatabase mit 9 Charakteren | `FighterDatabase.EnsureDefaultRoster` | `Assets/Scripts/Data/FighterDatabase.cs` |
| Tags, Layer, `PK_URP`, TMP-Ressourcen | `PkProjectSetup` | `Assets/Scripts/Editor/PkProjectSetup.cs` |
| Szene, Prefabs, Materialien | `PkQuickStart` | `Assets/Scripts/Editor/PkQuickStart.cs` |

**Heißt konkret:** Selbst wenn du eine leere Szene nimmst, ein leeres GameObject anlegst und nur
`Bootstrapper` draufziehst, startet ein vollständiger Kampf inklusive HUD.

---

## 4. Der MVP-Check

| # | Komponente | Status | Wo |
|---|---|---|---|
| 1 | Unity-Projekt (Packages + ProjectSettings) | ✅ liegt im Repo | `Packages/`, `ProjectSettings/` |
| 2 | 3D-Charaktere (Platzhalter) | ✅ prozedural | `FighterFactory` |
| 3 | 3D-Arena (Boden + Wände) | ✅ prozedural | `Bootstrapper`, `GameSetupWizard` |
| 4 | Bewegung / Sprung / Rolle | ✅ | `FighterController` |
| 5 | Angriff / Hitboxen | ✅ | `FighterController`, `Hitbox3D` |
| 6 | Blocken / Konter | ✅ | `FighterController`, `ParrySystem` |
| 7 | KI (6 Stufen) | ✅ | `AIController` |
| 8 | HUD | ✅ prozedural | `HudBuilder` |
| 9 | Kamera (4 Modi) | ✅ | `CameraController` |
| 10 | Steuerung Tastatur/Gamepad/Touch | ✅ | `FighterInput`, `TouchControls` |
| 11 | Runden, Timer, Best-of-3 | ✅ | `GameManager` |
| 12 | Frame-Daten / Hitbox-Anzeige | ✅ (F4) | `FrameDataOverlay` |
| 13 | Charaktermodelle, Animationen, Audio | ❌ fehlen weiterhin | siehe `STATUS.md` |

---

## 5. Wenn etwas nicht läuft

| Symptom | Ursache | Lösung |
|---|---|---|
| Tastatur reagiert nicht | *Active Input Handling* steht auf „Input Manager (Old)" | `Edit → Project Settings → Player → Active Input Handling = Both`, Unity neu starten |
| HUD ist leer / TMP-Fehler in der Konsole | TMP-Grundressourcen fehlen | `Window → TextMeshPro → Import TMP Essential Resources` |
| Kämpfer fallen ins Nichts | Kein Boden in der Szene | Bootstrapper auf ein GameObject ziehen oder Setup-Menü nutzen |
| Angriffe treffen nicht | `enemyLayer` war leer | wird seit dieser Version automatisch gesetzt (`FighterFactory.DefaultEnemyMask`) |
| Alles pink | URP-Material fehlt / falsche Pipeline | `Edit → Project Settings → Graphics → URP-Asset` zuweisen |
| Menü „Tools → Penner Kombat" fehlt | Kompilierfehler in der Konsole | Fehler zuerst beheben, Editor-Skripte laden sonst nicht |

---

## 6. Danach: echte Modelle einsetzen

**Schnellweg GLB:** `.glb`-Dateien in `Assets/Models/Fighters` legen und
`Tools → Penner Kombat → GLB → Modelle zu Kämpfer-Prefabs machen` — oder im laufenden Spiel
mit **F7** zuweisen. Details: **[MODELLE.md](MODELLE.md)**.

Der Platzhalter greift **nur**, wenn `FighterConfig.prefab` leer ist oder auf ein `PK_*`-Prefab zeigt.
Sobald du ein echtes Modell hast:

1. Modell mit **Humanoid-Rig** importieren.
2. Prefab bauen: `Rigidbody` + `CapsuleCollider` + Charakter-Skript (`LeBinde`, `Mell`, …)
   + Child `AttackPoint` + `Animator`.
3. Animator-Parameter: `Walk`, `Block`, `Jump`, `LightAttack`, `HeavyAttack`, `HitReact`,
   `Death`, `Roll`, `Special` — `AnimationsController3D` setzt nur, was tatsächlich existiert.
4. Prefab in `Assets/Resources/FighterDatabase.asset` beim passenden Charakter eintragen.

Logik, Hitboxen, Kamera und Netzwerk bleiben unangetastet — es wird nur der Renderer getauscht.
