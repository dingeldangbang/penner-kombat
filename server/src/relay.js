'use strict';

/**
 * Penner Kombat — WebSocket-Relay (v1.1)
 * =======================================
 * Verwaltet Räume, spiegelt Nachrichten zwischen den Spielern eines Raums und
 * bietet einfaches Matchmaking an. Der Server hält bewusst KEINEN
 * Spielzustand: er kennt Räume, Teilnehmer, Ready-Flags und Matchmaking-
 * Warteschlangen — alles andere (Position, HP, Combo, Aktionen) reicht er
 * nur unverändert an die übrigen Mitglieder weiter.
 *
 * NEU in v1.1 (Mobile-Server-Integration):
 *  - `matchmaking` / `matchmaking_cancel`-Nachrichten (paart 2 Spieler
 *    gleicher Mode/Region automatisch in einen Raum)
 *  - REST-Endpunkte /health, /api/status, /api/rooms (CORS-fähig,
 *    für ELO/Statistik-UI und Monitoring)
 *  - env-basierte Konfiguration + Docker-Readiness
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
const MATCHMAKING_ENABLED = process.env.MATCHMAKING_ENABLED !== 'false';
const MATCHMAKING_TIMEOUT_MS = Number(process.env.MATCHMAKING_TIMEOUT_MS || 90 * 1000);
const VERSION = require('../package.json').version;

/** @type {Map<string, {id:string, created:number, lastActivity:number, clients:Set<object>}>} */
const rooms = new Map();
/** @type {Array<{ws:object, mode:string, region:string, tag:string, joined:number}>} */
const matchmakingQueue = [];
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

