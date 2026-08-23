/**
 * main.js — KI-Werkstatt (AI Fighter Lab)
 *
 * Verdrahtet die komplette Pipeline aus docs/AI_LAB.md:
 *
 *   [ .glb Upload ] -> [ Three.js Canvas, hart auf 480p gesperrt ]
 *                                 |
 *   [ Chat-Eingabe ] -> [ AIEngine ] -> [ validiertes JSON ] -> [ Game Loop ]
 */

import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { installResolutionLock, RESOLUTION_PRESETS } from './res480.js';
import { ParticleSystem } from './vfx.js';
import { AIConfigurableFighter } from './fighter.js';
import { AIEngine } from './aiEngine.js';
import { manifest, VFX_LIBRARY, AUDIO_LIBRARY, HITBOX_LIBRARY } from './assetLibrary.js';
import { buildPlaceholderFighter, buildDummy } from './placeholder.js';
import { PRESET_LIST, presetConfigs } from './presets.js';
import { CinematicDirector } from './cinematic.js';
import { RagdollSystem } from './ragdoll.js';
import { StageProps } from './stage.js';
import * as AudioPool from './audioPool.js';

const $ = (s) => document.querySelector(s);
const STORE_KEY = 'pk_lab_v1';

// ---------------------------------------------------------------------------
//  Szene
// ---------------------------------------------------------------------------

const canvas = $('#lab-canvas');

// WebGL-Check, bevor Three.js einen kryptischen Fehler wirft
if (window.PK_APP && !window.PK_APP.checkWebGL()) {
  window.PK_APP.fatal(
    'Kein WebGL verfügbar',
    'Dein Browser oder Gerät stellt keinen WebGL-Kontext bereit. Aktiviere die Hardwarebeschleunigung ' +
    'oder probiere einen aktuellen Chrome/Firefox/Safari. Die 2D-Arena läuft auch ohne WebGL.',
  );
  throw new Error('WebGL nicht verfügbar');
}

let renderer;
try {
  renderer = new THREE.WebGLRenderer({ canvas, antialias: false, powerPreference: 'high-performance' });
} catch (err) {
  if (window.PK_APP) window.PK_APP.fatal('Renderer konnte nicht starten', String(err && err.message || err));
  throw err;
}
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.BasicShadowMap;

const scene = new THREE.Scene();
scene.background = new THREE.Color(0x141622);
scene.fog = new THREE.Fog(0x141622, 12, 34);

const camera = new THREE.PerspectiveCamera(52, 854 / 480, 0.1, 100);
camera.position.set(3.4, 2.3, 5.6);
camera.lookAt(0, 1.1, 0);

const resLock = installResolutionLock(renderer, canvas, camera, '480p');

// Licht
scene.add(new THREE.HemisphereLight(0x8899ff, 0x241a12, 0.55));
const key = new THREE.DirectionalLight(0xfff0d0, 1.5);
key.position.set(4, 8, 5);
key.castShadow = true;
key.shadow.mapSize.set(512, 512);
scene.add(key);
const rim = new THREE.PointLight(0x00bfff, 24, 20);
rim.position.set(-4, 3, -3);
scene.add(rim);
const warm = new THREE.PointLight(0xff6b00, 18, 18);
warm.position.set(4, 2.5, -2);
scene.add(warm);

// Boden + Kulisse
const ground = new THREE.Mesh(
  new THREE.CircleGeometry(11, 40),
  new THREE.MeshStandardMaterial({ color: 0x2a2c3a, roughness: 1 }),
);
ground.rotation.x = -Math.PI / 2;
ground.receiveShadow = true;
scene.add(ground);
const grid = new THREE.GridHelper(22, 22, 0x556, 0x333947);
grid.position.y = 0.01;
scene.add(grid);
for (let i = 0; i < 10; i++) {
  const w = new THREE.Mesh(
    new THREE.BoxGeometry(1.6, 2.6 + Math.random() * 3, 1.6),
    new THREE.MeshStandardMaterial({ color: 0x1d2030, roughness: 1 }),
  );
  const a = (i / 10) * Math.PI * 2;
  w.position.set(Math.cos(a) * 13, 1.3, Math.sin(a) * 13);
  scene.add(w);
}

const particles = new ParticleSystem(scene);

// Kämpfer + Trainingsdummy
const dummy = buildDummy();
dummy.position.set(2.6, 0, 0);
scene.add(dummy);
let dummyHP = 100;

