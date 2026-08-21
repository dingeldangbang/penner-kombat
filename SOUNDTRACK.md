# 🎵 Soundtrack — Alle 31 Tracks

Audiomanagement: `AudioManager` (Musik/SFX/Voice/Ambient). Die Tracks werden
über `battleTracks` / `characterThemes` / `menuTracks` / `storyTracks` geladen.
`GetCharacterThemeIndex()` mappt Charakter → Theme (inkl. der drei neuen Tracks 29–31).

## Teil 1 — Basis-Tracks (1–28)

| # | Track | Kontext |
|---|-------|---------|
| 1 | „Schrottplatz-Rhythmus" | Arena: Schrottplatz |
| 2 | „Beton und Regen" | Arena: Straße |
| 3 | „Kanal-Riff" | Arena: Kanalisation |
| 4 | „Neon-Graffiti" | Arena: U-Bahn |
| 5 | „Ziegel-Drums" | Arena: Hinterhof |
| 6 | „Dampf-Rock" | Arena: Dampfkessel |
| 7 | „Lagerhall-Blues" | Arena: Lagerhaus |
| 8 | „Blechdosen-Samba" | Arena: Recyclinghof |
| 9 | „Autowrack-Metal" | Arena: Schrottplatz II |
| 10 | „Laternen-Jazz" | Arena: Nachtstraße |
| 11 | „Plattenbau-Punk" | Arena: Plattenbau |
| 12 | „Gully-Techno" | Arena: Kanal II |
| 13 | „Leergut-Rhumba" | Arena: Getränkemarkt |
| 14 | „Flaschenklang" | Arena: Flaschenlager |
| 15 | „Regenrohr-Funk" | Arena: Industrie |
| 16 | „Bratkartoffel-Rock" | Arena: Imbiss |
| 17 | „Schmiede-Metal" | Arena: Werkstatt |
| 18 | „Taubenschlag-Swing" | Arena: Dach |
| 19 | „Altglas-Beat" | Arena: Glascontainer |
| 20 | „Rohrpost-Reggae" | Arena: Heizungskeller |
| 21 | „Mülltonnen-March" | Arena: Gasse |
| 22 | „Pulsierender Beton" | Story: Kapitel 1–2 |
| 23 | „Pankow-Drive" | Story: Kapitel 3–4 |
| 24 | „Hinterhof-Hymne" | Story: Kapitel 5–6 |
| 25 | „Turnier-Fanfare" | Story: Kapitel 7 |
| 26 | „Finales Drittel" | Story: Kapitel 8 |
| 27 | „Blaue-Eimer-Ouvertüre" | Hauptmenü |
| 28 | „Tagesanbruch im Kiez" | Story-Menü / Options |

## Teil 2 — DLC-Tracks (29–31) — Die drei neuen

| # | Track | Charakter | Stil / Dauer |
|---|-------|-----------|--------------|
| 29 | **„Le Bindes Mops-Walzer"** | Le Binde | 3/4-Takt-Polka trifft Doom Metal · 4:08 |
| 30 | **„Mells Puls"** | Mell | Breakcore 180→220 BPM, beschleunigt live mit ihrer Combo · 3:12 |
| 31 | **„Mojo (Dingeldang Dub)"** | Mojo Bob | Dub-Reggae mit Löffelperkussion und Zufalls-Sampler · 5:55 |

## Implementierung
- Die drei DLC-Themes werden in `AudioManager.characterThemes` an den Indizes
  **29, 30, 31** abgelegt; `GetCharacterThemeIndex()` liefert genau diese für
  `le_binde`, `mell`, `mojo_bob`.
- Fehlende Clips müssen als Audio-Assets unter `Assets/Resources/Audio/Music/`
  abgelegt werden (Setup siehe `docs/SETUP.md`).
