/**
 * engine.test.mjs — End-to-End-Test der Laufzeit-Engine mit echtem three.js
 *
 * Prüft, was die Doku verspricht: JSON rein → Materialien wechseln, Move-Katalog
 * hört auf die Eingabefolgen, Framedaten laufen ab, VFX werden am richtigen
 * Punkt gespawnt, Gegner reagiert auf Sog/Wandbounce.
 *
 * Läuft nur, wenn `three` installiert ist (`cd web && npm install`), sonst
 * werden die Tests übersprungen — CI ohne Netz bleibt grün.
 */

import test from 'node:test';
import assert from 'node:assert/strict';

let THREE = null;
try { THREE = await import('three'); } catch { /* three fehlt */ }

const maybe = { skip: !THREE ? 'three nicht installiert (npm install)' : false };

const { PRESETS, PRESET_LIST } = await import('../src/lab/presets.js');
const { validateConfig } = await import('../src/lab/schema.js');

// --- Profile: reine Datenprüfung (läuft immer) ------------------------------

test('Profile: alle vier validieren ohne Warnungen', () => {
  assert.equal(PRESET_LIST.length, 4);
  for (const p of PRESET_LIST) {
    const res = validateConfig(p.config);
    assert.ok(res.ok, `${p.name} ungültig`);
    assert.deepEqual(res.warnings, [], `${p.name}: ${res.warnings.join(', ')}`);
    assert.equal(res.config.profileName, p.name);
  }
});

test('Profile: Kernmechaniken sind wie spezifiziert gesetzt', () => {
  const scorpion = validateConfig(PRESETS.cyber_scorpion.config).config;
  const grab = scorpion.specialAttacks.find((s) => s.name === 'Magnet Grab');
  assert.equal(grab.hitboxShape, 'screen_long_box');
  assert.equal(grab.statusEffect, 'pull');
  assert.ok(grab.pullStrength > 0);
  const overload = scorpion.combos.find((c) => c.name === 'Overload');
  assert.equal(overload.hits, 4);
  assert.deepEqual(overload.inputSequence, ['LIGHT_PUNCH', 'LIGHT_PUNCH', 'LIGHT_PUNCH', 'LIGHT_PUNCH']);
  assert.ok(overload.startupFrames <= 6, 'Overload muss schnell anlaufen');
  assert.equal(scorpion.visualOverrides.metalness, 1);

  const ghoul = validateConfig(PRESETS.toxic_ghoulem.config).config;
  const acid = ghoul.specialAttacks.find((s) => s.name === 'Acid Vomit');
  assert.equal(acid.hitboxShape, 'wide_cone');
  assert.equal(acid.guardBreak, true);
  const carnage = ghoul.combos.find((c) => c.name === 'Brutal Carnage');
  assert.ok(carnage.startupFrames >= 14, 'Brutal Carnage soll träge anlaufen');
  assert.ok(carnage.hitStunFrames >= 30, 'Brutal Carnage soll langen Hitstun haben');
  assert.ok(ghoul.visualOverrides.roughness <= 0.15, 'nasse Optik = niedrige Roughness');

  const priest = validateConfig(PRESETS.voodoo_priest.config).config;
  const rift = priest.specialAttacks.find((s) => s.name === 'Abyssal Rift');
  assert.equal(rift.spawnAt, 'ground_target');
  assert.equal(rift.hitboxShape, 'aoe_sphere');
  const reap = priest.combos.find((c) => c.name === 'Soul Reap');
  assert.equal(reap.inputSequence[0], 'LIGHT_KICK');
  assert.ok(reap.cancelWindowFrames >= 24, 'Soul Reap braucht großes Cancel-Fenster');

  const mech = validateConfig(PRESETS.bio_mech.config).config;
  const slam = mech.specialAttacks.find((s) => s.name === 'Meltdown Slam');
  assert.ok(slam.recoveryFrames >= 40, 'Meltdown Slam braucht lange Recovery');
  assert.ok(slam.startupFrames >= 20);
  const smash = mech.combos.find((c) => c.name === 'Heavy Core Smash');
  assert.equal(smash.wallBounce, true);
  assert.equal(smash.inputSequence[0], 'HEAVY_PUNCH');
  assert.equal(mech.stats.maxHP, 220);
});

test('Profile: keine kollidierenden Eingabefolgen innerhalb eines Profils', () => {
  for (const p of PRESET_LIST) {
    const cfg = validateConfig(p.config).config;
    const seqs = [...(cfg.specialAttacks || []), ...(cfg.combos || [])].map((m) => m.inputSequence.join(','));
    assert.equal(new Set(seqs).size, seqs.length, `${p.name} hat doppelte Eingabefolgen`);
  }
});

// --- Laufzeit mit echtem three.js -------------------------------------------

