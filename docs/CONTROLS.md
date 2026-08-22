# 🎮 STEUERUNG, KI & MULTIPLAYER

Alles zu Eingaben (Tastatur, Gamepad, Touch), zum KI-Gegner und zu den
Multiplayer-Modi. Touch im Detail: [TOUCH.md](TOUCH.md).

---

## 1. Tastatur

### Spieler 1 (WASD)

| Aktion | Taste |
|---|---|
| Bewegung | `W` `A` `S` `D` |
| Leichter Angriff (□) | `J` |
| Schwerer Angriff (△) | `K` |
| Block | `Shift` |
| Sprung | `Leertaste` |
| Spezial 1 | `U` |
| Spezial 2 | `I` |
| EX-Move | `O` |
| Interaktion | `E` |
| X-Ray / Fatal Blow | `Y` |
| Med-Kapsel | `H` |
| Pause | `Esc` |

### Spieler 2 (Pfeiltasten + Nummernblock)

| Aktion | Taste |
|---|---|
| Bewegung | `↑` `←` `↓` `→` |
| Leichter Angriff | `Num 1` |
| Schwerer Angriff | `Num 2` |
| Block | `Num 3` |
| Sprung | `Num 0` |
| Spezial 1 | `Num 4` |
| Spezial 2 | `Num 5` |
| EX-Move | `Num 6` |
| Interaktion | `Num .` |
| X-Ray / Fatal Blow | `Num +` |
| Med-Kapsel | `Num Enter` |

**Umbelegen:** `KeyboardLayoutUI` zeigt beide Spalten und belegt auf Klick neu
(`Esc` bricht ab). Gespeichert wird pro Aktion in den PlayerPrefs
(`pk_key_<spieler>_<aktion>`), zurücksetzen über `KeyBindings.Reset()`.

**Gamepad** läuft parallel: Pad #1 = Spieler 1, Pad #2 = Spieler 2
(□ = A/Süd, △ = B/Ost, Sprung = Y/Nord, Block = Schultertasten,
EX = rechter Trigger, Interaktion = linker Trigger, Med = D-Pad hoch).

### Spezialbewegungen

Spezials laufen **nicht** über Extratasten, sondern über den vorhandenen
`CommandInput` in Numpad-Notation — identisch für Tastatur, Pad und Touch:

| Motion | Eingabe P1 | typischer Move |
|---|---|---|
| ↓↘→ + □ | `S` → `S+D` → `D` + `J` | Flaschenhals, Pampe, Riesenschwanz |
| ↓↙← + □ | `S` → `S+A` → `A` + `J` | gespiegelte Varianten |
| →→ + □ | `D` `D` + `J` | Doppelschicht, Dashs |
| ↓↓ + ○ | `S` `S` + `U` | REIF!, Kater, Ventil |
| →↘↓↙← + △ | Halbkreis + `K` | Sechzehn Stunden, Löffelsturm |

Frame-Daten und vollständige Listen: [MOVESETS.md](MOVESETS.md).

---

## 2. Med-Kapsel

`MedSystem` hängt automatisch an jedem Kämpfer:
**2 Ladungen**, **+25 HP**, 1,2 s Anwendung (verwundbar), 8 s Abklingzeit,
Reset bei Rundenbeginn. Die KI nutzt sie unter 30 % HP — aber nur, wenn der
Gegner nicht in Schlagdistanz steht.

---

## 3. KI-Gegner — sechs Stufen

| Stufe | Reaktion | Block | Combo | Spezials | Schaden |
|---|---|---|---|---|---|
| Sehr leicht | 0,8–1,2 s | 10 % | — | 5 % | 0,6× |
| Leicht | 0,5–0,9 s | 20 % | 15 % | 12 % | 0,8× |
| Mittel | 0,3–0,6 s | 35 % | 30 % | 25 % | 1,0× |
| Schwer | 0,15–0,35 s | 50 % | 50 % | 35 % | 1,2× |
| Sehr schwer | 0,08–0,2 s | 65 % | 70 % | 45 % | 1,4× |
| Boss | 0,05–0,15 s | 80 % | 90 % | 55 % | 1,6× |

