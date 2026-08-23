/**
 * ui.dom.test.mjs — Oberflächen-Abnahme der KI-Werkstatt in jsdom
 *
 * Lädt `lab.html` in ein echtes DOM, führt `src/lab/main.js` aus (three.js über
 * das Test-Doppel ohne WebGL) und klickt die Oberfläche durch: Kampfprofile,
 * Chat, Move-Liste, Tastatur, Auflösungsumschaltung, Export.
 *
 * Start (Hooks nötig):
 *   node --import ./test/helpers/register.mjs --test test/ui.dom.test.mjs
 *   npm run test:ui
 *
 * Ohne installiertes jsdom werden die Tests übersprungen.
 */

import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const dir = path.dirname(fileURLToPath(import.meta.url));
const WEB = path.join(dir, '..');

let JSDOM = null;
try { ({ JSDOM } = await import('jsdom')); } catch { /* optional */ }
const maybe = { skip: !JSDOM ? 'jsdom nicht installiert (npm install)' : false };

/** Minimaler 2D-Kontext, damit die Partikel-Textur in jsdom gebaut werden kann. */
function patchCanvas(win) {
  const ctx2d = {
    createRadialGradient: () => ({ addColorStop() {} }),
    fillRect() {}, clearRect() {}, drawImage() {},
    getImageData: () => ({ data: new Uint8ClampedArray(4) }),
    set fillStyle(v) {}, get fillStyle() { return '#000'; },
  };
  win.HTMLCanvasElement.prototype.getContext = function getContext(kind) {
    return kind === '2d' ? ctx2d : null;
  };
}

/** jsdom gibt jedem Fenster eigenen Speicher — für Reload-Tests teilen wir ihn. */
function makeStorage() {
  const map = new Map();
  return {
    getItem: (k) => (map.has(k) ? map.get(k) : null),
    setItem: (k, v) => map.set(k, String(v)),
    removeItem: (k) => map.delete(k),
    clear: () => map.clear(),
    key: (i) => [...map.keys()][i] ?? null,
    get length() { return map.size; },
  };
}

async function boot(sharedStorage = null) {
  const html = fs.readFileSync(path.join(WEB, 'lab.html'), 'utf8')
    .replace(/<script type="importmap">[\s\S]*?<\/script>/, '')
    .replace(/<script type="module"[\s\S]*?<\/script>/, '');

  const dom = new JSDOM(html, { url: 'http://localhost:8080/lab.html', pretendToBeVisual: true, runScripts: 'dangerously' });
  const { window } = dom;
  patchCanvas(window);

  // Globale, die main.js erwartet
  global.window = window;
  global.document = window.document;
  const storage = sharedStorage || makeStorage();
  global.localStorage = storage;
  global.HTMLInputElement = window.HTMLInputElement;
  global.HTMLTextAreaElement = window.HTMLTextAreaElement;
  global.requestAnimationFrame = () => 0;      // Loop nicht dauerhaft laufen lassen
  global.performance = { now: () => Date.now() };   // jsdom-Performance rekursiert sonst
  global.Blob = window.Blob;
  window.URL.createObjectURL = () => 'blob:stub';
  window.AudioContext = undefined;             // audioPool bleibt still

  const mod = await import('../src/lab/main.js?' + Math.random());
  return { window, doc: window.document, lab: window.PK_LAB, storage, mod };
}

function click(doc, selector) {
  const el = doc.querySelector(selector);
  assert.ok(el, 'Element fehlt: ' + selector);
  el.dispatchEvent(new el.ownerDocument.defaultView.MouseEvent('click', { bubbles: true }));
  return el;
}

test('UI: Seite bootet, 480p-Sperre steht, Loop rendert', maybe, async () => {
  const { doc, lab } = await boot();
  assert.ok(lab, 'window.PK_LAB fehlt');
  const canvas = doc.querySelector('#lab-canvas');
  assert.equal(canvas.width, 854);
  assert.equal(canvas.height, 480);
  assert.equal(canvas.style.width, '100%', 'CSS muss den Puffer strecken');
  assert.equal(doc.querySelector('#res-readout').textContent, '854×480');
  assert.equal(lab.resLock.preset, '480p');
});

