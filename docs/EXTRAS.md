# 💀 EXTRAS — NIX FÜR PUSSYS

Die zehn Systeme, die aus einem Prügelspiel eine Schlägerei im brennenden
Hinterhof machen. Alles prozedural, alles ohne Art-Assets lauffähig.

---

## 1. Arena-Zerstörung → `VFX/ArenaDestruction.cs`

Fünf Stufen, getrieben vom **kumulierten Schaden der ganzen Runde**:

| Stufe | Schaden | Was passiert |
|---|---|---|
| Intakt | 0 | Hinterhof steht |
| Beschädigt | 80 | 30 % der Props kippen, erste Trümmer |
| Alles kaputt | 200 | 60 % kippen, doppelte Trümmermenge, Staub |
| Es brennt | 350 | Gasflaschen explodieren, Feuer + Punktlichter |
| Nur noch Schutt | 550 | Hof wird komplett zerlegt, Feuermeer, Orangeblitz |

Nebenbei wandern Nebeldichte und Ambientlicht ins Rötliche — die Arena wird
mit jeder Stufe sichtbar düsterer. `ResetArena()` räumt zur nächsten Runde auf.

## 2. Beat-Sync → `Core/MusicSync.cs`

Zählt die Beats des laufenden Tracks und feuert `OnBeat`, `OnHalfBar`, `OnBar`.
**Die Combo treibt das Tempo:** +0,6 BPM pro Treffer bis maximal 180, der Pitch
zieht mit (der 808 wird härter). `Duck()` senkt die Musik für Cinematics,
`CutAndResume()` schneidet sie bei Fatalities hart weg.

## 3. Ragdoll → `VFX/RagdollController.cs`

Beim K.o. übernimmt Physik statt Todes-Animation. Ist ein Ragdoll-Rig
vorhanden, wird es aktiviert; ohne Rig zerlegt sich die Platzhalter-Kapsel in
fünf Bruchstücke mit Impuls und Drall. `Deactivate()` setzt alles zurück.

## 4. Wunden & Blut → `VFX/DamageVisuals.cs`

Jeder Treffer ab 6 Schaden hinterlässt einen Fleck am Körper (Größe skaliert
mit Schaden, maximal 12, danach Recycling) und eine Lache am Boden (max. 10,
20 s Standzeit). Bei sinkender HP wird der Kämpfer fahler und blutiger.

## 5. Konter → `Combat/ParrySystem.cs`

Wer **innerhalb von 0,16 s nach Blockbeginn** getroffen wird, kontert:
Combo des Gegners bricht, 14 Schaden, 14 Knockback, 0,55 s Stun, Hitstop,
blauer Blitz — und **1,2 s Bestrafungsfenster mit ×1,35 Schaden**.
Cooldown 1,2 s, damit Dauerblocken nichts bringt.

## 6. Waffen → `Combat/WeaponSystem.cs`

Fünf Fundstücke liegen im Hof und werden mit `E` / `Num .` aufgehoben:

| Waffe | Schaden | Effekt | Nutzungen |
|---|---|---|---|
| Schraubenzieher | 14 | Blutung (5 Ticks) | 6 |
| Maulschlüssel | 16 | Wallbounce | 6 |
| Rohrzange | 18 | Anti-Air | 6 |
| Kochlöffel | 9 | Stun 0,4 s | 6 |
| Rattengift | 8 | Giftwolke + DoT | 6 |

Mit Waffe in der Hand ersetzt `WeaponHolder.Strike()` den Standardschlag;
nach der letzten Nutzung fällt sie aus der Hand. Pickup respawnt nach 15 s.

## 7. Power-Ups → `VFX/PowerUpSystem.cs`

Alle 12 s taucht eins auf (max. 3 gleichzeitig, 20 s Standzeit):
**Bier** (+25 % Schaden), **Med** (+30 HP), **Speed** (×1,5), **Schild**
(+50 % Blockreduktion), **Feuer**, **Eis** (verlangsamt den Gegner),
**Pfand**, **Todeskuss** (×2,5 Schaden für 5 s).

## 8. Verbündete → `VFX/AllySummon.cs`

**Mercedes 190e:** Das Auto fährt vor, drei Atzen steigen aus (je 30 HP,
8 Schaden, 12 s), danach fährt es wieder ab. Cooldown 45 s.
**Punker-Crowd** („Kantenkanten Nackenschelle"): ab Combo 5 stürmen fünf Punks
aus fünf Richtungen, schlagen einmal zu (8/10/12/10/15 Schaden) und
verschwinden; Finale ist ein gemeinsamer Tritt mit Druckwelle. Cooldown 30 s.

```csharp
AllySummon.Ensure().CallMercedes(fighter);
AllySummon.Ensure().CallPunks(fighter);   // braucht Combo ≥ 5
```

## 9. Zuschauer → `VFX/CrowdReactions.cs`

24 Figuren im Ring wippen **im Takt der Musik** (Frequenz aus `MusicSync`).
Stimmung folgt der Combo: ab 4 interessiert, ab 8 wild, ab 15 Ekstase;
Fatality setzt „entsetzt". Je höher die Energie, desto höher die Sprünge.

## 10. Bosse → `AI/BossController.cs`

Vervierfachte HP und bis zu drei Phasen (bei 66 % und 33 % HP):
Schwierigkeit springt auf `VeryHard` bzw. `Boss`, Tempo +12 BPM pro Phase,
8 % Heilung, Zeitlupe, Druckwelle, roter Aura-Burst und Ansage.

```csharp
var boss = enemyGo.AddComponent<BossController>();
boss.bossName = "Der Baron";
```

---

## Verdrahtung

`FighterController` hängt `DamageVisuals`, `RagdollController`, `ParrySystem`
und `WeaponHolder` selbst an. Der `Bootstrapper` erzeugt `MusicSync`,
`ArenaDestruction`, `PowerUpSystem`, `CrowdReactions`, `AllySummon` und
verteilt das Waffen-Arsenal — alles einzeln abschaltbar. Der `GameManager`
räumt bei Matchende über `ResetExtras()` auf.

---

## Grenzen — ehrlich

| Punkt | Stand |
|---|---|
| **Ragdoll ohne Rig** | Bruchstücke statt Knochenphysik. Mit echtem Humanoid-Rig greift automatisch der bessere Pfad |
| **Zuschauer** | Kapseln, keine Animationen; Jubel-Sounds brauchen Audio-Clips im `AudioManager` |
| **Atzen/Punks** | Verhalten steht, Modelle und Sprüche fehlen |
| **Bosse** | Nutzen das Moveset des jeweiligen Charakters — eigene Boss-Attacken gibt es nicht |
| **Wunden** | Quads am Körper, keine echten Decals auf der Haut (dafür bräuchte es ein Decal-System oder Shader-Masken) |
| **Getestet** | Nichts davon lief bisher in Unity — siehe [STATUS.md](STATUS.md) |
