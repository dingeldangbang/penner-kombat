/**
 * lab.test.mjs — Tests der KI-Konfigurationspipeline
 * Läuft ohne Browser: getestet werden Parser, Schema, 480p-Klemmung, Sequenzen.
 */

import test from 'node:test';
import assert from 'node:assert/strict';

import { parseLocal, buildSystemPrompt, AIEngine } from '../src/lab/aiEngine.js';
import { validateConfig, normalizeSequence, extractJson } from '../src/lab/schema.js';
import { clampTo480p, MAX_PIXELS, RESOLUTION_PRESETS } from '../src/lab/res480.js';
import { isSequenceMatch, bestMatch, InputBuffer } from '../src/lab/inputBuffer.js';
import { manifest, resolveAsset, VFX_LIBRARY } from '../src/lab/assetLibrary.js';

// --- 480p-Sperre ------------------------------------------------------------

test('480p: Preset ist 854x480', () => {
  assert.deepEqual(RESOLUTION_PRESETS['480p'], { width: 854, height: 480 });
});

test('480p: 4K wird auf maximal 480p-Pixelbudget geklemmt', () => {
  const r = clampTo480p(3840, 2160);
  assert.ok(r.width * r.height <= MAX_PIXELS, 'Pixelbudget überschritten');
  assert.ok(Math.abs(r.width / r.height - 16 / 9) < 0.02, 'Seitenverhältnis verloren');
});

test('480p: kleinere Auflösungen bleiben unangetastet', () => {
  assert.deepEqual(clampTo480p(320, 180), { width: 320, height: 180 });
});

// --- Sequenzen --------------------------------------------------------------

test('Sequenz: Numpad-Notation 236HP', () => {
  assert.deepEqual(normalizeSequence('236HP'), ['DOWN', 'FORWARD', 'HEAVY_PUNCH']);
});

test('Sequenz: deutsche Wörter', () => {
  assert.deepEqual(normalizeSequence(['runter', 'vorne', 'schwerer schlag']), ['DOWN', 'FORWARD', 'HEAVY_PUNCH']);
});

test('Sequenz: Müll wird verworfen', () => {
  assert.deepEqual(normalizeSequence(['banane', 'LIGHT_PUNCH']), ['LIGHT_PUNCH']);
});

test('Matcher: längste passende Sequenz gewinnt', () => {
  const catalog = {
    Jab: { sequence: ['LIGHT_PUNCH'] },
    Hadouken: { sequence: ['DOWN', 'FORWARD', 'LIGHT_PUNCH'] },
  };
  const hit = bestMatch(['BACK', 'DOWN', 'FORWARD', 'LIGHT_PUNCH'], catalog);
  assert.equal(hit.name, 'Hadouken');
});

test('Matcher: isSequenceMatch prüft nur das Pufferende', () => {
  assert.ok(isSequenceMatch(['UP', 'DOWN', 'FORWARD', 'HEAVY_PUNCH'], ['DOWN', 'FORWARD', 'HEAVY_PUNCH']));
  assert.ok(!isSequenceMatch(['DOWN', 'FORWARD', 'HEAVY_PUNCH', 'UP'], ['DOWN', 'FORWARD', 'HEAVY_PUNCH']));
});

test('InputBuffer: Eingaben ausserhalb des Zeitfensters zählen nicht', () => {
  const b = new InputBuffer(8, 100);
  b.push('DOWN', 0);
  b.push('FORWARD', 50);
  b.push('HEAVY_PUNCH', 90);
  assert.ok(b.matches(['DOWN', 'FORWARD', 'HEAVY_PUNCH'], 120));
  assert.ok(!b.matches(['DOWN', 'FORWARD', 'HEAVY_PUNCH'], 5000));
});

// --- Lokaler Parser ---------------------------------------------------------

test('Parser: Feueratem-Beispiel aus der Architektur', () => {
  const raw = parseLocal('Gib diesem Charakter einen Feueratem-Spezialangriff und weise ihm eine 3-Treffer-Schlagkombination zu');
  const { ok, config } = validateConfig(raw);
  assert.ok(ok);
  assert.equal(config.specialAttacks.length, 1);
  assert.equal(config.specialAttacks[0].vfxAsset, 'fire_particle_stream');
  assert.equal(config.combos.length, 1);
  assert.equal(config.combos[0].inputSequence.length, 3);
  assert.deepEqual(config.combos[0].inputSequence, ['LIGHT_PUNCH', 'LIGHT_PUNCH', 'LIGHT_PUNCH']);
});

test('Parser: Schaden und Eingabefolge werden übernommen', () => {
  const { config } = validateConfig(parseLocal('Feuerball mit 30 Schaden, Eingabe runter vorne schwerer Schlag'));
  const s = config.specialAttacks[0];
  assert.equal(s.damage, 30);
  assert.deepEqual(s.inputSequence, ['DOWN', 'FORWARD', 'HEAVY_PUNCH']);
  assert.equal(s.projectile, true);
});

test('Parser: Kick-Kombo mit Trefferzahl', () => {
  const { config } = validateConfig(parseLocal('Schwere Kick-Kombo mit 4 Treffern und 22 Schaden'));
  assert.equal(config.combos[0].inputSequence.length, 4);
  assert.equal(config.combos[0].inputSequence[0], 'HEAVY_KICK');
  assert.equal(config.combos[0].totalDamage, 22);
});

