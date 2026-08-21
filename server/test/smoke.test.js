'use strict';

/**
 * Smoke-Test für das Relay: startet den Server, verbindet zwei Clients,
 * prüft Raumbeitritt, Spiegelung, Ready-Start und Verlassen.
 *
 *   cd server && npm install && npm test
 */

const assert = require('assert');
const { spawn } = require('child_process');
const path = require('path');
const WebSocket = require('ws');

const PORT = 5099;
const URL = `ws://127.0.0.1:${PORT}/kombat`;

let failures = 0;
const check = (name, condition) => {
  if (condition) {
    console.log(`  ✅ ${name}`);
  } else {
    console.error(`  ❌ ${name}`);
    failures++;
  }
};

function open(url) {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(url);
    ws.messages = [];
    ws.on('message', (d) => ws.messages.push(JSON.parse(d.toString())));
    ws.on('open', () => resolve(ws));
    ws.on('error', reject);
  });
}

const wait = (ms) => new Promise((r) => setTimeout(r, ms));

async function waitFor(ws, type, timeout = 2000, predicate = null) {
  const deadline = Date.now() + timeout;
  while (Date.now() < deadline) {
    const found = ws.messages.find((m) => m.type === type && (!predicate || predicate(m)));
    if (found) return found;
    await wait(25);
  }
  return null;
}

(async () => {
  console.log('Starte Relay auf Port', PORT);
  const server = spawn(process.execPath, [path.join(__dirname, '..', 'src', 'relay.js')], {
    env: { ...process.env, PORT: String(PORT) },
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  server.stdout.on('data', (d) => process.stdout.write('  [server] ' + d));
  server.stderr.on('data', (d) => process.stderr.write('  [server] ' + d));
  await wait(600);

  try {
    // --- 1. Zwei Clients, gleicher Raum ---
    const a = await open(URL);
    const b = await open(URL);

    check('Client A erhält welcome', !!(await waitFor(a, 'welcome')));

    a.send(JSON.stringify({ type: 'join', room: 'ab12cd', player: 'Le Binde' }));
    const joinedA = await waitFor(a, 'joined');
    check('A tritt Raum bei', joinedA && joinedA.room === 'AB12CD');
    check('A ist Host', joinedA && joinedA.host === true);

    b.send(JSON.stringify({ type: 'join', room: 'AB12CD', player: 'Mell' }));
    const joinedB = await waitFor(b, 'joined');
    check('B tritt demselben Raum bei', joinedB && joinedB.room === 'AB12CD');
    check('B ist nicht Host', joinedB && joinedB.host === false);
    check('A wird über B informiert', !!(await waitFor(a, 'peer_joined')));

    // room_state kommt mehrfach (Beitritt A, dann B) — auf den mit 2 Spielern warten
    const state = await waitFor(a, 'room_state', 2000, (m) => m.players.length === 2);
    check('room_state listet 2 Spieler', !!state);
    check('room_state markiert genau einen Host', state && state.players.filter((p) => p.host).length === 1);

    // --- 2. Zustands-Spiegelung ---
    b.messages.length = 0;
    a.send(JSON.stringify({ type: 'update', pos: [1, 0, 2], hp: 88.5, combo: 3 }));
    const update = await waitFor(b, 'update');
    check('B empfängt A-Update', !!update && update.hp === 88.5);
    check('Update trägt Absender-ID', !!update && typeof update.player === 'string');

    a.messages.length = 0;
    b.send(JSON.stringify({ type: 'action', action: 'special1' }));
    const action = await waitFor(a, 'action');
    check('A empfängt B-Aktion', !!action && action.action === 'special1');

    // --- 3. Kein Echo an den Absender ---
    b.messages.length = 0;
    b.send(JSON.stringify({ type: 'update', pos: [0, 0, 0], hp: 100, combo: 0 }));
    await wait(200);
    check('Absender bekommt kein Echo', !b.messages.some((m) => m.type === 'update'));

    // --- 4. Ready → Match-Start ---
    a.messages.length = 0;
    b.messages.length = 0;
    a.send(JSON.stringify({ type: 'ready', ready: true }));
    b.send(JSON.stringify({ type: 'ready', ready: true }));
    const start = await waitFor(a, 'start');
    check('Beide bereit → start', !!start);
    check('B erhält start ebenfalls', !!(await waitFor(b, 'start')));

    // --- 5. Verlassen ---
    a.messages.length = 0;
    b.messages.length = 0;
    a.close();
    check('B erfährt vom Verlassen', !!(await waitFor(b, 'peer_left')));

    // --- 6. Fehlerfälle ---
    const c = await open(URL);
    c.send(JSON.stringify({ type: 'update', hp: 1 }));
    const err = await waitFor(c, 'error');
    check('Update ohne Raum → error', err && err.error === 'not_in_room');

    c.send('kein json');
    const err2 = await waitFor(c, 'error');
    check('Ungültiges JSON → error', !!err2);

    // --- 7. Raum aus der URL ---
    const d = await open(`${URL}?room=ZZ99YY`);
    const joinedD = await waitFor(d, 'joined');
    check('Raum aus Query-String', joinedD && joinedD.room === 'ZZ99YY');

    b.close(); c.close(); d.close();
    await wait(200);
  } catch (e) {
    console.error('  ❌ Ausnahme:', e.message);
    failures++;
  } finally {
    server.kill('SIGTERM');
    await wait(300);
    if (!server.killed) server.kill('SIGKILL');
  }

  console.log(failures === 0 ? '\nAlle Tests bestanden.' : `\n${failures} Test(s) fehlgeschlagen.`);
  process.exit(failures === 0 ? 0 : 1);
})();
