# 🖧 RELAY-SERVER

Der Teil, der bisher fehlte: Ohne ihn kannten sich zwei Geräte zwar per
LAN-Discovery, hatten aber keine Gegenstelle zum Verbinden. Der Relay hält
Räume und spiegelt Nachrichten — **er kennt keinen Spielzustand** und trifft
keine Spielentscheidungen.

Ort: `server/` · Laufzeit: Node ≥ 18 · Abhängigkeit: `ws`

---

## Start

```bash
cd server
npm install
npm start                 # ws://0.0.0.0:5000/kombat
PORT=47654 npm start      # anderer Port
npm test                  # Smoke-Test (18 Prüfungen)
```

| Variable | Standard | Zweck |
|---|---|---|
| `PORT` | `5000` | Listen-Port |
| `HOST` | `0.0.0.0` | Bind-Adresse |
| `WS_PATH` | `/kombat` | WebSocket-Pfad |
| `MAX_PLAYERS` | `4` | Spieler pro Raum |
| `HEARTBEAT_MS` | `15000` | Ping-Intervall (tote Verbindungen fliegen raus) |
| `IDLE_ROOM_MS` | `300000` | Leerlauf, nach dem leere Räume verschwinden |

HTTP-Endpunkte zum Prüfen: `GET /health` → `{"ok":true,...}` · `GET /rooms` → Raumliste.

---

## Protokoll

Alles ist flaches JSON, eine Nachricht pro Frame. Raum wahlweise per
Query-String (`ws://host:5000/kombat?room=AB12CD`) oder per `join`-Nachricht.

### Client → Server

| `type` | Felder | Wirkung |
|---|---|---|
| `join` | `room`, `player` | Raum betreten (wird angelegt, falls neu). Erster Spieler ist Host |
| `leave` | — | Raum verlassen |
| `ready` | `ready` (bool) | Bereitschaft; sind **alle ≥ 2** bereit, sendet der Server `start` |
| `ping` | `t` | Antwort `pong` mit demselben `t` (Latenzmessung) |
| `update` / `state` | beliebig (`pos`, `hp`, `combo` …) | wird 1:1 an die anderen gespiegelt |
| `action` | `action` | dito, z. B. `light_attack`, `special1` |
| `chat` | `text` | dito |
| `round`, `hit`, `fatality` | beliebig | dito |

### Server → Client

| `type` | Felder | Bedeutung |
|---|---|---|
| `welcome` | `player`, `maxPlayers` | eigene Spieler-ID direkt nach dem Verbinden |
| `joined` | `room`, `player`, `host` | Beitritt bestätigt |
| `peer_joined` / `peer_left` | `player`, `name` | Mitspieler kam/ging |
| `room_state` | `players[]` mit `id`, `name`, `ready`, `host` | vollständige Raumliste nach jeder Änderung |
| `start` | `room`, `at` | alle bereit → Match starten |
| `update`, `action`, `chat`, … | Originalfelder **+ `player`, `name`** | gespiegelte Nachricht eines Mitspielers |
| `error` | `error` | `invalid_json`, `missing_room`, `room_full`, `not_in_room`, `unknown_type` |
| `pong` | `t` | Antwort auf `ping` |

**Zwei Regeln, die der Test absichert:** Der Absender bekommt seine eigene
Nachricht *nicht* zurück, und der Server ergänzt bei jeder Spiegelung `player`
und `name`, damit Clients die Quelle kennen, ohne ihr zu vertrauen.

Verlässt der Host den Raum, wird der nächste Verbliebene automatisch Host.
Ist der Raum leer, wird er gelöscht.

---

## Client-Anbindung

`NetworkManager` spricht dieses Protokoll direkt und bietet Ereignisse:

```csharp
var nm = NetworkManager.Instance;
nm.OnJoined      += room => Debug.Log($"Raum {room}");
nm.OnPeerJoined  += (id, name) => Debug.Log($"{name} da");
nm.OnMatchStart  += () => GameManager.Instance.StartVersusFight(0, 1);
nm.OnPeerUpdate  += json => ApplyRemoteState(json);
nm.OnPeerAction  += action => ReplayRemoteAction(action);

var code = nm.HostRoom();          // Raumcode + LAN-Broadcast
nm.JoinHost(code);                 // auf dem zweiten Gerät
nm.SendAction("special1");
nm.SendChat("Reif!");
```

Die Relay-URL steht in `MultiplayerConfig.relayServer`
(Standard `ws://localhost:5000/kombat`) und lässt sich im Multiplayer-Menü ändern.

---

## Deployment

**Lokal / LAN:** Ein Gerät (oder ein PC im selben Netz) startet den Server,
beide Clients tragen dessen IP ein — oder finden ihn per LAN-Discovery.

**Internet:** Auf einem kleinen VPS hinter einem Reverse Proxy mit TLS:

```nginx
location /kombat {
    proxy_pass http://127.0.0.1:5000;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_read_timeout 300s;
}
```

Clients nutzen dann `wss://deine-domain/kombat`.

### Android 11–15 (API 30–35)

- Die Client-URL wird an einer Stelle gesetzt: `network/relay/url` in
  `project.godot` (gelesen von `NetworkManager.gd`/`WebSocketClient.gd`, wenn
  deren `@export`-URL leer bleibt). Für Android **`wss://`** verwenden — Klartext
  `ws://` ist ab API 28 blockiert.
- Nur für lokale Entwicklung: `./Tools/install_android_build_template.sh --relay`
  ergänzt `android:usesCleartextTraffic="true"` im Gradle-Build-Template
  (`android/build/src/main/AndroidManifest.xml`). Für das Prebuilt-APK gilt das
  nicht — dort bleibt `wss://` der Weg.
- Weitere Details: [docs/ANDROID_11_15.md](ANDROID_11_15.md).

---

## Was der Relay bewusst **nicht** tut

| Punkt | Konsequenz |
|---|---|
| **Keine Authentifizierung** | Wer die Raum-ID kennt, kommt rein. Für öffentliche Server bräuchte es Tokens |
| **Keine Autorität über den Spielzustand** | Ein manipulierter Client kann `hp` beliebig senden — Cheating ist möglich |
| **Kein Rollback/Prediction** | Nachrichten werden nur weitergereicht; bei > ~60 ms Latenz fühlt sich der Kampf schwammig an. Für ernsthaftes Online-Fighting bräuchte es ein deterministisches Lockstep- oder Rollback-Modell (GGPO-Prinzip) |
| **Keine Persistenz** | Räume, Ranglisten und Statistiken leben nur im Arbeitsspeicher |
| **Kein TLS** | `wss://` über Reverse Proxy lösen |

Diese Punkte sind Design-Entscheidungen für einen kleinen Fan-Titel — sie
stehen hier, damit niemand den Relay für produktionsreifes Matchmaking hält.
