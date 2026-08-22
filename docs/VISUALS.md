# 🎮 VISUALS — Gameplay-Look, Effekte & Combo-Feedback

**Referenz: das Cover-Artwork** (Hinterhof „Zum Blauen Eimer", Nacht, warmes
Laternenlicht, Neonschild, Bierkästen, Wäscheleine, dreckig-humorvoller Comic-Look).
Dieses Dokument ist **kein reines Konzept** — jeder Abschnitt nennt die Klasse,
die ihn umsetzt (`Assets/Scripts/VFX/…`).

> Alle Effekte werden **zur Laufzeit prozedural erzeugt** (Partikelsysteme,
> Lichter, Texturen, Sounds). Es sind **keine Art-Assets nötig**, damit der Look
> ohne fertiges Unity-Projekt greift. Eigene Prefabs/Clips lassen sich überall
> als Override zuweisen und haben dann Vorrang.

---

## 1. Farbpalette → `PennerPalette.cs`

| Element | Farbe | Hex | Verwendung |
|---|---|---|---|
| Hauptakzent | Blutrot | `#8B0000` | Titel, Lebensbalken, Blut |
| Hintergrund | Düsteres Blaugrau | `#1A1C2A` | Hinterhof, Nachtszene, Fog |
| Warmlicht | Orange | `#FF6B00` | Laternen, Treffer-Flackern |
| Kontrast | Gold | `#FFD700` | Mojo Bob, Krit, Sieg |
| Kühl | Neonblau | `#00BFFF` | Mell, Sigi, Elektro, Neonschild |
| Erde | Braun | `#8B4513` | Holz, Bierkästen, Staub |
| Gift | Grün | `#228B22` | TetraPak, Rattengift, Glassplitter |
| Weiß | Reinweiß | `#FFFFFF` | Text, UI, Highlights |

Signaturfarbe pro Kämpfer: `PennerPalette.ForCharacter(fighterId)` ·
Combo-Farbe: `PennerPalette.ForCombo(combo)` (1–4 weiß · 5–9 gold · 10+ rot).

---

## 2. Charakter-Visuals → `CharacterVisuals.cs`

Wird in `FighterController.Awake()` automatisch an jeden Kämpfer gehängt:
Signatur-Aura (Point Light), Bewegungs-Trail und ein Ambient-Partikelsystem.

| Charakter | Look | Zustandslogik |
|---|---|---|
| **Le Binde** — „Der Schlachter" | Blutrot/Braun, öliger Fettfilm-Schimmer, träge Aura mit langsamem Puls | Schmier-Schlüppa-Ladungen; `BuffFlash` (roter Glow, 6 s) bei **REIF!** |
| **Mell** — „Schneeweißchen" | Neonblau → Orange → Blutrot je nach Puls, Speedlines ab 160 bpm | `Mell.Pulse` (60–220) steuert Aura-Farbe, Herzschlag-Frequenz, Trail-Länge und den roten Bildschirmrand |
| **Mojo Bob** — „Der Glücksbringer" | Goldener Glückspartikel-Staub, Emissionsrate = Mojo-Punkte/7 | Krit → Goldblitz + Frame-Freeze; **Dingeneldang!** → Aura-Burst + Goldblitz |
| Dieter/Uschi/TetraPak/Sigi/Rolf/Kalle | Signaturfarbe, Aura-Intensität = Fatal-Blow-Ladung | Aura funkelt, sobald der X-Ray bereit ist |

---

## 3. Arena „Zum Blauen Eimer" → `ArenaVisuals.cs`