const director = new CinematicDirector(camera, { position: camera.position.clone(), lookAt: new THREE.Vector3(1.2, 1.05, 0) });
const ragdoll = new RagdollSystem(scene, particles);
const stage = new StageProps(scene, particles);

let fighterRoot = buildPlaceholderFighter();
scene.add(fighterRoot);
let fighter = new AIConfigurableFighter(fighterRoot, { scene, particles });
wireFighter();

// ---------------------------------------------------------------------------
//  KI-Engine
// ---------------------------------------------------------------------------

stage.on('hit', ({ object, damage, sfx }) => {
  dummyHP = Math.max(0, dummyHP - damage);
  $('#dummy-hp').style.width = dummyHP + '%';
  $('#dummy-hp-label').textContent = `Dummy ${dummyHP} HP`;
  flash(`${object} · ${damage} DMG`);
  AudioPool.play(sfx || 'impact_metal');
  director.impact({ frames: 4, strength: 1.0, zoom: 0.25 });
  fighter.addMeter(8);
});

const settings = loadSettings();
const ai = new AIEngine(settings.ai);

function loadSettings() {
  try {
    const raw = JSON.parse(localStorage.getItem(STORE_KEY) || '{}');
    return {
      ai: { mode: 'local', endpoint: '/api/ai/config', model: 'gpt-4o-mini', apiKey: '', ...(raw.ai || {}) },
      resolution: raw.resolution || '480p',
      hitboxes: !!raw.hitboxes,
      config: raw.config || null,
    };
  } catch { return { ai: { mode: 'local', endpoint: '/api/ai/config', model: 'gpt-4o-mini', apiKey: '' }, resolution: '480p', hitboxes: false, config: null }; }
}
function saveSettings() {
  localStorage.setItem(STORE_KEY, JSON.stringify({
    ai: { mode: ai.mode, endpoint: ai.endpoint, model: ai.model, apiKey: ai.apiKey },
    resolution: resLock.preset,
    hitboxes: fighter.showHitboxes,
    config: fighter.exportConfiguration(),
  }));
}

// ---------------------------------------------------------------------------
//  Chat
// ---------------------------------------------------------------------------

const chatLog = $('#chat-log');
const jsonView = $('#json-view');

function addMsg(role, html, cls = '') {
  const el = document.createElement('div');
  el.className = `msg ${role} ${cls}`.trim();
  el.innerHTML = html;
  chatLog.appendChild(el);
  chatLog.scrollTop = chatLog.scrollHeight;
  return el;
}

const esc = (s) => String(s).replace(/[&<>]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c]));

async function sendPrompt(text) {
  if (!text.trim()) return;
  addMsg('user', esc(text));
  const pending = addMsg('ai', '<span class="dots">KI denkt…</span>');
  const ctx = { characterName: currentModelName, knownMoves: Object.keys(fighter.moveCatalog) };

  let res;
  try {
    res = await ai.request(text, ctx);
  } catch (e) {
    pending.className = 'msg ai error';
    pending.innerHTML = 'Fehler: ' + esc(e.message);
    return;
  }

  if (!res.ok || !res.config) {
    pending.className = 'msg ai error';
    pending.innerHTML = [
      '⚠️ Konnte daraus keine Konfiguration bauen.',
      ...(res.errors || []).map(esc),
      ...(res.warnings || []).map(esc),
      '<small>Beispiel: „Gib ihm einen Feueratem mit 25 Schaden, Eingabe runter vorne schwerer Schlag“</small>',
    ].join('<br>');
    jsonView.textContent = JSON.stringify(res.raw ?? {}, null, 2);
    return;
  }

  const changes = fighter.applyAiConfiguration(res.config);
  jsonView.textContent = JSON.stringify(res.config, null, 2);
  if (res.config.profileName) $('#profile-label').textContent = 'Profil: ' + res.config.profileName;
  pending.className = 'msg ai';
  pending.innerHTML = [
    `<b>${res.config.requestType}</b> <span class="src">via ${res.source}</span>`,
    ...changes.map((c) => '✅ ' + esc(c)),
    ...(res.warnings || []).map((w) => '<span class="warn">⚠️ ' + esc(w) + '</span>'),
    res.config.notes ? `<small>${esc(res.config.notes)}</small>` : '',
  ].filter(Boolean).join('<br>');
  renderCatalog();
  saveSettings();
}

