'use strict';

/**
 * Penner Kombat — WebSocket-Relay
 * ================================
 * Verwaltet Räume und spiegelt Nachrichten zwischen den Spielern eines Raums.
 * Der Server hält bewusst KEINEN Spielzustand: er kennt Räume, Teilnehmer und
 * Ready-Flags — alles andere (Position, HP, Combo, Aktionen) reicht er nur
 * unverändert an die übrigen Mitglieder weiter.
 *
 * Protokoll: siehe docs/SERVER.md
 *
 * Start:  npm start           (Port 5000, überschreibbar via PORT)
 *         PORT=47654 npm start
 */

const http = require('http');
const { WebSocketServer } = require('ws');
const { URL } = require('url');

const PORT = Number(process.env.PORT || 5000);
const HOST = process.env.HOST || '0.0.0.0';
const PATH = process.env.WS_PATH || '/kombat';
const MAX_PLAYERS = Number(process.env.MAX_PLAYERS || 4);
const HEARTBEAT_MS = Number(process.env.HEARTBEAT_MS || 15000);
const IDLE_ROOM_MS = Number(process.env.IDLE_ROOM_MS || 5 * 60 * 1000);

/** @type {Map<string, {id:string, created:number, lastActivity:number, clients:Set<object>}>} */
const rooms = new Map();
let nextClientId = 1;

// ---------------------------------------------------------------------------
//  Hilfsfunktionen
// ---------------------------------------------------------------------------

const log = (...args) => console.log(new Date().toISOString(), ...args);

function send(ws, obj) {
  if (ws.readyState !== ws.OPEN) return;
  ws.send(JSON.stringify(obj));
}

function broadcast(room, obj, exceptWs = null) {
  const payload = JSON.stringify(obj);
  for (const client of room.clients) {
    if (client === exceptWs) continue;
    if (client.readyState === client.OPEN) client.send(payload);
  }
}

function getOrCreateRoom(id) {
  let room = rooms.get(id);
  if (!room) {
    room = { id, created: Date.now(), lastActivity: Date.now(), clients: new Set() };
    rooms.set(id, room);
    log(`room + ${id}`);
  }
  return room;
}

function roomState(room) {
  return {
    type: 'room_state',
    room: room.id,
    players: [...room.clients].map((c) => ({
      id: c.playerId,
      name: c.playerName,
      ready: !!c.ready,
      host: !!c.isHost,
    })),
  };
}

function leaveRoom(ws, reason = 'leave') {
  const room = ws.room;
  if (!room) return;

  room.clients.delete(ws);
  ws.room = null;
  log(`- ${ws.playerName} (${ws.playerId}) verlässt ${room.id} [${reason}]`);

  if (room.clients.size === 0) {
    rooms.delete(room.id);
    log(`room - ${room.id} (leer)`);
    return;
  }

  // Verbliebenen Spieler zum Host machen, falls der Host ging
  if (ws.isHost) {
    const next = [...room.clients][0];
    next.isHost = true;
  }

  broadcast(room, { type: 'peer_left', player: ws.playerId, name: ws.playerName });
  broadcast(room, roomState(room));
  room.lastActivity = Date.now();
}

// ---------------------------------------------------------------------------
//  HTTP (Health-Check + Raumliste)
// ---------------------------------------------------------------------------

const server = http.createServer((req, res) => {
  if (req.url === '/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ ok: true, rooms: rooms.size, uptime: process.uptime() }));
    return;
  }
  if (req.url === '/rooms') {
    const list = [...rooms.values()].map((r) => ({
      room: r.id,
      players: r.clients.size,
      created: r.created,
    }));
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(list));
    return;
  }
  res.writeHead(200, { 'Content-Type': 'text/plain; charset=utf-8' });
  res.end('Penner Kombat Relay laeuft. WebSocket: ' + PATH + '?room=XXXXXX\n');
});

const wss = new WebSocketServer({ server, path: PATH });

// ---------------------------------------------------------------------------
//  Verbindungen
// ---------------------------------------------------------------------------

