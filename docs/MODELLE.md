# 🧊 MODELLE — GLB-Import über das Menü

Ziel: echte Charaktermodelle ins Spiel bringen, **ohne** Prefabs von Hand zu bauen.
Zwei Wege, beide über Menüs.

---

## 1. Weg A — im Editor: GLB → Kämpfer-Prefab (empfohlen)

1. **Paket installieren**: `Tools → Penner Kombat → GLB → glTFast (GLB-Import) installieren`
   (installiert `com.unity.cloud.gltfast`; alternativ Package Manager → *Add package by name*).
   Danach setzt `PkProjectSetup` automatisch das Define **`PK_GLTFAST`**.
2. **Ordner öffnen**: `Tools → Penner Kombat → GLB → Modell-Ordner anlegen und öffnen`
   → legt `Assets/Models/Fighters` an.
3. **`.glb`-Dateien hineinkopieren.** Der Dateiname muss die Charakter-ID enthalten:

   | Charakter | erwarteter Dateiname (Beispiele) |
   |---|---|
   | Le Binde | `le_binde.glb`, `lebinde_v3.glb` |
   | Mell | `mell.glb`, `mell-nurse.glb` |
   | Mojo Bob | `mojo_bob.glb` |
   | Dieter · Uschi · TetraPak | `dieter.glb` · `uschi.glb` · `tetrapak.glb` |
   | Sigi · Rolf · Kalle | `sigi.glb` · `rolf.glb` · `kalle.glb` |

4. **Prefabs bauen**: `Tools → Penner Kombat → GLB → Modelle zu Kämpfer-Prefabs machen`
   Das erzeugt pro Modell ein Prefab `PK_<id>_glb.prefab` mit Rigidbody, CapsuleCollider,
   Charakterskript, `AttackPoint` und dem Modell als Kind — automatisch auf **1,80 m**
   skaliert, auf den Boden gesetzt und mittig ausgerichtet. Anschließend wird es in der
   `FighterDatabase` eingetragen.
5. **Play.** Fertig.

Zurück zu den Kapseln: `Tools → Penner Kombat → GLB → Zurück zu Platzhalter-Kapseln`.

---

### Automatik (Standard an)

Du musst Schritt 4 gar nicht anstoßen: Ein **Import-Wächter** (`FighterModelPostprocessor`)
beobachtet `Assets/Models/Fighters`. Sobald dort eine Datei landet — per Drag & Drop, Kopie
im Explorer oder Verschieben —, wird sie erkannt und **sofort** zu einem spielfertigen
Kämpfer verarbeitet:

```
le_binde.glb  →  Assets/Prefabs/Fighters/PK_le_binde.prefab  →  FighterDatabase[le_binde]
```

Was dabei automatisch gesetzt wird:

| Schritt | Detail |
|---|---|
| Charaktererkennung | über den Dateinamen (`GuessCharacterId`) |
| Charakterskript | `LeBinde`, `Mell`, `MojoBob`, … je nach ID, mit Balance-Werten aus der Datenbank |
| Größe | auf die **Statur** des Charakters normiert (Hager 1,90 m · Normal 1,80 m · Breit 1,82 m · Adipös 1,74 m), Füße auf y = 0, x/z zentriert |
| Masse & Hitbox | aus der Statur: Adipös 1,45× Masse und 1,35× Angriffs-Hitbox, Hager 0,85× / 0,85× |
| Rigidbody | Masse 1, Rotation gesperrt, Interpolation, Continuous |
| CapsuleCollider | Höhe und Radius aus den **echten Modellmaßen** |
| AttackPoint | Reihenfolge: **Kind namens `AttackPoint` im Modell** > rechter Handknochen (Humanoid-Avatar oder Namen wie `mixamorig:RightHand`, `Hand_R`) > Fallback vor dem Körper (mit Warnung) |
| WeaponSlot | Kind namens `WeaponSlot` oder die Schlaghand → landet als `socket` im `WeaponHolder`, die Waffe erscheint dann **in der Hand** statt an der Hüfte |
| Hitbox-Größe | aus Radius, Höhe und `attackRange` des Charakters |
| Animator | Controller + Avatar vom Modell auf die Prefab-Wurzel gezogen; fehlt ein Controller, wird einer **generiert** und mit passenden Clips bestückt (§4) |
| Ragdoll | aus dem Humanoid-Rig: Rigidbodies, Collider und CharacterJoints an Hüfte, Wirbelsäule, Kopf, Armen, Beinen — kinematisch und Collider aus, bis der K.o. kommt |
| Tag / Layer | `Fighter` |
| Ablage | `Assets/Prefabs/Fighters/PK_<id>.prefab`, wird bei erneutem Import **aktualisiert**, nicht dupliziert |
| Eintrag | automatisch in `Assets/Resources/FighterDatabase.asset` |