$('#chat-form').addEventListener('submit', (e) => {
  e.preventDefault();
  const inp = $('#chat-input');
  const v = inp.value;
  inp.value = '';
  AudioPool.initAudio(); AudioPool.resumeAudio();
  sendPrompt(v);
});

document.querySelectorAll('.chip').forEach((b) => {
  b.addEventListener('click', () => { $('#chat-input').value = b.dataset.prompt; $('#chat-input').focus(); });
});

// ---------------------------------------------------------------------------
//  Move-Katalog-Panel
// ---------------------------------------------------------------------------

function renderCatalog() {
  const list = $('#move-list');
  const entries = Object.entries(fighter.moveCatalog);
  $('#move-count').textContent = entries.length;
  if (!entries.length) {
    list.innerHTML = '<p class="empty">Noch keine Moves. Beschreib im Chat, was der Charakter können soll.</p>';
    renderHardcore();
    return;
  }
  list.innerHTML = entries.map(([name, d]) => `
    <div class="move">
      <div class="move-head">
        <b>${esc(name)}</b>
        <span class="tag ${d.kind}">${d.kind === 'special' ? 'SPECIAL' : 'KOMBO'}</span>
      </div>
      <div class="seq">${d.sequence.map((s) => `<kbd>${esc(s)}</kbd>`).join('<span>→</span>')}</div>
      <div class="meta">${d.damage} DMG · ${d.startup}f Startup · ${d.active}f Active${d.vfxType ? ' · ' + esc(d.vfxType) : ''}${d.status && d.status !== 'none' ? ' · ' + esc(d.status) : ''}</div>
      <div class="move-actions">
        <button data-play="${esc(name)}">▶ Testen</button>
        <button data-del="${esc(name)}" class="del">Entfernen</button>
      </div>
    </div>`).join('');
  list.querySelectorAll('[data-play]').forEach((b) => b.addEventListener('click', () => {
    AudioPool.initAudio(); AudioPool.resumeAudio();
    const name = b.dataset.play;
    fighter.executeSpecialMove(name, fighter.moveCatalog[name]);
  }));
  list.querySelectorAll('[data-del]').forEach((b) => b.addEventListener('click', () => {
    delete fighter.moveCatalog[b.dataset.del];
    renderCatalog(); saveSettings();
  }));
  renderHardcore();
}

/** X-Ray-, Fatality- und Arena-Panel. */
function renderHardcore() {
  const xr = Object.entries(fighter.cinematics);
  $('#xray-list').innerHTML = xr.length ? xr.map(([n, d]) => `
    <div class="move xray">
      <div class="move-head"><b>${esc(n)}</b><span class="tag cinematic">X-RAY</span></div>
      <div class="seq">${d.inputSequence.map((s2) => `<kbd>${esc(s2)}</kbd>`).join('<span>→</span>')}</div>
      <div class="meta">${d.damage} DMG · ${esc(d.boneTarget)} · Slow-Mo ${d.slowMotionFactor}× · Zoom bei Frame ${d.cinematicZoomFrame} · ${esc(d.triggerCondition)}</div>
      <div class="move-actions"><button data-xray="${esc(n)}">▶ Testen</button></div>
    </div>`).join('') : '<p class="empty">Kein X-Ray. Frag die KI: „Gib ihm einen X-Ray auf die Wirbelsäule“.</p>';

  const ft = Object.entries(fighter.fatalities);
  $('#fatality-list').innerHTML = ft.length ? ft.map(([n, d]) => `
    <div class="move fatality">
      <div class="move-head"><b>${esc(n)}</b><span class="tag fatality">FATALITY</span></div>
      <div class="seq">${d.inputSequence.map((s2) => `<kbd>${esc(s2)}</kbd>`).join('<span>→</span>')}</div>
      <div class="meta">${esc(d.finisherType)} · ${esc(d.distance)} · Ragdoll ${d.ragdoll ? 'an' : 'aus'} · ${esc(d.vfxExplosionAsset)}</div>
      <div class="move-actions"><button data-fatality="${esc(n)}">▶ Testen</button></div>
    </div>`).join('') : '<p class="empty">Keine Fatality. Frag die KI: „Fatality mit Explosion“.</p>';

  const props = fighter.stageInteractions || [];
  $('#stage-list').innerHTML = props.length ? props.map((o) => `
    <div class="move stage">
      <div class="move-head"><b>${esc(o.object)}</b><span class="tag stage">${esc(o.role)}</span></div>
      <div class="meta">${o.damage} DMG · Position ${o.position.map((v) => v.toFixed(1)).join(' / ')}</div>
    </div>`).join('') : '<p class="empty">Keine Arena-Objekte gesetzt.</p>';

  const ip = fighter.impactProfile;
  $('#impact-info').textContent = `leicht ${ip.lightHitstopFrames}f · schwer ${ip.heavyHitstopFrames}f · Shake ${ip.shakeStrength} · Zoom ${ip.zoomPunch ? 'an' : 'aus'}`;

  $('#xray-list').querySelectorAll('[data-xray]').forEach((b) => b.addEventListener('click', () => {
    const n = b.dataset.xray;
    fighter.meter = 100;                       // Testknopf füllt die Leiste
    fighter.executeCinematic(n, fighter.cinematics[n]);
  }));
  $('#fatality-list').querySelectorAll('[data-fatality]').forEach((b) => b.addEventListener('click', () => {
    const n = b.dataset.fatality;
    fighter.executeFatality(n, fighter.fatalities[n]);
  }));
}