async function makeRig() {
  const { AIConfigurableFighter, LiveFighterEngine } = await import('../src/lab/fighter.js');
  const scene = new THREE.Scene();
  const mesh = new THREE.Mesh(
    new THREE.BoxGeometry(1, 1, 1),
    new THREE.MeshStandardMaterial({ color: 0x808080, metalness: 0, roughness: 1 }),
  );
  const root = new THREE.Group();
  root.add(mesh);
  scene.add(root);

  const spawned = [];
  const particles = { spawn: (name, pos, opts) => { spawned.push({ name, pos: pos.clone(), opts }); } };

  const dummy = new THREE.Group();
  dummy.position.set(2.6, 0, 0);
  scene.add(dummy);

  const fighter = new AIConfigurableFighter(root, { scene, particles });
  return { fighter, LiveFighterEngine, mesh, dummy, spawned, scene };
}

/** Spult die Move-Zustandsmaschine ab (60 fps), max. 6 Sekunden. */
function runFrames(fighter, dummy, seconds = 3) {
  const step = 1 / 60;
  for (let i = 0; i < Math.round(seconds / step); i++) {
    fighter.update(step, dummy);
    if (fighter.state.phase === 'idle' && i > 2) break;
  }
}

test('Engine: injectAiConfiguration bindet Katalog, Optik und Werte', maybe, async () => {
  const { fighter, mesh } = await makeRig();
  const changes = fighter.injectAiConfiguration(PRESETS.cyber_scorpion.config);

  assert.ok(changes.length >= 4, 'Änderungsliste zu kurz: ' + changes.join(' | '));
  assert.ok(fighter.moveCatalog['Magnet Grab'], 'Magnet Grab nicht gebunden');
  assert.ok(fighter.moveCatalog['Overload'], 'Overload nicht gebunden');
  assert.equal(fighter.moveCatalog['Overload'].kind, 'combo');
  assert.equal(fighter.profileName, 'Cyber-Scorpion');

  // Material hat sich wirklich verändert (Chrom)
  assert.equal(mesh.material.metalness, 1);
  assert.ok(mesh.material.roughness <= 0.2);
  assert.equal(mesh.material.color.getHexString(), 'c8d2dc');
  assert.equal(mesh.material.emissive.getHexString(), '00bfff');

  // Werte
  assert.equal(fighter.stats.maxHP, 95);
  assert.equal(fighter.hp, 95);
});

test('Engine: Eingabefolge löst den Move aus, Frames laufen ab', maybe, async () => {
  const { fighter, dummy } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.cyber_scorpion.config);

  const phases = [];
  fighter.on('move', ({ name, phase }) => phases.push(`${name}:${phase}`));

  ['DOWN', 'FORWARD', 'LIGHT_PUNCH'].forEach((t) => fighter.pushInput(t));
  assert.equal(fighter.state.phase, 'startup');
  assert.equal(fighter.state.move.name, 'Nano Bolt');

  runFrames(fighter, dummy);
  assert.deepEqual(phases, ['Nano Bolt:startup', 'Nano Bolt:active', 'Nano Bolt:recovery', 'Nano Bolt:idle']);
  assert.equal(fighter.state.phase, 'idle');
});

test('Engine: 4-Treffer-Kette Overload feuert nur komplett', maybe, async () => {
  const { fighter, dummy } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.cyber_scorpion.config);

  fighter.pushInput('LIGHT_PUNCH');
  fighter.pushInput('LIGHT_PUNCH');
  assert.equal(fighter.state.phase, 'idle', 'zwei Treffer dürfen die 4er-Kette nicht auslösen');
  fighter.pushInput('LIGHT_PUNCH');
  fighter.pushInput('LIGHT_PUNCH');
  assert.equal(fighter.state.move.name, 'Overload');

  let hit = null;
  fighter.on('hit', (h) => { hit = h; });
  runFrames(fighter, dummy);
  assert.equal(hit, null, 'kleine Kugel darf auf 2,6 m nicht treffen (Whiff)');

  // Auf Jab-Distanz sitzt die Kette
  dummy.position.set(1.2, 0, 0);
  ['LIGHT_PUNCH', 'LIGHT_PUNCH', 'LIGHT_PUNCH', 'LIGHT_PUNCH'].forEach((t) => fighter.pushInput(t));
  runFrames(fighter, dummy);
  assert.ok(hit, 'kein Treffer auf Nahdistanz');
  assert.equal(hit.damage, 24);
  assert.equal(hit.hits, 4);
});

test('Engine: Magnet Grab zieht den Gegner heran', maybe, async () => {
  const { fighter, dummy, spawned } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.cyber_scorpion.config);
  dummy.position.set(7, 0, 0);
  const before = dummy.position.x;

  ['BACK', 'FORWARD', 'GRAB'].forEach((t) => fighter.pushInput(t));
  runFrames(fighter, dummy);

  assert.ok(dummy.position.x < before - 1, `Sog wirkungslos (${before} -> ${dummy.position.x})`);
  assert.ok(spawned.some((s) => s.name === 'magnet_pull'), 'magnet_pull VFX fehlt');
});

test('Engine: Acid Vomit meldet Guard Break im Kegel', maybe, async () => {
  const { fighter, dummy, spawned } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.toxic_ghoulem.config);
  dummy.position.set(2.2, 0, 0);

  let hit = null;
  fighter.on('hit', (h) => { hit = h; });
  ['DOWN', 'BACK', 'HEAVY_PUNCH'].forEach((t) => fighter.pushInput(t));
  runFrames(fighter, dummy);

  assert.ok(hit, 'Kegel hat nicht getroffen');
  assert.equal(hit.guardBreak, true);
  assert.equal(hit.status, 'guard_break');
  assert.ok(spawned.some((s) => s.name === 'acid_spray'));
});