- **Combo-Ketten:** 3 Schritte (Sehr leicht) bis 13 (Boss), pro Kette neu
  gewürfelt aus Light/Heavy/Jump/Spezial; Abstand zwischen den Schritten
  sinkt von 0,45 s auf 0,18 s. Die Kette bricht ab, wenn der Gegner aus der
  Reichweite läuft.
- **Schadensmultiplikator** wird einmalig auf `lightDamage`/`heavyDamage`
  angewendet, nicht pro Treffer — sonst würde er sich aufmultiplizieren.
- Umschalten zur Laufzeit: `aiController.SetDifficulty(AIDifficulty.Hard)`.

---

## 4. Multiplayer

### 4.1 Lokal (ein Gerät)
Spieler 1 auf WASD, Spieler 2 auf Pfeiltasten/Nummernblock — oder Spieler 2
als KI mit wählbarer Stufe. Auswahl im Multiplayer-Menü.

### 4.2 Kopplung per Raumcode / QR
Der Host erzeugt einen sechsstelligen Code (ohne verwechselbare Zeichen) und
den Payload `PK://<ip>:<port>/<raum>`. Der zweite Spieler tippt den Code ein
oder wählt den Host aus der LAN-Liste.

> **QR ist optional:** Das Repo bringt keinen QR-Encoder mit. Ohne ZXing zeigt
> `QrCoupling` den Textcode (funktioniert vollwertig). Mit ZXing im Projekt und
> Scripting-Define **`PK_ZXING`** wird derselbe Payload als QR gerendert und
> per Kamera scannbar.

### 4.3 LAN-Auto-Discovery
`LanDiscovery` sendet als Host alle 2 s einen UDP-Broadcast
(`PK1|ip|port|raum|name`, Port 47654) und hört als Client darauf. Empfang läuft
in einem Hintergrund-Thread, Auswertung über eine Queue auf dem Main-Thread.
Hosts verschwinden nach 8 s ohne Lebenszeichen aus der Liste.

### 4.4 Online-Relay
`NetworkManager` + `WebSocketClient` verbinden sich mit dem in der
Konfiguration hinterlegten Relay (`ws://…`). `HostRoom()`, `JoinHost(code)` und
`LeaveRoom()` sind die Einstiegspunkte; Ereignisse (`OnPeerJoined`,
`OnMatchStart`, `OnPeerUpdate`, `OnPeerAction`) liefern die Gegenseite.
Der passende Server liegt in `server/` — Start und Protokoll: [SERVER.md](SERVER.md).

### Konfiguration
`MultiplayerConfig` (JSON in PlayerPrefs, Key `pk_mp_config`): Spielername,
Farbe, Port, Relay-URL, max. Spieler, Ping-Timeout, zuletzt genutzter Modus,
KI-Stufe.

---

## 5. Was hier ehrlich fehlt

| Punkt | Stand |
|---|---|
| **Relay-Server** | ✅ ergänzt: `server/` (Node + `ws`), siehe [SERVER.md](SERVER.md). Ein Gerät im Netz startet ihn, beide Clients verbinden sich dorthin. |
| **Direkter P2P-WebSocket** | Der Host öffnet weiterhin keinen eigenen Server im Spiel — es läuft immer über den Relay (lokal oder auf einem VPS). |
| **Netcode/Rollback** | Zustandsabgleich ist simples State-Sending, keine Vorhersage, kein Rollback. Für ein Fighting Game über das Internet zu wenig. |
| **QR-Encoder/Scanner** | Nur mit ZXing (Define `PK_ZXING`). |
| **Split-Screen** | Zwei Spieler teilen sich eine Kamera; getrennte Viewports sind nicht umgesetzt. |
| **Getestet** | Nichts davon lief bisher in Unity — siehe [STATUS.md](STATUS.md). |