function wireFighter() {
  fighter.showHitboxes = $('#opt-hitboxes')?.checked ?? false;
  fighter.on('hit', ({ name, damage, status, guardBreak, wallBounce }) => {
    dummyHP = Math.max(0, dummyHP - damage);
    $('#dummy-hp').style.width = dummyHP + '%';
    $('#dummy-hp-label').textContent = `Dummy ${dummyHP} HP`;
    flash(`${name} · ${damage} DMG${status && status !== 'none' ? ' · ' + status : ''}${guardBreak ? ' · GUARD BREAK' : ''}${wallBounce ? ' · WALL BOUNCE' : ''}`);
    if (dummyHP === 0) {
      const hasFatality = Object.keys(fighter.fatalities).length > 0;
      if (hasFatality && !fighter.finisherMode) {
        fighter.setFinisherMode(true);
        AudioPool.play('finish_him');
        const fh = $('#finish-him');
        fh.classList.add('show');
        const list = Object.entries(fighter.fatalities)
          .map(([n, f]) => `${n}: ${f.inputSequence.join(' → ')}`).join(' · ');
        fh.innerHTML = `FINISH HIM!<small>${esc(list)}</small>`;
        addMsg('sys', `🔥 <b>FINISH HIM!</b> — ${esc(list)}`);
      } else if (!hasFatality) {
        setTimeout(resetDummy, 900);
      }
    }
  });
  fighter.on('finisher-mode', ({ active }) => {
    $('#state-label').textContent = active ? 'FINISH HIM!' : 'bereit';
  });
  fighter.on('move', ({ name, phase }) => {
    if (phase === 'startup') $('#state-label').textContent = `▶ ${name}`;
    if (phase === 'idle') $('#state-label').textContent = fighter.finisherMode ? 'FINISH HIM!' : 'bereit';
  });
  fighter.on('input', ({ buffer }) => {
    $('#buffer-view').textContent = buffer.slice(-8).join(' → ') || '—';
  });

  // --- Wucht: Hitstop, Shake, Zoom bei jedem Treffer ---
  fighter.on('hit', ({ damage, cinematic }) => {
    const ip = fighter.impactProfile;
    const heavy = cinematic || damage >= 20;
    director.impact({
      frames: heavy ? ip.heavyHitstopFrames : ip.lightHitstopFrames,
      strength: ip.shakeStrength * (heavy ? 1.4 : 0.8),
      zoom: ip.zoomPunch ? (heavy ? 0.45 : 0.18) : 0,
    });
  });

  // --- Meter / Leiste ---
  fighter.on('meter', ({ meter, full }) => {
    $('#meter-fill').style.width = meter + '%';
    $('#meter-label').textContent = full ? 'X-RAY BEREIT' : `Leiste ${Math.round(meter)} %`;
    $('#meter-label').classList.toggle('ready', full);
  });

  fighter.on('blocked', ({ name, reason }) => flash(`${name}: ${reason}`));

  // --- X-Ray / Cinematic ---
  fighter.on('cinematic', (ev) => {
    AudioPool.initAudio(); AudioPool.resumeAudio();
    director.play({
      path: ev.cameraPath, victim: dummy.position, attacker: fighterRoot.position,
      durationFrames: ev.durationFrames, slowMotionFactor: ev.slowMotionFactor,
      zoomFrame: ev.zoomFrame, label: ev.name, kind: 'xray',
    });
    showCinemaBanner(`X-RAY · ${ev.name}`, `${ev.boneTarget} · ${ev.damage} DMG`);
    addMsg('sys', `🦴 <b>${esc(ev.name)}</b> — Zeitlupe ${ev.slowMotionFactor}×, Kamera „${esc(ev.cameraPath)}", Ziel ${esc(ev.boneTarget)}.`);
  });
  fighter.on('cinematic-end', () => hideCinemaBanner());

  // --- Fatality ---
  fighter.on('fatality', (ev) => {
    AudioPool.initAudio(); AudioPool.resumeAudio();
    director.play({
      path: ev.cameraPath, victim: dummy.position, attacker: fighterRoot.position,
      durationFrames: ev.durationFrames, slowMotionFactor: ev.slowMotionFactor,
      zoomFrame: 20, label: ev.name, kind: 'fatality',
    });
    const dir = dummy.position.clone().sub(fighterRoot.position).setY(0).normalize();
    if (ev.ragdoll) ragdoll.explode(dummy, ev.finisherType, dir);
    if (particles) particles.spawn(ev.vfxExplosionAsset, dummy.position.clone().setY(1.2), { scale: 1.3 });
    showCinemaBanner(`FATALITY · ${ev.name}`, ev.finisherType);
    addMsg('sys', `💀 <b>FATALITY: ${esc(ev.name)}</b> — ${esc(ev.finisherType)}, Ragdoll ${ev.ragdoll ? 'an' : 'aus'}.`);
  });
  fighter.on('fatality-end', () => {
    hideCinemaBanner();
    setTimeout(() => { ragdoll.clear(); resetDummy(); }, 1400);
  });

  // --- Arena-Objekte aus dem JSON aufbauen ---
  fighter.on('stage', ({ interactions }) => {
    const n = stage.build(interactions);
    $('#stage-label').textContent = n ? `${n} Arena-Objekte` : 'keine Arena-Objekte';
  });
}