- **Key-Light:** oranges Laternen-Spotlight (Intensität 4,2 · 70°) über der Kampfzone, weiche Schatten
- **Fill-Light:** zweite Laterne, gold-orange
- **Ambient:** bläuliches Mondlicht (Directional) + Trilight-Ambient in Nachtblau
- **Fog:** `ExponentialSquared`, `#1A1C2A`, Dichte 0,018 — Tiefe im Hinterhof
- **Neonschild:** Point Light in Neonblau, Grundflackern 1–2 Hz + zufällige Aussetzer (`NeonGlitch`, alle 2,5–7 s)
- **Partikel:** Mücken um die Laterne · Staub in der Kampfzone · Rauch von der Gasflasche (alle mit Noise-Turbulenz)
- **Boden:** Setup-Wizard färbt ihn nachtblau und setzt Smoothness 0,65 → nasses Pflaster spiegelt die Laternen
- `WallImpact(pos)` → Staubwolke + 6 px Shake · `TrashTheYard()` → alle Props kippen + 0,1× Zeitlupe (Mops-Kommando)

---

## 4. Treffer- und Combo-Effekte

### 4.1 Trefferklassen → `HitTier` + `VFXManager.PlayHit()`

| Tier | Partikel | Shake | Extra |
|---|---|---|---|
| `Light` (□) | 5 weiß/orange Funken + 3 Blut | — | — |
| `Heavy` (△) | 12 goldene Funken + 6 Blut | 2 px / 0,05 s | — |
| `Special` (○) | 18 Funken in Charakterfarbe | 5 px / 0,10 s | — |
| `Ex` (R1+○) | 40 Funken + Schockwellenring | 8 px / 0,12 s | Hitstop 0,05 s |
| `Critical` (⭐) | 25 goldene Sterne | 6 px / 0,10 s | **2 Frames Freeze**, Goldblitz, „DINGENELDANG!" |
| `FatalBlow` | 50 Funken + Ring + 20 Blut | 15 px / 0,30 s | Zeitlupe 0,5× (0,8 s), Weißblitz |
| `Fatality` | Blutfontäne (14 Stöße) | 20 px / 0,50 s | Zeitlupe 0,3×, **Blut auf der Linse** |

Blockierte Treffer: weißer Abprall-Blitz in Neonblau, kein Blut (`PlayBlock`).

Aufruf aus dem Kampfsystem: `FighterController.PlayHitFeedback(target, damage, tier)` —
Basisangriffe rufen es automatisch, Spezials/EX/Krits mit passendem Tier.

### 4.2 Combo-Zähler → `ComboCounterUI.cs`

Weltraum-verankertes Label über dem Kämpfer:

| Combo | Farbe | Größe | Animation |
|---|---|---|---|
| 2–4 | Weiß | 100 % | Pop beim Treffer |
| 5–9 | Gold | 120 % | Pulsieren (10 Hz) |
| 10+ | Rot | 140 % | Flackern (Perlin) + Outline in Blutrot, Untertitel „KOMBO!" |

### 4.3 Eskalation → `ComboSystem.cs`

| Stufe | Shake | Zoom | Vignette | Sound | Partikel |
|---|---|---|---|---|---|
| 1–4 | — | 1,00× | 0,30 | Ping bei 3 | — |
| 5–7 | 2 px | 1,05× | 0,37 | „Pling!" bei 5 | Rotblitz 5 % |
| 8–10 | 5 px | 1,05–1,10× | 0,43 | „DING!" bei 8 | Rotblitz 10 % |
| 11–15 | 8 px | 1,10× | 0,60 | „KOMBO!" | Staubwolke |
| 16–20 | 12 px | 1,15× | 0,70 | „KOMBO!" | EX-Impact-Burst |
| 21+ | 12 px | 1,25× | 0,80 | „KOMBO!" | **Feuerwerk** (5 Bursts in Regenbogenfarben) |

Mit aktivem `PK_URP` fährt `UrpPostProcessingDriver.SetCombo()` zusätzlich die
komplette Profilzeile (Bloom 0,5→1,2 · CA 0,1→0,5 · Grain 0,05→0,10 ·
Kontrast +15→+55 % · Sättigung +5→+45 %) und legt den DoF-Fokus auf den
Combo-Kämpfer.

