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
| Größe | auf 1,80 m normiert, Füße auf y = 0, x/z zentriert |
| Rigidbody | Masse 1, Rotation gesperrt, Interpolation, Continuous |
| CapsuleCollider | Höhe und Radius aus den **echten Modellmaßen** |
| AttackPoint | am **rechten Handknochen**, wenn ein Rig da ist (Humanoid-Avatar oder Namen wie `mixamorig:RightHand`, `Hand_R`) — sonst vor dem Körper |
| Hitbox-Größe | aus Radius, Höhe und `attackRange` des Charakters |
| Animator | Controller + Avatar vom Modell auf die Prefab-Wurzel gezogen |
| Tag / Layer | `Fighter` |
| Ablage | `Assets/Prefabs/Fighters/PK_<id>.prefab`, wird bei erneutem Import **aktualisiert**, nicht dupliziert |
| Eintrag | automatisch in `Assets/Resources/FighterDatabase.asset` |

FBX-Dateien in diesem Ordner werden beim ersten Import zusätzlich gleich auf
**Humanoid-Rig** gestellt (Kameras/Lichter aus).

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

## 4. Animationen

Ein GLB **kann** Animationen mitbringen (z. B. Mixamo-Export). Dann gilt:

- Im Editor-Weg (A) wird ein vorhandener `Animator` mit Controller und Avatar auf die
  Prefab-Wurzel übernommen.
- `AnimationsController3D` setzt nur Parameter, die im Controller wirklich existieren:
  `Walk`, `Block`, `Jump`, `LightAttack`, `HeavyAttack`, `HitReact`, `Death`, `Roll`, `Special`.
- Fehlt ein Parameter, passiert nichts — das Modell steht dann in T-Pose oder Idle, der Kampf
  läuft trotzdem.

Für saubere Kampfanimationen braucht es einen Animator-Controller mit genau diesen Namen.
Das ist Editor-Arbeit und lässt sich nicht sinnvoll erraten.

---

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