function showCinemaBanner(title, sub) {
  const el = $('#cinema-banner');
  el.innerHTML = `<b>${esc(title)}</b><small>${esc(sub || '')}</small>`;
  el.classList.add('show');
  document.body.classList.add('cinema');
}
function hideCinemaBanner() {
  $('#cinema-banner').classList.remove('show');
  document.body.classList.remove('cinema');
}

function resetDummy() {
  dummyHP = 100;
  dummy.position.set(2.6, 0, 0);
  dummy.rotation.set(0, 0, 0);
  dummy.visible = true;
  $('#dummy-hp').style.width = '100%';
  $('#dummy-hp-label').textContent = 'Dummy 100 HP';
  fighter.setFinisherMode(false);
  $('#finish-him').classList.remove('show');
}

let flashTimer = 0;
function flash(text) {
  const el = $('#hit-flash');
  el.textContent = text;
  el.classList.add('show');
  clearTimeout(flashTimer);
  flashTimer = setTimeout(() => el.classList.remove('show'), 900);
}

// ---------------------------------------------------------------------------
//  GLB-Upload
// ---------------------------------------------------------------------------

const loader = new GLTFLoader();
let currentModelName = 'Platzhalter-Penner';

function loadGlbFromUrl(url, name) {
  $('#model-status').textContent = 'Lade ' + name + ' …';
  loader.load(url, (gltf) => {
    scene.remove(fighterRoot);
    fighterRoot = gltf.scene;

    // Auf ~1.8 m normalisieren und auf den Boden setzen
    const box = new THREE.Box3().setFromObject(fighterRoot);
    const size = box.getSize(new THREE.Vector3());
    const s = size.y > 0.001 ? 1.8 / size.y : 1;
    fighterRoot.scale.setScalar(s);
    const box2 = new THREE.Box3().setFromObject(fighterRoot);
    const c = box2.getCenter(new THREE.Vector3());
    fighterRoot.position.x -= c.x;
    fighterRoot.position.z -= c.z;
    fighterRoot.position.y -= box2.min.y;
    fighterRoot.traverse((o) => { if (o.isMesh) { o.castShadow = true; o.frustumCulled = false; } });
    scene.add(fighterRoot);

    const clips = gltf.animations || [];
    const prev = fighter.exportConfiguration();
    fighter = new AIConfigurableFighter(fighterRoot, { scene, particles, clips });
    wireFighter();
    fighter.applyAiConfiguration(prev);   // Moves am neuen Modell weiterverwenden
    fighter.playIdle();
    currentModelName = name;

    $('#model-status').innerHTML = `<b>${esc(name)}</b> geladen · ${clips.length} Animation(en)`;
    $('#clip-list').innerHTML = clips.length
      ? clips.map((c) => `<kbd>${esc(c.name)}</kbd>`).join(' ')
      : '<small>Keine Clips im GLB — Framedaten steuern trotzdem VFX &amp; Hitboxen.</small>';
    addMsg('sys', `📦 Modell <b>${esc(name)}</b> geladen (${clips.length} Clips). Die KI kennt die Clipnamen jetzt.`);
    renderCatalog();
  }, undefined, (err) => {
    $('#model-status').textContent = 'Fehler beim Laden: ' + err.message;
  });
}