function makeRoomCode() {
  // 6 Zeichen, Großbuchstaben + Ziffern (wie Client-RoomCode)
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  let code = '';
  for (let i = 0; i < 6; i++) code += alphabet[Math.floor(Math.random() * alphabet.length)];
  return code;
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
//  Matchmaking
// ---------------------------------------------------------------------------

function removeFromQueue(ws) {
  const idx = matchmakingQueue.findIndex((q) => q.ws === ws);
  if (idx === -1) return false;
  matchmakingQueue.splice(idx, 1);
  return true;
}

function updateQueueStatus(ws, status, extra = {}) {
  send(ws, { type: 'matchmaking_update', status, mode: ws.matchmaking ? ws.matchmaking.mode : null, ...extra });
}

function handleMatchmaking(ws, msg) {
  if (!MATCHMAKING_ENABLED) {
    send(ws, { type: 'error', error: 'matchmaking_disabled' });
    return;
  }
  if (ws.room) {
    send(ws, { type: 'error', error: 'already_in_room' });
    return;
  }
  if (ws.matchmaking) {
    updateQueueStatus(ws, 'waiting');
    return;
  }

  const mode = String(msg.mode || 'ranked').slice(0, 24);
  const region = String(msg.region || 'auto').slice(0, 24);
  const tag = String(msg.tag || '').slice(0, 64);
  ws.matchmaking = { mode, region, tag, joined: Date.now() };

  // Passenden Gegner suchen (gleiche Mode/Region, nicht selbst)
  const partner = matchmakingQueue.find(
    (q) => q !== ws && q.mode === mode && q.region === region && q.ws.room === null && q.ws.readyState === q.ws.OPEN
  );

  if (partner) {
    removeFromQueue(partner.ws);
    ws.matchmaking = null;
    partner.ws.matchmaking = null;
    const roomCode = makeRoomCode();
    joinRoom(ws, roomCode, ws.playerName);
    joinRoom(partner.ws, roomCode, partner.ws.playerName);
    // "matched" ist die Bestätigung für beide – der Raum ist danach per join/joined nutzbar.
    send(ws, { type: 'matched', room: roomCode, opponent: { player: partner.ws.playerId, name: partner.ws.playerName }, at: Date.now() });
    send(partner.ws, { type: 'matched', room: roomCode, opponent: { player: ws.playerId, name: ws.playerName }, at: Date.now() });
    updateQueueStatus(ws, 'paired', { room: roomCode });
    updateQueueStatus(partner.ws, 'paired', { room: roomCode });
    log(`⚔ Matchmaking: ${ws.playerName} vs. ${partner.ws.playerName} → ${roomCode} [${mode}/${region}]`);
    return;
  }

  matchmakingQueue.push({ ws, mode, region, tag, joined: ws.matchmaking.joined });
  updateQueueStatus(ws, 'waiting', { queued: matchmakingQueue.length });
  log(`+ Matchmaking-Warteschlange: ${ws.playerName} [${mode}/${region}] (${matchmakingQueue.length})`);
}

// ---------------------------------------------------------------------------
//  HTTP: Health, Status, Räume (CORS für ELO-UI/Dashboards)
// ---------------------------------------------------------------------------

function corsHeaders() {
  return {
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Methods': 'GET, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
    'Cache-Control': 'no-store',
  };
}

function sendJson(res, code, obj, headers = {}) {
  res.writeHead(code, { 'Content-Type': 'application/json; charset=utf-8', ...corsHeaders(), ...headers });
  res.end(JSON.stringify(obj));
}

const server = http.createServer((req, res) => {
  const url = new URL(req.url, `http://${req.headers.host || 'localhost'}`);

  if (req.method === 'OPTIONS') {
    res.writeHead(204, corsHeaders());
    res.end();
    return;
  }

  if (req.method !== 'GET') {
    sendJson(res, 405, { ok: false, error: 'method_not_allowed' });
    return;
  }

  if (url.pathname === '/health') {
    sendJson(res, 200, {
      ok: true,
      version: VERSION,
      rooms: rooms.size,
      clients: wss.clients.size,
      matchmaking: MATCHMAKING_ENABLED ? matchmakingQueue.length : -1,
      uptime: process.uptime(),
    });
    return;
  }

  if (url.pathname === '/api/status') {
    sendJson(res, 200, {
      ok: true,
      version: VERSION,
      uptime: process.uptime(),
      rooms: rooms.size,
      clients: wss.clients.size,
      maxPlayersPerRoom: MAX_PLAYERS,
      matchmaking: { enabled: MATCHMAKING_ENABLED, queued: matchmakingQueue.length, timeoutMs: MATCHMAKING_TIMEOUT_MS },
      wsPath: PATH,
      port: PORT,
    });
    return;
  }

  if (url.pathname === '/rooms' || url.pathname === '/api/rooms') {
    const list = [...rooms.values()].map((r) => ({
      room: r.id,
      players: r.clients.size,
      maxPlayers: MAX_PLAYERS,
      created: r.created,
      lastActivity: r.lastActivity,
      ready: [...r.clients].filter((c) => c.ready).length,
    }));
    sendJson(res, 200, list);
    return;
  }

  res.writeHead(200, { 'Content-Type': 'text/plain; charset=utf-8', ...corsHeaders() });
  res.end('Penner Kombat Relay laeuft. WebSocket: ' + PATH + '?room=XXXXXX · REST: /api/status\n');
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
  ws.matchmaking = null;

  // Raum darf schon in der URL stehen: ws://host/kombat?room=AB12CD
  let urlRoom = null;
  try {
    const parsed = new URL(req.url, `http://${req.headers.host}`);
    urlRoom = parsed.searchParams.get('room');
  } catch (_) { /* ignorieren */ }

  send(ws, { type: 'welcome', player: ws.playerId, maxPlayers: MAX_PLAYERS, version: VERSION });
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

  ws.on('close', () => {
    removeFromQueue(ws);
    leaveRoom(ws, 'close');
  });
  ws.on('error', () => {
    removeFromQueue(ws);
    leaveRoom(ws, 'error');
  });
});

function joinRoom(ws, roomId, playerName) {
  if (!roomId) {
    send(ws, { type: 'error', error: 'missing_room' });
    return;
  }
  if (ws.room && ws.room.id === roomId) return;
  if (ws.room) leaveRoom(ws, 'switch');
  // Wer einem Raum beitritt, verlässt die Matchmaking-Warteschlange.
  removeFromQueue(ws);

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
      removeFromQueue(ws);
      leaveRoom(ws, 'message');
      return;

    case 'matchmaking':
      handleMatchmaking(ws, msg);
      return;

    case 'matchmaking_cancel':
      if (removeFromQueue(ws)) {
        ws.matchmaking = null;
        updateQueueStatus(ws, 'canceled');
      }
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
      removeFromQueue(ws);
      leaveRoom(ws, 'timeout');
      ws.terminate();
      continue;
    }
    ws.isAlive = false;
    ws.ping();
  }

  const now = Date.now();
  for (const [id, room] of rooms) {
    if (room.clients.size === 0) {
      rooms.delete(id);
      log(`room - ${id} (aufgeräumt)`);
    }
  }

  // Verdampfte Matchmaking-Einträge entfernen
  for (let i = matchmakingQueue.length - 1; i >= 0; i--) {
    const entry = matchmakingQueue[i];
    if (now - entry.joined > MATCHMAKING_TIMEOUT_MS) {
      const ws = entry.ws;
      matchmakingQueue.splice(i, 1);
      if (ws.matchmaking) {
        ws.matchmaking = null;
        updateQueueStatus(ws, 'timeout');
      }
    }
  }
}, HEARTBEAT_MS);

wss.on('close', () => clearInterval(heartbeat));

server.listen(PORT, HOST, () => {
  log(`Penner Kombat Relay v${VERSION}: ws://${HOST}:${PORT}${PATH}  (max ${MAX_PLAYERS} Spieler/Raum, Matchmaking ${MATCHMAKING_ENABLED ? 'an' : 'aus'})`);
  log(`REST: http://${HOST}:${PORT}/api/status`);
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

module.exports = { server, wss, rooms, matchmakingQueue };