Fehlen Audio-Clips, erzeugt `ComboSystem.ProceduralPing` den Ton zur Laufzeit
(Sinus mit Hüllkurve, Tonhöhe 660 → 1320 Hz).

### 4.4 Charakterspezifisch (bereits verdrahtet)

- **Le Binde:** Flaschenhals → Glassplitter · Großer Schwung → EX-Impact + Wallbounce-Staub ·
  Mops-Kommando → 0,1× Zeitlupe, Props kippen, „FASSUNGSLOSIGKEIT"-Banner ·
  REIF! → roter Glow 6 s + Callout
- **Mell:** Defi → blaue Elektro-Arcs + Überbelichtung + Special-Impact ·
  Blackout (Puls 220) → 3 s Schwarzbild mit blau/weißem Flackern ·
  Puls ≥ 100 → roter, im Herzschlag pulsierender Bildschirmrand
- **Mojo Bob:** jeder Krit → Goldsterne, Frame-Freeze, „DINGENELDANG!" ·
  Super-Mode → Aura-Burst + Goldblitz · Lockout (15 s) → grauer Bildschirm-Tint

---

## 4.5 Signatur-Combos aller 9 Charaktere → `SignatureFx.cs`

Jede Combo aus der Spec ist eine aufrufbare Inszenierung — die Charakterklassen
rufen nur noch eine Zeile, Partikel/Kamera/Screen/Callout stecken in `SignatureFx`.

| Charakter | Aufruf | Was passiert |
|---|---|---|
| Le Binde | `LeBinde_Grease(f, charges)` | Fettspritzer, Boden-Schmierfilm 8 s, goldener Aura-Burst |
| | `LeBinde_Slip(def, atk)` | weiße Schleifspur + „ABGERUTSCHT" |
| | `LeBinde_Flaschenhals(f, t, ex)` | Glassplitter (EX: 3 Bursts + Shake), Blutungs-DoT 5 s |
| | `LeBinde_Reif(f)` | Cinematic-Zoom aufs Gesicht, Rotblitz, 6 s Funken-Buff, „…keuch" |
| | `LeBinde_ReifImpact(f, t, dmg)` | Feuerball + EX-Impact + Schadenszahl |
| | `LeBinde_Pfanne(f, t)` | Feuerball, Burn-DoT 3 s, „FETT VERBRANNT" |
| Mell | `Mell_Doppelschicht(f, t, step)` | Schritt 1 weiß, Schritt 2 rot + „UNBLOCKBAR" |
| | `Mell_Defi(f, t)` | 10 blaue Arcs, Weißblitz, „BUFF GEPURGT" |
| | `Mell_SechzehnStunden(f, t)` | Regenbogen-Sweep, 11 Nachbilder im Kreis, 11 farbige Treffer, goldenes Finale + Feuerwerk |
| Mojo Bob | `MojoBob_Gamble(f, p, chance)` | goldener Würfel dreht sich 0,8 s, dann Krit-Prozent |
| | `MojoBob_GambleLost(f)` | Rotblitz, „MOJO WEG", Shake |
| | `MojoBob_Loeffelsturm(f, crits)` | 17 Flugbahnen im Fächer, Goldblitz je Krit |
| | `MojoBob_Dingeneldang(f, 8 s)` | Goldsäule, `ScreenState.Dingeneldang`, Krit-Puls alle 0,25 s, danach 15 s `MojoLockout` |
| Dieter | `Dieter_Kanal`, `Dieter_Abflussreiniger` | Schlammfontäne / grüne Giftwolke + Buff-Glow |
| Uschi | `Uschi_Heal`, `Uschi_Topfdeckel` | grüne Heilpartikel + HP-Zahl / blauer Schild-Flash |
| TetraPak | `TetraPak_FuselAtem`, `_Trinken`, `_ZweiterWind` | Feuerstoß + `ScreenState.Fusel`, Trink-Unschärfe, weißer Glow |
| Sigi | `Sigi_Hack(f, t, label)` | Arcs am Ziel, Label, `ScreenState.Matrix` |
| Rolf | `Rolf_Ratten`, `Rolf_Gift` | Schwarm-Staub + `ScreenState.RatSwarm`, Giftwolke |
| Kalle | `Kalle_Zug`, `Kalle_Reparatur` | Zug-Impact „KOMM HER", blauer Reparatur-Glow |

