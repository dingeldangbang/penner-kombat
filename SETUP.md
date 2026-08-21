# Setup-Anleitung (Unity)

Diese Anleitung führt durch den Aufbau einer **minimalen, spielbaren Arena-Szene**
mit dem Code aus diesem Repo.

---

## 1. Projekt anlegen

1. **Unity Hub** öffnen → **Neues Projekt** → **3D (URP)**.
2. Name: `Penner Kombat`, Pfad beliebig.
3. Unity **2023 LTS** oder **Unity 6** verwenden.

## 2. Code kopieren

Ordner `Assets/Scripts` (und optional `docs`, `LICENSE`, `README.md`)
aus diesem Repo in `Assets/` des neuen Projekts kopieren.

## 3. Pakete aktivieren

**Package Manager** → installieren:

| Paket | Warum |
|-------|-------|
| **Input System** (`com.unity.inputsystem`) | `FighterInput` nutzt `Gamepad`/`Keyboard`-API |
| **TextMeshPro** (TMP Essentials importieren) | HUD/Dialoge nutzen `TextMeshProUGUI` |

Danach: **Edit → Project Settings → Player → Active Input Handling → "Both"**
(oder "Input System Package"). Sonst wirft `FighterInput` beim Start ggf. eine Exception.

## 4. Tags & Layer

**Project Settings → Tags and Layers**:

| Tag | Zweck |
|-----|-------|
| `Ground` | Boden-Collider |
| `Fighter` | Kämpfer (Boden-Berührung + Ziel-Layer) |
| `Interactable` | Arena-Props |
| `Projectile` | Projektile |

Die **`enemyLayer`**-Maske jedes Kämpfers auf einen `Fighter`-Layer setzen.

## 5. Minimal-Szene

Neue Szene **`Arena`**:

1. **Directional Light** + ein **GameObject `Boot`** → `Bootstrapper`-Komponente anhängen.
   → Erzeugt automatisch: `FighterInput`, `AudioManager`, `ArenaManager`, `CameraController`, `FatalBlowSystem`, `GameManager`.
2. **Ebene** (Plane) mit Tag `Ground` + Collider.
3. **Zwei Spawn-Punkte** (`Empty`) → im `GameManager` als `spawnPoint1/spawnPoint2` zuweisen.
4. **FighterDatabase**-Asset anlegen:
   *Project → Create → PennerKombat → FighterDatabase*.
   Im `GameManager` zuweisen. (`EnsureDefaultRoster()` füllt die 9 Charaktere automatisch.)

## 6. Kämpfer-Prefabs

Für jeden Charakter ein Prefab (Beispiel `Le Binde`):

- **Root**: `Capsule` + `Rigidbody` (Constraints: Rotation fixieren) + `CapsuleCollider`.
- **Child `Mesh`**: ein paar Boxes/Sphären als Körper (Kopf, Rumpf, Arme).
- **Child `AttackPoint`**: `Empty` vor der Faust → im `FighterController` zuweisen.
- **Komponenten**:
  - Charakter-Skript (z.B. `LeBinde`)
  - `Animator` mit Controller (siehe §7)
  - optional `AIController` (nur für Bots)
- **Audio**: Hit/Block/Hurt-Clips zuweisen (optional).

> Pro Tip: Ein Basis-"BotTemplate"-Prefab (nur `FighterController`-Unterklasse + AI)
> reicht für den ersten spielbaren Test gegen die KI.

## 7. Animator

Erzeuge einen Animator-Controller mit den **Parametern**, die der Code verwendet:

| Typ | Name |
|-----|------|
| Bool | `Walk` |
| Bool | `Block` |
| Trigger | `LightAttack` |
| Trigger | `HeavyAttack` |
| Trigger | `Jump` |
| Trigger | `HitReact` |
| Trigger | `Death` |

States: `Idle`, `Walk`, `Block`, `LightAttack`, `HeavyAttack`, `Jump`, `HitReact`, `Death`.

> **Wichtig:** Der Code ruft `anim.SetTrigger(...)` / `anim.SetBool(...)` mit genau diesen
> Namen auf. Fehlt ein Parameter, loggt Unity eine Warnung, kompiliert aber weiter.

## 8. HUD (UI)

1. Canvas + EventSystem anlegen.
2. HP-Balken (`Image`, Filled), Timer/Runden/Combo-`Text (TMP)`.
3. Fertiges **HUD-Prefab** mit dem `UIManager`-Skript befüllen und dem `GameManager.uiManager` zuweisen.

## 9. Test

1. **`Arena`-Szene** starten.
2. Im `GameManager.Start()` ist bereits `StartVersusFight(0, 2)` verdrahtet
   → **Le Binde (Spieler) vs. Mojo Bob (KI)**.
3. Optional `TestRunner` an ein Object hängen → Tests im Log.

---

## Balance-/Feinabstimmung

Alle zentralen Werte liegen zentral in `GameConstants.cs` (Block-Reduktion,
Fatal-Blow-Meter, Runden-Settings) und in den `[Header]`-Feldern der Charakter-Skripte.