function handleFile(file) {
  if (!file) return;
  if (!/\.(glb|gltf)$/i.test(file.name)) {
    $('#model-status').textContent = 'Nur .glb / .gltf werden unterstützt.';
    return;
  }
  const url = URL.createObjectURL(file);
  loadGlbFromUrl(url, file.name.replace(/\.(glb|gltf)$/i, ''));
}

$('#file-input').addEventListener('change', (e) => handleFile(e.target.files[0]));
const drop = $('#drop-zone');
['dragenter', 'dragover'].forEach((ev) => drop.addEventListener(ev, (e) => { e.preventDefault(); drop.classList.add('over'); }));
['dragleave', 'drop'].forEach((ev) => drop.addEventListener(ev, (e) => { e.preventDefault(); drop.classList.remove('over'); }));
drop.addEventListener('drop', (e) => handleFile(e.dataTransfer.files[0]));
$('#btn-placeholder').addEventListener('click', () => {
  scene.remove(fighterRoot);
  fighterRoot = buildPlaceholderFighter();
  scene.add(fighterRoot);
  const prev = fighter.exportConfiguration();
  fighter = new AIConfigurableFighter(fighterRoot, { scene, particles });
  wireFighter();
  fighter.applyAiConfiguration(prev);
  currentModelName = 'Platzhalter-Penner';
  $('#model-status').textContent = 'Platzhalter-Rig aktiv';
  $('#clip-list').innerHTML = '<small>Prozeduraler Rig, keine Clips.</small>';
});

// ---------------------------------------------------------------------------
//  Eingaben (Tastatur + Touch)
// ---------------------------------------------------------------------------

const KEYMAP = {
  KeyW: 'UP', ArrowUp: 'UP',
  KeyS: 'DOWN', ArrowDown: 'DOWN',
  KeyD: 'FORWARD', ArrowRight: 'FORWARD',
  KeyA: 'BACK', ArrowLeft: 'BACK',
  KeyJ: 'LIGHT_PUNCH', KeyK: 'HEAVY_PUNCH',
  KeyN: 'LIGHT_KICK', KeyM: 'HEAVY_KICK',
  ShiftLeft: 'BLOCK', KeyG: 'GRAB', Space: 'SPECIAL',
};

/** Arena-Interaktionen liegen auf eigenen Tasten (E = werfen, Q = Wandsprung). */
const STAGE_KEYS = { KeyE: 'THROWABLE', KeyQ: 'ESCAPE_PAD', KeyR: 'HAZARD' };

function useStage(role) {
  const res = stage.interact(fighterRoot.position, dummy.position, role);
  if (!res) { flash('Kein Objekt in Reichweite'); return; }
  flash(`${res.type}: ${res.object}${res.damage ? ' · ' + res.damage + ' DMG' : ''}`);
  if (res.type === 'ESCAPE') director.impact({ frames: 1, strength: 0.3, zoom: 0.1 });
}

window.addEventListener('keydown', (e) => {
  if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) return;
  if (STAGE_KEYS[e.code]) {
    e.preventDefault();
    AudioPool.initAudio(); AudioPool.resumeAudio();
    useStage(STAGE_KEYS[e.code]);
    return;
  }
  const token = KEYMAP[e.code];
  if (!token) return;
  e.preventDefault();
  AudioPool.initAudio(); AudioPool.resumeAudio();
  fighter.pushInput(token);
});

document.querySelectorAll('[data-token]').forEach((b) => {
  const fire = (e) => {
    e.preventDefault();
    AudioPool.initAudio(); AudioPool.resumeAudio();
    fighter.pushInput(b.dataset.token);
  };
  b.addEventListener('pointerdown', fire);
});