## 4.6 Mops-Kommando — 180-Frame-Sequenz → `MopsKommandoSequence.cs`

Exakt nach Spec §3.2 getaktet (unskalierte Zeit, 1 Frame = 1/60 s):

| Frames | Umsetzung |
|---|---|
| 0–20 | Zeitlupe 0,1×, Kamera auf Paula (`CameraController.SetCinematic`), Staub |
| 20–40 | Sprungbogen aus der Tasche, Skalierung 0,2 → 0,5, Landestaub |
| 40–60 | Weg zum Gegner, alle 0,08 s ein Pfotenabdruck (verblasst nach 2 s) |
| 60–80 | Auge-in-Auge: Kamera auf den Gegner, Gegner-Stun |
| 80–100 | Häufchen mit Dampfpartikeln, Callout „…plumps" |
| 100–120 | Debuff-Banner „FASSUNGSLOSIGKEIT" |
| 120–150 | Zeitlupe 0,3×, alle `ArenaProp`s kippen (Gasflasche explodiert), Shake 9 px |
| 150–180 | 6 Staubwolken, Neon-Panik-Flackern, zurück zur Kampfkamera |

## 4.7 Arena-Props → `ArenaProp.cs` / `ArenaHazard.cs`

| Prop | Verhalten laut Spec |
|---|---|
| Bierkasten-Turm | kippt, 14 % Schaden im Umkreis, hinterlässt **Glassplitter-Hazard** (2 % + 0,2 s Stolper-Stun, 12 s) |
| Gasflasche | 4 Treffer → Explosion 25 % / 5 m, Feuerball, Brand-Hazard (4 %, 6 s) |
| Wäscheleine | reißt, Unterhosen fallen (Rigidbody), 0,3 s Stun als Combo-Extender |
| Mülltonne | kippt und rollt 1,2 s, 7 % Schaden im Rollpfad |
| Paula-Napf | kosmetisch, „unheilvoller Ton" als Callout |
| Baugerüst | kippbar, markiert die Stage-Fatality-Zone |
| Neonschild | Grundflackern 1–2 Hz, `PanicNeon()` bei Chaos |
| Pflastersteine | nasser Boden mit Reflexion (Setup-Wizard) |

`ArenaVisuals.BuildYard()` stellt das komplette Set als Platzhalter-Geometrie auf —
echte Meshes ersetzen später nur die Primitives, die `ArenaProp`-Logik bleibt.

## 5. Partikelsysteme → `VFXManager.cs`

| System | Renderer | Farbe | Größe | Lebensdauer | Speed | Menge |
|---|---|---|---|---|---|---|
| `HitSpark` | Billboard, additiv | Weiß → Orange/Rot | 0,05–0,20 | 0,1–0,4 s | 2–8 m/s | 5–50 |
| `BloodSplat` | Billboard, alpha | `#8B0000`, Gravity 1,4 | 0,1–0,5 (schadensabhängig) | 0,5–2,0 s | 1–5 m/s | 3–20 |
| `Shockwave` | Quad-Ring, additiv | Charakterfarbe → transparent | 0,5 m → 3,0 m | 0,15 s | — | 1 |
| `GoldKrit` | Billboard, additiv | Gold → Weiß | 0,2–0,8 | 0,3–0,8 s | 5–15 m/s | 25 |
| `Dust` | Billboard, alpha | Braun/Nachtblau | 0,3–1,2 | 0,6–1,6 s | 0,5–2,5 m/s | 12+ |
| `Sludge` | Billboard, alpha | `#2F4F4F` → `#1A1A1A`, Gravity 0,9 | 0,3–0,6 | 2,0 s | 0,2–0,8 m/s | 12–26 |
| `PulseGlow` | Billboard, additiv | `#FF0000` → `#FF4444` | 0,1–0,5 | 0,2–0,6 s | 0–2 m/s | 3–14 |
| `Grease` | Billboard, alpha | `#FFD700` → `#8B8B00`, tropfend | 0,1–0,4 | 0,5–1,5 s | 0,5–2 m/s | 8+ |
| `Fire` | Billboard, additiv | Gold → Orange → Rot, Auftrieb | 0,2–0,7 | 0,3–0,9 s | 2–6 m/s | 20–40 |
| `Poison` | Billboard, alpha | Giftgrün, Noise-Turbulenz | 0,4–1,1 | 1,2–2,4 s | 0,1–0,7 m/s | 24 |