test('Engine: Abyssal Rift spawnt am Boden unter dem Gegner', maybe, async () => {
  const { fighter, dummy, spawned } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.voodoo_priest.config);
  dummy.position.set(3.1, 0, 1.4);

  ['DOWN', 'BACK', 'SPECIAL'].forEach((t) => fighter.pushInput(t));
  runFrames(fighter, dummy);

  const rift = spawned.find((s) => s.name === 'void_rift');
  assert.ok(rift, 'void_rift wurde nicht gespawnt');
  assert.ok(Math.abs(rift.pos.x - 3.1) < 0.01 && Math.abs(rift.pos.z - 1.4) < 0.01, 'nicht an den Gegner-Koordinaten');
  assert.ok(rift.pos.y < 0.3, 'nicht am Boden');
});

test('Engine: Heavy Core Smash schleudert in die Wand', maybe, async () => {
  const { fighter, dummy } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.bio_mech.config);
  dummy.position.set(2.0, 0, 0);
  const before = dummy.position.x;

  let hit = null;
  fighter.on('hit', (h) => { hit = h; });
  fighter.pushInput('HEAVY_PUNCH');
  fighter.pushInput('HEAVY_PUNCH');
  runFrames(fighter, dummy);

  assert.ok(hit && hit.wallBounce, 'Wandbounce nicht gemeldet');
  assert.ok(dummy.position.x > before + 1, 'Gegner wurde nicht weggeschleudert');
});

test('Engine: Meltdown Slam hat wirklich lange Recovery', maybe, async () => {
  const { fighter, dummy } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.bio_mech.config);

  ['DOWN', 'DOWN', 'HEAVY_PUNCH'].forEach((t) => fighter.pushInput(t));
  const step = 1 / 60;
  let frames = 0;
  while (fighter.state.phase !== 'idle' && frames < 400) { fighter.update(step, dummy); frames++; }
  // 26 Startup + 20 Active + 46 Recovery = 92 Frames
  assert.ok(frames >= 90 && frames <= 96, `Framedauer ${frames} passt nicht zu 26/20/46`);
});

test('Engine: Profilwechsel ersetzt Optik, reset stellt sie wieder her', maybe, async () => {
  const { fighter, mesh } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.cyber_scorpion.config);
  assert.equal(mesh.material.metalness, 1);

  fighter.injectAiConfiguration(PRESETS.bio_mech.config);
  assert.equal(mesh.material.color.getHexString(), '66ff00');
  assert.equal(fighter.stats.maxHP, 220);

  fighter.reset();
  assert.equal(mesh.material.metalness, 0);
  assert.equal(mesh.material.roughness, 1);
  assert.equal(mesh.material.color.getHexString(), '808080');
  assert.equal(Object.keys(fighter.moveCatalog).length, 0);
});

test('Engine: Export/Reimport erhält alle Moves verlustfrei', maybe, async () => {
  const { fighter } = await makeRig();
  for (const p of PRESET_LIST) fighter.injectAiConfiguration(p.config);
  const exported = fighter.exportConfiguration();
  const total = Object.keys(fighter.moveCatalog).length;
  assert.equal(total, 12, 'alle vier Profile zusammen = 8 Specials + 4 Combos');

  const res = validateConfig(exported);
  assert.ok(res.ok);
  const { fighter: fresh } = await makeRig();
  fresh.applyAiConfiguration(res.config);
  assert.equal(Object.keys(fresh.moveCatalog).length, total);
  assert.deepEqual(
    fresh.moveCatalog['Magnet Grab'].sequence,
    fighter.moveCatalog['Magnet Grab'].sequence,
  );
});

test('Engine: LiveFighterEngine ist derselbe Konstruktor', maybe, async () => {
  const { fighter, LiveFighterEngine } = await makeRig();
  assert.ok(fighter instanceof LiveFighterEngine);
  assert.equal(typeof fighter.injectAiConfiguration, 'function');
});

test('Engine: kaputtes JSON in injectAiConfiguration wird abgefangen', maybe, async () => {
  const { fighter } = await makeRig();
  const changes = fighter.injectAiConfiguration('{ kaputt ');
  assert.match(changes[0], /injectAiConfiguration/);
  assert.equal(Object.keys(fighter.moveCatalog).length, 0);
});

test('Engine: Chat-Prompt lädt Profil und bindet es an die Engine', maybe, async () => {
  const { AIEngine } = await import('../src/lab/aiEngine.js');
  const { fighter } = await makeRig();
  const res = await new AIEngine({ mode: 'local' }).request('Lade das Profil Toxic Blood-Ghoulem');
  assert.ok(res.ok);
  fighter.applyAiConfiguration(res.config);
  assert.ok(fighter.moveCatalog['Acid Vomit']);
  assert.equal(fighter.stats.maxHP, 185);
});