// ---------------------------------------------------------------------------
//  Optionen / Bibliothek / Export
// ---------------------------------------------------------------------------

const resSelect = $('#opt-resolution');
Object.keys(RESOLUTION_PRESETS).forEach((p) => {
  const o = document.createElement('option');
  o.value = p; o.textContent = p + ` (${RESOLUTION_PRESETS[p].width}×${RESOLUTION_PRESETS[p].height})`;
  resSelect.appendChild(o);
});
resSelect.value = settings.resolution;
resLock.set(settings.resolution);
resSelect.addEventListener('change', () => {
  const s = resLock.set(resSelect.value);
  $('#res-readout').textContent = `${s.width}×${s.height}`;
  saveSettings();
});

$('#opt-hitboxes').checked = settings.hitboxes;
fighter.showHitboxes = settings.hitboxes;
$('#opt-hitboxes').addEventListener('change', (e) => { fighter.showHitboxes = e.target.checked; if (!e.target.checked) fighter.hideHitbox(); saveSettings(); });

const modeSel = $('#opt-ai-mode');
modeSel.value = ai.mode;
const remoteBox = $('#remote-settings');
const syncMode = () => { remoteBox.classList.toggle('hidden', modeSel.value !== 'remote'); };
modeSel.addEventListener('change', () => { ai.configure({ mode: modeSel.value }); syncMode(); saveSettings(); });
syncMode();
$('#opt-endpoint').value = ai.endpoint;
$('#opt-model').value = ai.model;
$('#opt-key').value = ai.apiKey;
['endpoint', 'model', 'key'].forEach((k) => {
  $('#opt-' + k).addEventListener('change', (e) => {
    ai.configure({ [k === 'key' ? 'apiKey' : k]: e.target.value });
    saveSettings();
  });
});

// Asset-Bibliothek anzeigen (das Manifest, das die KI liest)
$('#lib-vfx').innerHTML = Object.entries(VFX_LIBRARY).map(([k, v]) => `<li><code>${k}</code><span>${v.label}</span></li>`).join('');
$('#lib-audio').innerHTML = Object.entries(AUDIO_LIBRARY).map(([k, v]) => `<li><code>${k}</code><span>${v.label}</span></li>`).join('');
$('#lib-hitbox').innerHTML = Object.entries(HITBOX_LIBRARY).map(([k, v]) => `<li><code>${k}</code><span>${v.label}</span></li>`).join('');
$('#lib-manifest').textContent = JSON.stringify(manifest(), null, 2);

// --- Kampfprofile ---------------------------------------------------------
const presetList = $('#preset-list');
presetList.innerHTML = PRESET_LIST.map((p) => `
  <button class="preset" data-preset="${p.id}" style="--accent:${p.accent}">
    <span class="emoji">${p.emoji}</span>
    <span><b>${esc(p.name)}</b><small>${esc(p.role)} · ${esc(p.summary)}</small></span>
  </button>`).join('');

function loadPreset(id) {
  const preset = PRESET_LIST.find((p) => p.id === id);
  if (!preset) return;
  AudioPool.initAudio(); AudioPool.resumeAudio();
  // Genau der Weg aus der Doku: roher JSON-Block rein, Validierung inklusive.
  const changes = fighter.injectAiConfiguration(preset.config);
  jsonView.textContent = JSON.stringify(preset.config, null, 2);
  $('#profile-label').textContent = 'Profil: ' + preset.name;
  presetList.querySelectorAll('.preset').forEach((b) => b.classList.toggle('active', b.dataset.preset === id));
  addMsg('sys', `${preset.emoji} <b>${esc(preset.name)}</b> injiziert:<br>` + changes.map((c) => '✅ ' + esc(c)).join('<br>'));
  renderCatalog();
  saveSettings();
}
presetList.querySelectorAll('[data-preset]').forEach((b) => b.addEventListener('click', () => loadPreset(b.dataset.preset)));

$('#btn-export-presets').addEventListener('click', () => {
  const blob = new Blob([JSON.stringify(presetConfigs(), null, 2)], { type: 'application/json' });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = 'pk-fighter-profiles.json';
  a.click();
});