wss.on('connection', (ws, req) => {
  ws.playerId = `p${nextClientId++}`;
  ws.playerName = 'Penner';
  ws.isAlive = true;
  ws.ready = false;
  ws.room = null;

  // Raum darf schon in der URL stehen: ws://host/kombat?room=AB12CD
  let urlRoom = null;
  try {
    const parsed = new URL(req.url, `http://${req.headers.host}`);
    urlRoom = parsed.searchParams.get('room');
  } catch (_) { /* ignorieren */ }

  send(ws, { type: 'welcome', player: ws.playerId, maxPlayers: MAX_PLAYERS });
  if (urlRoom) joinRoom(ws, urlRoom, ws.playerName);

  ws.on('pong', () => { ws.isAlive = true; });

  ws.on('message', (data) => {
    let msg;
    try {
      msg = JSON.parse(data.toString());
    } catch (_) {
      send(ws, { type: 'error', error: 'invalid_json' });
      return;
    }
    handle(ws, msg);
  });

  ws.on('close', () => leaveRoom(ws, 'close'));
  ws.on('error', () => leaveRoom(ws, 'error'));
});

function joinRoom(ws, roomId, playerName) {
  if (!roomId) {
    send(ws, { type: 'error', error: 'missing_room' });
    return;
  }
  if (ws.room && ws.room.id === roomId) return;
  if (ws.room) leaveRoom(ws, 'switch');

  const room = getOrCreateRoom(String(roomId).toUpperCase());
  if (room.clients.size >= MAX_PLAYERS) {
    send(ws, { type: 'error', error: 'room_full', room: room.id });
    return;
  }

  ws.playerName = playerName || ws.playerName;
  ws.isHost = room.clients.size === 0;
  ws.ready = false;
  ws.room = room;
  room.clients.add(ws);
  room.lastActivity = Date.now();

  log(`+ ${ws.playerName} (${ws.playerId}) betritt ${room.id} [${room.clients.size}/${MAX_PLAYERS}]`);

  send(ws, { type: 'joined', room: room.id, player: ws.playerId, host: ws.isHost });
  broadcast(room, { type: 'peer_joined', player: ws.playerId, name: ws.playerName }, ws);
  broadcast(room, roomState(room));
}

function handle(ws, msg) {
  const type = msg.type;
  if (ws.room) ws.room.lastActivity = Date.now();

  switch (type) {
    case 'join':
      joinRoom(ws, msg.room, msg.player);
      return;

    case 'leave':
      leaveRoom(ws, 'message');
      return;

    case 'ready': {
      if (!ws.room) return;
      ws.ready = !!msg.ready;
      broadcast(ws.room, roomState(ws.room));

      const all = [...ws.room.clients];
      if (all.length >= 2 && all.every((c) => c.ready)) {
        broadcast(ws.room, { type: 'start', room: ws.room.id, at: Date.now() });
        log(`▶ Match startet in ${ws.room.id}`);
      }
      return;
    }

    case 'ping':
      send(ws, { type: 'pong', t: msg.t ?? Date.now() });
      return;

    // Weiterreichen ohne Interpretation: Zustand, Aktionen, Chat, Rundenevents
    case 'update':
    case 'state':
    case 'action':
    case 'chat':
    case 'round':
    case 'hit':
    case 'fatality': {
      if (!ws.room) {
        send(ws, { type: 'error', error: 'not_in_room' });
        return;
      }
      broadcast(ws.room, { ...msg, player: ws.playerId, name: ws.playerName }, ws);
      return;
    }

    default:
      send(ws, { type: 'error', error: 'unknown_type', received: type ?? null });
  }
}

// ---------------------------------------------------------------------------
//  Aufräumen
// ---------------------------------------------------------------------------

const heartbeat = setInterval(() => {
  for (const ws of wss.clients) {
    if (ws.isAlive === false) {
      leaveRoom(ws, 'timeout');
      ws.terminate();
      continue;
    }
    ws.isAlive = false;
    ws.ping();
  }

  const now = Date.now();
  for (const [id, room] of rooms) {
    if (room.clients.size === 0 || now - room.lastActivity > IDLE_ROOM_MS) {
      if (room.clients.size === 0) {
        rooms.delete(id);
        log(`room - ${id} (aufgeräumt)`);
      }
    }
  }
}, HEARTBEAT_MS);

wss.on('close', () => clearInterval(heartbeat));

server.listen(PORT, HOST, () => {
  log(`Penner Kombat Relay: ws://${HOST}:${PORT}${PATH}  (max ${MAX_PLAYERS} Spieler/Raum)`);
});

// Sauberes Beenden
for (const sig of ['SIGINT', 'SIGTERM']) {
  process.on(sig, () => {
    log(`${sig} — fahre herunter`);
    clearInterval(heartbeat);
    for (const ws of wss.clients) ws.close(1001, 'server_shutdown');
    server.close(() => process.exit(0));
    setTimeout(() => process.exit(0), 2000).unref();
  });
}

module.exports = { server, wss, rooms };