Dazu: `PlayFireworks(pos, bursts)` für Combo 21+ und `PlayRainbowHit(pos, dir, i)`
für die 11 Farbtreffer von „Sechzehn Stunden".

Alle Systeme werden gepoolt (`GetPool`) und manuell emittiert — kein Instantiate-Spam.
Globale Regler: `VFXManager.intensity` (0–2) und `gore` (Blut aus für Streams/Jugendschutz).

---

## 6. HUD → `HudStatusBars.cs` + `UIManager.cs`

- **Lebensbalken:** Verlauf Blutrot → Orange → Gold (`UIManager.HealthColor`)
- **Fatal-Blow-Leiste:** Neonblau, pulsiert blutrot, sobald bereit
- **Mojo Bob:** 7 goldene Punkte + Krit-Prozent
- **Mell:** Puls-Balken 60–220 bpm, Farbe Neonblau → Blutrot, schlägt im Takt
- **Le Binde:** Schmier-Schlüppa-Ladungen (Gold → Braun)
- **Alle anderen:** Fatal-Blow-Ladung in Signaturfarbe
- **Callouts:** `FloatingText.Show(...)` / `FloatingText.ShowDamage(...)`

---

## 7. Kamera & Post-Processing

### `CameraShake.cs` (läuft additiv nach dem `CameraController`)
- `Shake(pixel, dauer)` — Amplitude in „Pixeln" laut Spec-Tabelle (2/5/8/12/15/20)
- `SetComboZoom(combo)` — FOV-Zoom 1,05× / 1,10× / 1,15× / 1,25×, weich über ~0,3 s
- `HitStop(s)`, `FrameFreeze(frames)`, `SlowMotion(scale, s)` — Zeitlupe für Mops-Kommando (0,1×), X-Ray (0,5×), Fatality (0,3×), Krit (Freeze)

### `ScreenEffects.cs` (Overlay-Canvas, ohne Package-Zwang)
Vignette (prozedurale Textur, Basis 0,3), Farbblitz, Blut-Splatter auf der Linse,
Puls-Rand, Blackout-Sequenz, Dauer-Tint (Lockout/Gift).

### Benannte Bildschirm-Zustände → `ScreenState`
`Normal` · `Dingeneldang` (Gold-Überbelichtung, goldener Rand, Bloom 2,0) ·
`MojoLockout` (grauer Filter, Sättigung −45) · `Fatality` (Vignette 0,9, CA 0,8,
Sättigung −20) · `Fusel` (oranger Filter + Trunkenheits-DoF) · `Matrix` (grüner
Glitch) · `RatSwarm` (brauner Schleier) · `Monochrome` (Blackout/X-Ray).
Aufruf: `ScreenEffects.SetState(ScreenState.Fatality)` — leitet bei aktivem
`PK_URP` automatisch an den Post-Processing-Driver weiter.