$('#btn-export').addEventListener('click', () => {
  const blob = new Blob([JSON.stringify(fighter.exportConfiguration(), null, 2)], { type: 'application/json' });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = 'pk-fighter-config.json';
  a.click();
});
$('#btn-import').addEventListener('click', () => $('#import-input').click());
$('#import-input').addEventListener('change', async (e) => {
  const f = e.target.files[0];
  if (!f) return;
  try {
    const { validateConfig } = await import('./schema.js');
    const res = validateConfig(await f.text());
    if (!res.ok) throw new Error(res.errors.join(' '));
    fighter.applyAiConfiguration(res.config);
    jsonView.textContent = JSON.stringify(res.config, null, 2);
    renderCatalog(); saveSettings();
    addMsg('sys', `📥 Konfiguration <b>${esc(f.name)}</b> importiert.`);
  } catch (err) {
    addMsg('sys', '⚠️ Import fehlgeschlagen: ' + esc(err.message));
  }
});
$('#btn-reset').addEventListener('click', () => {
  fighter.reset();
  ragdoll.clear();
  stage.clear();
  director.cancel();
  hideCinemaBanner();
  resetDummy();
  $('#meter-fill').style.width = '0%';
  $('#meter-label').textContent = 'Leiste 0 %';
  $('#stage-label').textContent = 'keine Arena-Objekte';
  $('#profile-label').textContent = 'Profil: —';
  presetList.querySelectorAll('.preset').forEach((b) => b.classList.remove('active'));
  renderCatalog(); saveSettings();
  jsonView.textContent = '{}';
  addMsg('sys', '♻️ Charakter zurückgesetzt.');
});

document.querySelectorAll('.tab').forEach((t) => t.addEventListener('click', () => {
  document.querySelectorAll('.tab').forEach((x) => x.classList.toggle('active', x === t));
  document.querySelectorAll('.tab-panel').forEach((p) => p.classList.toggle('hidden', p.id !== 'panel-' + t.dataset.tab));
}));

// Gespeicherte Konfiguration wiederherstellen
if (settings.config && (settings.config.specialAttacks?.length || settings.config.combos?.length)) {
  fighter.applyAiConfiguration(settings.config);
  jsonView.textContent = JSON.stringify(settings.config, null, 2);
  addMsg('sys', '💾 Letzte Konfiguration aus diesem Browser wiederhergestellt.');
}
renderCatalog();

addMsg('sys', [
  'Willkommen in der <b>KI-Werkstatt</b>. Lade links ein <code>.glb</code> hoch (oder nimm den Platzhalter) und beschreibe unten, was der Kämpfer können soll.',
  'Die KI schreibt keinen Code — sie übersetzt deinen Satz in ein JSON-Datenpaket, das die Game-Loop sofort lädt.',
].join('<br>'));

// ---------------------------------------------------------------------------
//  Game Loop
// ---------------------------------------------------------------------------

let last = performance.now();
let fpsAcc = 0, fpsFrames = 0;
const size0 = resLock.size;
$('#res-readout').textContent = `${size0.width}×${size0.height}`;

function tick(now) {
  const rawDt = Math.min(0.05, (now - last) / 1000);
  last = now;

  // Der Regisseur entscheidet über Hitstop (dt = 0) und Zeitlupe
  const dt = director.update(rawDt);

  if (fighterRoot.userData.animate) fighterRoot.userData.animate(now / 1000);
  fighter.update(dt, dummy);
  particles.update(dt);
  ragdoll.update(dt);
  stage.update(dt, dummy);

  // Ruhekamera nur, wenn keine Kamerafahrt läuft
  if (!director.isPlaying) {
    const camX = 3.4 + Math.sin(now / 4200) * 0.35;
    camera.position.set(camX, 2.3, 5.6);
    camera.lookAt(1.2, 1.05, 0);
    director.setHome(camera.position, new THREE.Vector3(1.2, 1.05, 0));
    if (!ragdoll.active) dummy.rotation.y = Math.sin(now / 1800) * 0.12 - 0.5;
  }

  renderer.render(scene, camera);

  fpsAcc += dt; fpsFrames++;
  if (fpsAcc >= 0.5) {
    $('#fps-readout').textContent = Math.round(fpsFrames / fpsAcc) + ' FPS';
    fpsAcc = 0; fpsFrames = 0;
  }
  requestAnimationFrame(tick);
}
requestAnimationFrame(tick);

// Für Konsole/Debug
window.PK_LAB = {
  get fighter() { return fighter; },
  ai, scene, particles, resLock, sendPrompt, loadPreset,
  director, ragdoll, stage, useStage,
  get dummyHP() { return dummyHP; },
  set dummyHP(v) { dummyHP = v; },
  dummy,
  presets: presetConfigs(),
};