FBX-Dateien in diesem Ordner werden beim ersten Import zusätzlich gleich auf
**Humanoid-Rig** gestellt (Kameras/Lichter aus).

**Eigene Änderungen schützen:** Jedes erzeugte Prefab trägt die Komponente
`PkAutoSetupInfo` (Quellmodell, Datum, Version). Setzt du dort das Häkchen
**„Lock Manual Edits"**, rührt die Automatik dieses Prefab nie wieder an — du kannst
Collider, Hitbox oder Sockets also gefahrlos von Hand nachziehen.

**Optionale Vorbereitung im 3D-Programm:** Leere Objekte `AttackPoint` (an der Faust) und
`WeaponSlot` (in der Hand) ins Modell legen — die werden bevorzugt verwendet und schlagen
jede Automatik.

Abschalten: `Tools → Penner Kombat → GLB → Automatik: neue Modelle sofort einrichten`
(Häkchen). Einzelne Datei nachträglich verarbeiten: im Project-Fenster markieren →
`Tools → Penner Kombat → GLB → Ausgewähltes Modell zu Kämpfer machen`.

---

## 2. Weg B — im Spiel: Modell-Menü (Taste **F7**)

Für schnelles Ausprobieren und für Handy-Builds, ohne neu zu bauen.

1. GLB-Dateien in den Modell-Ordner legen. Das Spiel sucht in dieser Reihenfolge:
   1. `Application.persistentDataPath/Models` ← **hier landet man mit „ORDNER ZEIGEN"**
   2. `Application.streamingAssetsPath/Models`
   3. `<Projektordner>/Models` (Editor/Desktop)
