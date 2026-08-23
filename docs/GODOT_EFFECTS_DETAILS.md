# Penner Kombat — Effekte & Details

Dieses Paket ergänzt die Godot-Mobile-Implementierung um visuelle Spektakel, HUD, Post-Processing, interaktive Arena-Objekte, Beat-Sync, Charaktervisuals, Lokalisierung und mobile Performance-Schalter.

## Effektmodule

| Datei | Zweck |
|---|---|
| `scripts/effects/BloodSplat.gd` | Schadensskalierte Blutpartikel mit dunkler Farbvariation |
| `scripts/effects/HitSpark.gd` | Trefferfunken mit kritischem 2-Frame Freeze |
| `scripts/effects/Shockwave.gd` | Billboard-Ring als kurze Schockwelle |
| `scripts/effects/CritGold.gd` | Goldene Sterne für Krit-/Mojo-Treffer |
| `scripts/effects/HealingEffect.gd` | Grüne aufsteigende Heilpartikel |
| `scripts/effects/FireEffect.gd` | Feuer-/Fusel-Atem-Partikel mit Turbulenz |
| `scripts/effects/ElectricEffect.gd` | Cyanfarbene Blitz-Arcs plus Screen-Flash |
| `scripts/effects/FatalityBlood.gd` | Große Blutfontäne für Fatality-Momente |
| `scripts/effects/ScreenShake.gd` | Globale Kamera-Shake-Instanz |
| `scripts/effects/PostProcessing.gd` | Glow, ACES Tonemap, Vignette-Overlay, Slow-Mo, Fatality/X-Ray Looks |

`CombatController` erzeugt bei Treffern automatisch HitSpark, BloodSplat, Shockwave, CritGold bei hohem Schaden und ScreenShake. Heilung erzeugt `HealingEffect`.

## HUD

- `scripts/ui/HUD.gd`
- `scenes/ui/HUD.tscn`

Features:

- P1/P2 Healthbars mit Farbwechsel
- Combo-Counter mit Tween-Animation und Shake bei hohen Combos
- Timer und Rundenlabel
- Status-Icons
- Damage-Popup
- Mojo-, Puls- und Fett-Specialmeter

`MainUI` instanziert das HUD automatisch und verbindet Player-Health sowie Combo-Signale.

## Arena & Audio

- `scripts/world/InteractiveObject.gd` für Bierkästen, Gasflasche, Wäscheleine, Mülltonne, Baugerüst und Neon-Schild.
- `scripts/audio/BeatSync.gd` mit Godot-Spectrum-Analyzer für Beat-getriggerte Pulse.
- `scripts/audio/AudioManager.gd` als statischer Positions-SFX-Manager.

## Charaktervisuals

- `scripts/characters/GreaseEffect.gd`
- `scripts/characters/PulseVisuals.gd`
- `scripts/characters/GoldGlow.gd`

Dazu passende Shader:

- `shaders/GreaseShader.gdshader`
- `shaders/PulseShader.gdshader`
- `shaders/GoldGlowShader.gdshader`
- `shaders/XRayShader.gdshader`

## Lokalisierung & Performance

- `scripts/core/Localization.gd`
- `translations/translations.json`
- `scripts/core/PerformanceManager.gd`

Sprachen: Deutsch, Englisch, Französisch, Spanisch.