### Charakter-Shader → `Assets/Shaders/PennerCharacter.shader` + `CharacterShaderBinder.cs`
Ein URP-Forward-Shader mit Rim-Light und sechs Signatur-Modi:
`1 Grease` (öliger Wanderfilm) · `2 Pulse` (Herzschlag, Frequenz aus `Mell.Pulse`) ·
`3 Gold` (Metallic-Blitz, Intensität aus Mojo-Punkten) · `4 Fire` (Flammen auf der
Kleidung) · `5 Matrix` (fallende Zeichen) · `6 Rat` (Silhouetten).
Dazu `_Flash` für die Überbelichtung beim Treffer — der Binder setzt alles per
`MaterialPropertyBlock`, ohne Material-Instanzen zu erzeugen.

### `UrpPostProcessingDriver.cs` (optional, Define `PK_URP`)
Setzt das volle URP-Profil: Bloom (Schwelle 0,8 · Intensität 0,5), ACES-Tonemapping,
Kontrast +15 %, Sättigung +5 %, Weißabgleich +5, Vignette 0,3–0,6 (combo-abhängig),
Chromatic Aberration 0,1 (Fatality 0,5), DoF (Bokeh, Blende 2,0), Film Grain 0,05.

**Aktivieren:** Project Settings → Player → *Scripting Define Symbols* → `PK_URP`
(erst, wenn das URP-Package im Projekt liegt). Ohne Define bleibt alles funktionsfähig.

---

## 8. Audio-visuelle Kopplung

| Ereignis | Bild | Ton |
|---|---|---|
| Leichter Treffer | kleiner Funke + Blut | „Paff" |
| Schwerer Treffer | großer Funke + Blut + Shake | „Wumm" + Echo |
| Block | weißer Abprall | Klack |
| Spezial / EX | Charakterfarbe + Ring | Charakter-SFX |
| Krit (Bob) | Goldblitz + Freeze | „DINGENELDANG!" + Glocke |
| Combo 3/5/8/10+ | Zähler-Pop, Zoom, Vignette | Ping-Leiter (prozedural, falls kein Clip) |
| Puls ≥ 160 (Mell) | roter Rand im Herzschlag | Herzschlag |
| Blackout | Schwarz + Elektroflackern | Herzton flacht ab |
| Fatality | Blutfontäne + Linsen-Splatter | Sub-Bass + Schmatz |

---

## 9. Integration & Setup

1. `Tools → Penner Kombat → Setup-Szene erzeugen` — legt Boden (nasses Pflaster),
   Spawns, `Bootstrapper` **und** `ArenaVisuals` an.
2. Der `Bootstrapper` erzeugt beim Start: `VFXManager`, `ComboSystem`,
   `ComboCounterUI`, `HudStatusBars`, `ScreenEffects`, `CameraShake`, `ArenaVisuals`
   (einzeln abschaltbar über Checkboxen).
3. `TestRunner` prüft die VFX-Schicht mit: „✅ VFX-Schicht", „✅ Farbpalette",
   „✅ Combo-Stufen".

### Performance-Schalter
| Regler | Wirkung |
|---|---|
| `VFXManager.intensity` | Partikelmenge global (0 = aus, 2 = doppelt) |
| `VFXManager.gore` | Blut komplett aus |
| `Bootstrapper.create*` | einzelne Systeme abschalten |
| `CharacterVisuals.enableAura` | Aura-Lichter aus (mobile) |
| `ArenaVisuals.spawnAmbientParticles` | Atmosphäre-Partikel aus |

---

## 10. Was noch Art-Assets braucht

Prozedural abgedeckt sind Licht, Partikel, Screen-FX, HUD und Callouts.
**Nicht** ersetzbar sind: Charaktermodelle/Sprites im Cover-Stil, Animationen
(Frame-Daten stehen in `docs/MOVESETS.md`), Arena-Props (Bierkästen, Wäscheleine,
Neonschild-Mesh) und die Sprach-Samples („DINGENELDANG!", „KOMBO!").
Sobald diese existieren, werden sie in den Prefab-Feldern von `VFXManager`,
`ArenaVisuals` und `ComboSystem` zugewiesen und überschreiben die prozeduralen
Platzhalter.