test('Parser: Optik und Werte', () => {
  const { config } = validateConfig(parseLocal('Mach ihn rot und lass ihn leuchten, setz die Lebenspunkte auf 150'));
  assert.equal(config.visualOverrides.tintColor, 0xcc2222);
  assert.ok(config.visualOverrides.emissiveIntensity > 0);
  assert.equal(config.stats.maxHP, 150);
});

test('Parser: englische Eingabe funktioniert ebenfalls', () => {
  const { config } = validateConfig(parseLocal('give him an ice projectile special with 18 damage'));
  assert.equal(config.specialAttacks[0].vfxAsset, 'ice_shards');
  assert.equal(config.specialAttacks[0].damage, 18);
});

test('Parser: Entfernen erkennt bekannte Moves', () => {
  const raw = parseLocal('Entferne den Feueratem', { knownMoves: ['Feueratem', 'Triple Jab'] });
  assert.deepEqual(raw.removeMoves, ['Feueratem']);
  assert.equal(raw.requestType, 'REMOVE_MOVE');
});

test('Parser: unverständliche Eingabe erzeugt NOOP statt Unsinn', () => {
  const res = validateConfig(parseLocal('hallo wie geht es dir'));
  assert.equal(res.ok, false);
  assert.equal(res.config.requestType, 'NOOP');
});

// --- Schema-Härtung ---------------------------------------------------------

test('Schema: Schaden wird auf Balance-Grenzen geklemmt', () => {
  const { config } = validateConfig({
    requestType: 'UPDATE_ABILITIES',
    specialAttacks: [{ name: 'Insta-Kill', damage: 99999, startupFrames: -5, activeFrames: 5000, inputSequence: ['HEAVY_PUNCH'] }],
  });
  assert.equal(config.specialAttacks[0].damage, 60);
  assert.equal(config.specialAttacks[0].startupFrames, 1);
  assert.equal(config.specialAttacks[0].activeFrames, 90);
});

test('Schema: erfundene Assetnamen werden gemappt oder ersetzt', () => {
  const { config, warnings } = validateConfig({
    specialAttacks: [{ name: 'X', vfxAsset: 'super_mega_explosion_9000', inputSequence: ['HEAVY_PUNCH'] }],
  });
  assert.ok(Object.keys(VFX_LIBRARY).includes(config.specialAttacks[0].vfxAsset));
  assert.ok(warnings.length >= 0);
});

test('Schema: "fire" wird zu fire_blast aufgelöst', () => {
  assert.equal(resolveAsset('vfx', 'fire'), 'fire_blast');
  assert.equal(resolveAsset('vfx', 'Fire Blast'), 'fire_blast');
});

test('Schema: kaputtes JSON wird sauber abgelehnt', () => {
  const res = validateConfig('{ das ist kein json ');
  assert.equal(res.ok, false);
  assert.ok(res.errors[0].includes('Kein gültiges JSON'));
});

test('Schema: JSON aus Markdown-Fences wird extrahiert', () => {
  const txt = 'Klar!\n```json\n{"requestType":"UPDATE_STATS","stats":{"maxHP":200}}\n```\nViel Spaß.';
  const res = validateConfig(extractJson(txt));
  assert.equal(res.config.stats.maxHP, 200);
});

test('Schema: keine Code-Ausführung — unbekannte Felder fallen raus', () => {
  const { config } = validateConfig({
    stats: { maxHP: 120 },
    onUpdate: 'while(true){}',
    __proto__: { hacked: true },
    script: '<script>alert(1)</script>',
  });
  assert.equal(config.onUpdate, undefined);
  assert.equal(config.script, undefined);
  assert.equal(config.stats.maxHP, 120);
});

// --- Engine & Manifest ------------------------------------------------------

test('AIEngine: lokaler Modus liefert validierte Konfiguration', async () => {
  const ai = new AIEngine({ mode: 'local' });
  const res = await ai.request('Blitzschlag Special mit 30 Schaden');
  assert.equal(res.ok, true);
  assert.equal(res.source, 'local');
  assert.equal(res.config.specialAttacks[0].vfxAsset, 'electricity');
});

test('AIEngine: Remote-Ausfall fällt auf den lokalen Parser zurück', async () => {
  const original = globalThis.fetch;
  globalThis.fetch = async () => { throw new Error('offline'); };
  try {
    const ai = new AIEngine({ mode: 'remote', endpoint: 'http://127.0.0.1:1/api' });
    const res = await ai.request('Feuerball mit 20 Schaden');
    assert.equal(res.source, 'local-fallback');
    assert.equal(res.config.specialAttacks[0].damage, 20);
  } finally { globalThis.fetch = original; }
});

test('AIEngine: Remote-Antwort wird geparst und validiert', async () => {
  const original = globalThis.fetch;
  globalThis.fetch = async () => ({
    ok: true,
    json: async () => ({ choices: [{ message: { content: '{"requestType":"UPDATE_STATS","stats":{"maxHP":9999}}' } }] }),
  });
  try {
    const ai = new AIEngine({ mode: 'remote' });
    const res = await ai.request('mach ihn unsterblich');
    assert.equal(res.source, 'remote');
    assert.equal(res.config.stats.maxHP, 400, 'Grenze greift auch bei Remote-Antworten');
  } finally { globalThis.fetch = original; }
});

test('Manifest: Systemprompt enthält nur existierende Assets', () => {
  const m = manifest();
  const prompt = buildSystemPrompt({ characterName: 'Le Binde' });
  assert.ok(prompt.includes('fire_particle_stream'));
  assert.ok(prompt.includes('Le Binde'));
  assert.ok(m.vfxAssets.length >= 8 && m.inputTokens.includes('HEAVY_KICK'));
});