2. Im Spiel **F7** drücken → Liste aller neun Charaktere.
3. Pro Zeile:
   | Knopf | Wirkung |
   |---|---|
   | `<` `>` | nächstes / vorheriges Modell (eine Position ist immer „Platzhalter") |
   | `−` `+` | Größe in 5-%-Schritten (überschreibt die Automatik) |
   | `↻ 90°` | Drehung, falls das Modell seitlich oder rückwärts schaut |
   | `LEER` | zurück zur Kapsel |
4. **AUTO-ZUORDNEN** rät anhand der Dateinamen, **ÜBERNEHMEN** tauscht die Modelle im
   laufenden Kampf sofort aus.

Alle Zuordnungen liegen in `PlayerPrefs` (`pk_model_<id>`, `_scale`, `_yaw`, `_yoff`) und
überleben Neustarts.

---

## 3. Was automatisch passiert

| Schritt | Wo |
|---|---|
| Höhe auf 1,80 m normieren (oder manueller Faktor) | `GlbModelLoader.Normalize` |
| Füße auf Boden, Mitte auf x/z = 0 | dito |
| Drehung nach vorn (+Z) | dito, Wert aus `GlbLibrary.GetYaw` |
| Kapsel-Platzhalter ausblenden | `GlbModelLoader.HidePlaceholder` |
| Modell-Dateien finden und zuordnen | `GlbLibrary` |
| Nachladen im laufenden Match | `GlbModelLoader.RefreshAll` |

Physik, Hitboxen, Kamera, KI und Netzwerk bleiben unverändert — das Modell ist reine Optik,
der Collider bleibt die Kapsel. Genau so war der Code von Anfang an gebaut.

---

## 4. Animationen — Controller wird mitgebaut

Beim Auto-Setup entsteht **automatisch** ein Animator-Controller unter
`Assets/Animations/Controllers/PK_<id>.controller` — mit exakt den Parametern, die der
Kampfcode ansteuert:

| Typ | Parameter |
|---|---|
| Float | `MoveSpeed`, `Direction` |
| Int | `ComboCount`, `SpecialIndex` |
| Bool | `IsGrounded`, `Walk`, `Block` |
| Trigger | `LightAttack`, `HeavyAttack`, `Jump`, `Roll`, `HitReact`, `Death`, `Special` |

Zustandsmaschine: `Idle ⇄ Walk`, `Block` als Haltezustand, alle Aktionen als
Any-State-Übergang mit Rückkehr nach Idle (Death bleibt liegen).

**Clips werden über Schlüsselwörter im Namen zugeordnet** — passend zu dem, was Mixamo & Co.
exportieren:

| Zustand | erkannte Wörter |
|---|---|
| Idle | idle, stand, breathing |
| Walk | walk, run, jog, strafe |
| LightAttack | punch, jab, light, hook, cross |
| HeavyAttack | heavy, kick, smash, slam, strong |
| Block | block, guard, defend |
| Jump | jump, hop, leap |
| Roll | roll, dodge, evade, dive |
| HitReact | hit, impact, react, hurt, stagger |
| Death | death, dying, die, falling back |
| Special | special, combo, spell, cast, taunt |

Gesucht wird in zwei Quellen: **im Modell selbst** (GLB/FBX mit eingebetteten Clips) und in
**`Assets/Animations`** (dort einfach alle Mixamo-Downloads reinwerfen). `T-Pose`-Clips werden
übersprungen, jeder Clip wird nur einmal vergeben.

Nachträglich neu bauen: `Tools → Penner Kombat → GLB → Animator-Controller für alle Kämpfer bauen`.
Ragdoll nachrüsten: `… → Ragdoll für alle Kämpfer bauen` (braucht ein Humanoid-Rig; ohne Rig
bleibt es bei den Primitiv-Trümmern aus `RagdollController`).

Bringt das Modell bereits einen eigenen Controller mit, bleibt der unangetastet.
Und falls dort Parameter fehlen: `FighterController` prüft seit dieser Version jeden
Parameter vor dem Setzen — es gibt also keine Warnungsflut in der Konsole.

## 5. Grenzen — ehrlich

- **Ohne glTFast passiert nichts.** Der Code ist mit `#if PK_GLTFAST` abgesichert, statt
  hart abzustürzen; ohne Paket bleiben die Kapseln stehen und es kommt eine Warnung.
- **Android/StreamingAssets:** Dateien im APK lassen sich nicht mit `File.ReadAllBytes` lesen.
  Auf dem Handy funktioniert zuverlässig nur `persistentDataPath/Models` (per USB/Dateimanager
  befüllen). Für ausgelieferte Modelle ist Weg A (Prefabs im Build) der richtige.
- **Keine Materialkonvertierung geprüft:** glTFast bringt eigene URP-Shader mit. Sehen Modelle
  pink aus, fehlt das URP-Asset unter *Project Settings → Graphics*.
- **Nicht getestet:** In dieser Umgebung gibt es kein Unity — der GLB-Pfad ist geschrieben,
  aber nie ausgeführt worden. Erste Fehlermeldungen bitte durchreichen.
- **Automatik ist Namensraterei.** Heißt die Datei `char01.glb`, passiert nichts — dann
  umbenennen oder den Menüpunkt für die Auswahl nutzen.
- **Rig-Erkennung ist heuristisch.** Ohne Humanoid-Avatar wird über Knochennamen gesucht;
  exotische Namensschemata landen beim Fallback (AttackPoint vor dem Körper). Das spielt
  sich trotzdem, sitzt aber optisch nicht an der Faust.
- Skalierung nach Bounding-Box: Modelle mit Waffen/Umhängen, die weit übers Kopfende ragen,
  werden zu klein — dann im F7-Menü mit `+` nachjustieren.
