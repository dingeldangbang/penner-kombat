#!/usr/bin/env node
/**
 * lab_browser_check.mjs — Browser-Abnahmetest der KI-Werkstatt
 *
 * Startet nichts selbst: es wird eine laufende Instanz erwartet
 * (`node server/src/ai-proxy.js`, Standard http://localhost:8080).
 *
 * Prüft im echten Chrome:
 *   1. Seite lädt ohne Konsolen-/Seitenfehler
 *   2. WebGL-Backbuffer ist exakt 854×480 (Render-Sperre greift)
 *   3. Canvas wird sichtbar gerendert (nicht schwarz/leer)
 *   4. Alle vier Kampfprofile lassen sich per Klick injizieren
 *   5. Chat-Prompt erzeugt einen Move und rendert ihn in die Liste
 *   6. Tastatur-Eingabefolge löst einen Special aus
 *   7. Auflösungsumschaltung greift auf den Puffer durch
 *
 * Voraussetzung (bewusst KEINE Repo-Abhängigkeit):
 *   npm install puppeteer      # z. B. global oder in einem Temp-Ordner
 *   node Tools/lab_browser_check.mjs [URL]
 */

const URL_BASE = process.argv[2] || process.env.LAB_URL || 'http://localhost:8080/lab.html';

let puppeteer;
try {
  puppeteer = (await import('puppeteer')).default;
} catch {
  console.error('puppeteer fehlt — bitte "npm install puppeteer" ausführen.');
  process.exit(2);
}

const results = [];
const check = (name, ok, info = '') => {
  results.push({ name, ok, info });
  console.log(`${ok ? '✅' : '❌'} ${name}${info ? ' — ' + info : ''}`);
};

const browser = await puppeteer.launch({
  headless: 'new',
  args: ['--no-sandbox', '--use-gl=swiftshader', '--enable-unsafe-swiftshader', '--disable-dev-shm-usage'],
});
const page = await browser.newPage();
await page.setViewport({ width: 1440, height: 900 });

const errors = [];
page.on('pageerror', (e) => errors.push('pageerror: ' + e.message));
page.on('console', (m) => { if (m.type() === 'error') errors.push('console: ' + m.text()); });
page.on('requestfailed', (r) => errors.push('request: ' + r.url() + ' ' + r.failure()?.errorText));

await page.goto(URL_BASE, { waitUntil: 'networkidle2', timeout: 60000 });
await page.waitForFunction(() => window.PK_LAB && window.PK_LAB.fighter, { timeout: 30000 });

// 1 — Fehlerfreiheit
check('Seite lädt ohne Fehler', errors.length === 0, errors.slice(0, 3).join(' | '));

// 2 — 480p-Sperre
const buf = await page.evaluate(() => {
  const c = document.querySelector('#lab-canvas');
  return { w: c.width, h: c.height, cssW: c.clientWidth, dpr: window.devicePixelRatio };
});
check('WebGL-Puffer exakt 854×480', buf.w === 854 && buf.h === 480, `${buf.w}×${buf.h}, CSS ${buf.cssW}px, DPR ${buf.dpr}`);

// 3 — Es wird wirklich gerendert
await new Promise((r) => setTimeout(r, 1200));
const notBlank = await page.evaluate(() => {
  const c = document.querySelector('#lab-canvas');
  const tmp = document.createElement('canvas');
  tmp.width = c.width; tmp.height = c.height;
  tmp.getContext('2d').drawImage(c, 0, 0);
  const d = tmp.getContext('2d').getImageData(0, 0, tmp.width, tmp.height).data;
  let sum = 0;
  for (let i = 0; i < d.length; i += 4 * 97) sum += d[i] + d[i + 1] + d[i + 2];
  return sum;
});
check('Canvas rendert sichtbaren Inhalt', notBlank > 0, 'Helligkeitssumme ' + notBlank);

// 4 — Profile
const presetIds = await page.$$eval('[data-preset]', (els) => els.map((e) => e.dataset.preset));
check('Vier Kampfprofile in der UI', presetIds.length === 4, presetIds.join(', '));

for (const id of presetIds) {
  await page.click(`[data-preset="${id}"]`);
  await new Promise((r) => setTimeout(r, 250));
  const state = await page.evaluate(() => ({
    profile: document.querySelector('#profile-label').textContent,
    moves: Object.keys(window.PK_LAB.fighter.moveCatalog).length,
    cards: document.querySelectorAll('#move-list .move').length,
    hp: window.PK_LAB.fighter.stats.maxHP,
  }));
  check(`Profil ${id} injiziert`, state.moves >= 3 && state.cards === state.moves,
    `${state.profile}, ${state.moves} Moves, ${state.hp} HP`);
}

// 5 — Chat
await page.click('#chat-input');
await page.type('#chat-input', 'Gib ihm einen Feueratem mit 25 Schaden, Eingabe runter vorne schwerer Schlag');
await page.click('#chat-form button[type="submit"]');
await page.waitForFunction(() => Object.keys(window.PK_LAB.fighter.moveCatalog).some((n) => /feueratem/i.test(n)), { timeout: 15000 });
const chatMove = await page.evaluate(() => {
  const k = Object.keys(window.PK_LAB.fighter.moveCatalog).find((n) => /feueratem/i.test(n));
  const m = window.PK_LAB.fighter.moveCatalog[k];
  return { k, dmg: m.damage, seq: m.sequence.join(','), vfx: m.vfxType };
});
check('Chat-Prompt erzeugt Move', chatMove.dmg === 25 && chatMove.seq === 'DOWN,FORWARD,HEAVY_PUNCH',
  `${chatMove.k}: ${chatMove.dmg} DMG, ${chatMove.seq}, ${chatMove.vfx}`);

// 6 — Tastatureingabe löst aus
await page.evaluate(() => document.querySelector('#chat-input').blur());
await page.keyboard.press('KeyS');
await page.keyboard.press('KeyD');
await page.keyboard.press('KeyK');
await new Promise((r) => setTimeout(r, 150));
const fired = await page.evaluate(() => ({
  phase: window.PK_LAB.fighter.state.phase,
  move: window.PK_LAB.fighter.state.move?.name || null,
  label: document.querySelector('#state-label').textContent,
}));
check('Tastenfolge S-D-K feuert den Special', fired.move !== null, `${fired.move} (${fired.phase})`);

// 7 — Auflösungsumschaltung
await page.select('#opt-resolution', '240p');
await new Promise((r) => setTimeout(r, 300));
const small = await page.evaluate(() => {
  const c = document.querySelector('#lab-canvas');
  return { w: c.width, h: c.height, readout: document.querySelector('#res-readout').textContent };
});
check('Umschalten auf 240p greift durch', small.w === 426 && small.h === 240, `${small.w}×${small.h} (${small.readout})`);

await page.select('#opt-resolution', '480p');
await new Promise((r) => setTimeout(r, 300));

await page.screenshot({ path: process.env.LAB_SHOT || '/tmp/lab-check.png' });

const failed = results.filter((r) => !r.ok);
console.log(`\n${results.length - failed.length}/${results.length} Prüfungen bestanden`);
if (errors.length) console.log('Konsolenmeldungen:\n' + errors.slice(0, 10).join('\n'));
await browser.close();
process.exit(failed.length ? 1 : 0);