test('UI: alle vier Kampfprofile sind klickbar und binden Moves', maybe, async () => {
  const { doc, lab } = await boot();
  const buttons = [...doc.querySelectorAll('[data-preset]')];
  assert.equal(buttons.length, 4);

  const expected = {
    cyber_scorpion: { moves: ['Magnet Grab', 'Nano Bolt', 'Overload'], hp: 95 },
    toxic_ghoulem: { moves: ['Acid Vomit', 'Blood Geyser', 'Brutal Carnage'], hp: 185 },
    voodoo_priest: { moves: ['Abyssal Rift', 'Shadow Step', 'Soul Reap'], hp: 88 },
    bio_mech: { moves: ['Meltdown Slam', 'Fallout Cloud', 'Heavy Core Smash'], hp: 220 },
  };

  for (const [id, exp] of Object.entries(expected)) {
    click(doc, `[data-preset="${id}"]`);
    const catalog = Object.keys(lab.fighter.moveCatalog);
    for (const m of exp.moves) assert.ok(catalog.includes(m), `${id}: ${m} fehlt im Katalog`);
    assert.equal(lab.fighter.stats.maxHP, exp.hp, id + ': HP falsch');
    assert.equal(doc.querySelectorAll('#move-list .move').length, catalog.length, id + ': Kartenanzahl ≠ Katalog');
    assert.match(doc.querySelector('#profile-label').textContent, /^Profil: \S/, id + ': Profil-Label leer');
    assert.ok(doc.querySelector('#json-view').textContent.includes(exp.moves[0]), id + ': JSON-Tab nicht aktualisiert');
    lab.fighter.reset();
  }
});

test('UI: Chat-Formular erzeugt Move und Katalogkarte', maybe, async () => {
  const { doc, lab } = await boot();
  const input = doc.querySelector('#chat-input');
  input.value = 'Gib ihm einen Feueratem mit 25 Schaden, Eingabe runter vorne schwerer Schlag';
  doc.querySelector('#chat-form').dispatchEvent(new doc.defaultView.Event('submit', { bubbles: true, cancelable: true }));
  await new Promise((r) => setTimeout(r, 50));

  const name = Object.keys(lab.fighter.moveCatalog).find((n) => /feueratem/i.test(n));
  assert.ok(name, 'Kein Move aus dem Chat entstanden');
  const move = lab.fighter.moveCatalog[name];
  assert.equal(move.damage, 25);
  assert.deepEqual(move.sequence, ['DOWN', 'FORWARD', 'HEAVY_PUNCH']);
  assert.equal(move.vfxType, 'fire_particle_stream');
  assert.ok(doc.querySelector('#move-list').textContent.includes(name));
  assert.ok(doc.querySelector('#chat-log').textContent.includes('UPDATE_ABILITIES'));
});

test('UI: Chat lädt ein komplettes Profil per Namen', maybe, async () => {
  const { doc, lab } = await boot();
  const input = doc.querySelector('#chat-input');
  input.value = 'Lade das Profil Voodoo Shadow-Priest';
  doc.querySelector('#chat-form').dispatchEvent(new doc.defaultView.Event('submit', { bubbles: true, cancelable: true }));
  await new Promise((r) => setTimeout(r, 50));

  assert.ok(lab.fighter.moveCatalog['Abyssal Rift'], 'Profil nicht geladen');
  assert.match(doc.querySelector('#profile-label').textContent, /Voodoo/);
});

test('UI: unverständlicher Prompt meldet Fehler statt Unsinn', maybe, async () => {
  const { doc, lab } = await boot();
  doc.querySelector('#chat-input').value = 'hallo wie geht es dir';
  doc.querySelector('#chat-form').dispatchEvent(new doc.defaultView.Event('submit', { bubbles: true, cancelable: true }));
  await new Promise((r) => setTimeout(r, 50));
  assert.equal(Object.keys(lab.fighter.moveCatalog).length, 0);
  assert.ok(doc.querySelector('#chat-log').textContent.includes('Konfiguration'));
});

test('UI: Touch-Buttons und Tastatur feuern denselben Move', maybe, async () => {
  const { doc, lab } = await boot();
  click(doc, '[data-preset="cyber_scorpion"]');

  // Touch-Pad: BACK, FORWARD, GRAB gibt es nicht als Button -> Tastatur nutzen
  const press = (code) => doc.defaultView.dispatchEvent(
    new doc.defaultView.KeyboardEvent('keydown', { code, bubbles: true, cancelable: true }),
  );
  press('KeyA'); press('KeyD'); press('KeyG');
  assert.equal(lab.fighter.state.move?.name, 'Magnet Grab', 'Tastenfolge A-D-G löst Magnet Grab nicht aus');

  // Buffer-Anzeige wurde mitgeführt
  assert.ok(doc.querySelector('#buffer-view').textContent.includes('GRAB'));

  // Touch-Buttons: 4x LP feuert Overload
  lab.fighter.state = { phase: 'idle', move: null, timer: 0, hitApplied: false };
  for (let i = 0; i < 4; i++) {
    const b = doc.querySelector('[data-token="LIGHT_PUNCH"]');
    b.dispatchEvent(new doc.defaultView.MouseEvent('pointerdown', { bubbles: true, cancelable: true }));
  }
  assert.equal(lab.fighter.state.move?.name, 'Overload');
});

test('UI: Move-Karte hat Test- und Löschknopf, beide wirken', maybe, async () => {
  const { doc, lab } = await boot();
  click(doc, '[data-preset="bio_mech"]');
  const before = Object.keys(lab.fighter.moveCatalog).length;

  click(doc, '[data-play="Meltdown Slam"]');
  assert.equal(lab.fighter.state.move?.name, 'Meltdown Slam', '▶ Testen startet den Move nicht');

  click(doc, '[data-del="Fallout Cloud"]');
  assert.equal(Object.keys(lab.fighter.moveCatalog).length, before - 1);
  assert.ok(!doc.querySelector('#move-list').textContent.includes('Fallout Cloud'));
});

test('UI: Auflösungsumschaltung greift auf den Puffer durch', maybe, async () => {
  const { doc, lab } = await boot();
  const sel = doc.querySelector('#opt-resolution');
  assert.deepEqual([...sel.options].map((o) => o.value), ['480p', '360p', '240p', '144p']);

  sel.value = '240p';
  sel.dispatchEvent(new doc.defaultView.Event('change', { bubbles: true }));
  assert.equal(doc.querySelector('#lab-canvas').width, 426);
  assert.equal(doc.querySelector('#res-readout').textContent, '426×240');
  assert.equal(lab.resLock.preset, '240p');

  sel.value = '480p';
  sel.dispatchEvent(new doc.defaultView.Event('change', { bubbles: true }));
  assert.equal(doc.querySelector('#lab-canvas').width, 854);
});

test('UI: Konfiguration überlebt einen Reload (localStorage)', maybe, async () => {
  const shared = makeStorage();
  const first = await boot(shared);
  click(first.doc, '[data-preset="toxic_ghoulem"]');
  assert.ok(shared.getItem('pk_lab_v1'), 'nichts gespeichert');

  const second = await boot(shared);   // simulierter Reload mit gleichem Speicher
  assert.ok(second.lab.fighter.moveCatalog['Acid Vomit'], 'Konfiguration nicht wiederhergestellt');
  assert.ok(second.doc.querySelector('#chat-log').textContent.includes('wiederhergestellt'));
});

test('UI: Reset räumt Katalog, Label und Liste auf', maybe, async () => {
  const { doc, lab } = await boot();
  click(doc, '[data-preset="cyber_scorpion"]');
  click(doc, '#btn-reset');
  assert.equal(Object.keys(lab.fighter.moveCatalog).length, 0);
  assert.equal(doc.querySelector('#profile-label').textContent, 'Profil: —');
  assert.ok(doc.querySelector('#move-list').textContent.includes('Noch keine Moves'));
});

test('UI: Asset-Bibliothek und Tabs sind gefüllt', maybe, async () => {
  const { doc } = await boot();
  assert.ok(doc.querySelectorAll('#lib-vfx li').length >= 14);
  assert.ok(doc.querySelectorAll('#lib-audio li').length >= 15);
  assert.ok(doc.querySelectorAll('#lib-hitbox li').length >= 9);
  assert.ok(doc.querySelector('#lib-manifest').textContent.includes('screen_long_box'));

  click(doc, '.tab[data-tab="lib"]');
  assert.ok(!doc.querySelector('#panel-lib').classList.contains('hidden'));
  assert.ok(doc.querySelector('#panel-moves').classList.contains('hidden'));
});

test('UI: Export der vier Profile funktioniert ohne Fehler', maybe, async () => {
  const { doc, lab } = await boot();
  click(doc, '#btn-export-presets');
  click(doc, '#btn-export');
  assert.equal(Object.keys(lab.presets).length, 4);
});
