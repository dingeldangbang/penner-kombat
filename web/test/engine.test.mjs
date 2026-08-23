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

test('Profile: alle validieren ohne Warnungen', () => {
  assert.equal(PRESET_LIST.length, 5);
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
  assert.equal(total, 14, 'alle fünf Profile zusammen = 9 Specials + 5 Combos');

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

// ---------------------------------------------------------------------------
//  Hardcore-Stufe: X-Ray, Fatality, Ragdoll, Stage, Hitstop
// ---------------------------------------------------------------------------

test('Hardcore: Doku-JSON (Spine Shatter + Acid Meltdown) validiert 1:1', () => {
  const res = validateConfig({
    cinematicMoves: [{
      name: 'Spine Shatter X-Ray', inputSequence: ['LIGHT_PUNCH', 'BLOCK'],
      triggerCondition: 'METERS_FULL', cinematicZoomFrame: 14,
      slowMotionFactor: 0.15, boneTarget: 'SPINE_T3',
    }],
    fatalities: [{
      name: 'Acid Meltdown', distance: 'MEDIUM',
      inputSequence: ['DOWN', 'DOWN', 'FORWARD', 'HEAVY_KICK'],
      finisherType: 'DISMEMBERMENT', vfxExplosionAsset: 'acid_spit_corrosive',
    }],
  });
  assert.ok(res.ok);
  const x = res.config.cinematicMoves[0];
  assert.equal(x.boneTarget, 'SPINE_T3');
  assert.equal(x.slowMotionFactor, 0.15);
  assert.equal(x.cinematicZoomFrame, 14);
  const f = res.config.fatalities[0];
  assert.equal(f.finisherType, 'DISMEMBERMENT');
  assert.equal(f.distance, 'MEDIUM');
  assert.equal(f.vfxExplosionAsset, 'acid_corrosive', 'erfundener VFX-Name muss gemappt werden');
  assert.equal(f.ragdoll, true);
});

test('Hardcore: Schema klemmt irrsinnige Kino-Werte', () => {
  const { config } = validateConfig({
    cinematicMoves: [{ name: 'Overkill', damage: 9999, slowMotionFactor: 0, hitstopFrames: 999, boneTarget: 'LEBER' }],
    fatalities: [{ name: 'X', finisherType: 'TELEPORT_TO_MARS' }],
  });
  assert.equal(config.cinematicMoves[0].damage, 90);
  assert.equal(config.cinematicMoves[0].slowMotionFactor, 0.05);
  assert.equal(config.cinematicMoves[0].hitstopFrames, 30);
  assert.equal(config.cinematicMoves[0].boneTarget, 'SPINE_T3');
  assert.equal(config.fatalities[0].finisherType, 'EXPLOSION');
});

test('Hardcore: alle fünf Profile bringen X-Ray und Fatality mit', () => {
  assert.equal(PRESET_LIST.length, 5);
  for (const p of PRESET_LIST) {
    const cfg = validateConfig(p.config).config;
    assert.equal(cfg.cinematicMoves.length, 1, p.name + ' ohne X-Ray');
    assert.equal(cfg.fatalities.length, 1, p.name + ' ohne Fatality');
    assert.ok(cfg.impactProfile, p.name + ' ohne Wucht-Profil');
  }
  const viper = validateConfig(PRESETS.nicro_viper.config).config;
  assert.equal(viper.stageInteractions.length, 4);
  assert.equal(viper.fatalities[0].name, 'Acid Meltdown');
  assert.equal(viper.cinematicMoves[0].name, 'Spine Shatter X-Ray');
});

test('Hardcore: X-Ray feuert nur mit voller Leiste', maybe, async () => {
  const { fighter, dummy } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.nicro_viper.config);

  const blocked = [];
  fighter.on('blocked', (b) => blocked.push(b));
  let cine = null;
  fighter.on('cinematic', (c) => { cine = c; });

  fighter.pushInput('LIGHT_PUNCH');
  fighter.pushInput('BLOCK');
  assert.equal(cine, null, 'X-Ray darf ohne Meter nicht starten');
  assert.equal(blocked.length, 1);
  assert.match(blocked[0].reason, /Leiste/);

  fighter.meter = 100;
  fighter.pushInput('LIGHT_PUNCH');
  fighter.pushInput('BLOCK');
  assert.ok(cine, 'X-Ray startet mit voller Leiste nicht');
  assert.equal(cine.boneTarget, 'SPINE_T3');
  assert.equal(cine.slowMotionFactor, 0.15);
  assert.equal(fighter.state.phase, 'cinematic');
  assert.equal(fighter.meter, 0, 'Leiste muss verbraucht werden');

  // Treffer sitzt beim Zoom-Frame, danach zurück in idle
  let hit = null;
  fighter.on('hit', (h) => { hit = h; });
  runFrames(fighter, dummy, 4);
  assert.ok(hit && hit.cinematic === 'cinematic');
  assert.equal(hit.boneTarget, 'SPINE_T3');
  assert.equal(fighter.state.phase, 'idle');
});

test('Hardcore: Treffer füllen die Leiste', maybe, async () => {
  const { fighter, dummy } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.nicro_viper.config);
  dummy.position.set(1.6, 0, 0);
  assert.equal(fighter.meter, 0);

  ['DOWN', 'FORWARD', 'HEAVY_KICK'].forEach((t) => fighter.pushInput(t));
  runFrames(fighter, dummy);
  assert.ok(fighter.meter > 0, 'Leiste füllt sich nicht');
});

test('Hardcore: Fatality nur im FINISH-HIM-Modus', maybe, async () => {
  const { fighter, dummy } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.nicro_viper.config);

  let fat = null;
  fighter.on('fatality', (f) => { fat = f; });
  ['DOWN', 'DOWN', 'FORWARD', 'HEAVY_KICK'].forEach((t) => fighter.pushInput(t));
  assert.equal(fat, null, 'Fatality darf im Normalzustand nicht zünden');
  fighter.state = { phase: 'idle', move: null, timer: 0, hitApplied: false };
  fighter.inputBuffer.clear();

  fighter.setFinisherMode(true);
  ['DOWN', 'DOWN', 'FORWARD', 'HEAVY_KICK'].forEach((t) => fighter.pushInput(t));
  assert.ok(fat, 'Fatality zündet im Finisher-Modus nicht');
  assert.equal(fat.finisherType, 'DISMEMBERMENT');
  assert.equal(fat.ragdoll, true);
  assert.equal(fighter.state.phase, 'fatality');

  let ended = false;
  fighter.on('fatality-end', () => { ended = true; });
  runFrames(fighter, dummy, 6);
  assert.ok(ended, 'Fatality endet nicht');
});

test('Hardcore: Ragdoll zerlegt das Opfer und lässt Teile fallen', maybe, async () => {
  const { scene, dummy, spawned } = await makeRig();
  const { RagdollSystem } = await import('../src/lab/ragdoll.js');
  const particles = { spawn: (name, pos) => spawned.push({ name, pos: pos.clone() }) };
  const rag = new RagdollSystem(scene, particles);

  const info = rag.explode(dummy, 'EXPLOSION', new THREE.Vector3(1, 0, 0));
  assert.equal(info.parts, 6, 'EXPLOSION muss 6 Teile abtrennen');
  assert.equal(dummy.visible, false, 'Skelett-Darstellung muss aus sein');
  assert.ok(spawned.some((s) => s.name === 'gore_explosion'));

  const y0 = rag.parts.map((p) => p.mesh.position.y);
  for (let i = 0; i < 120; i++) rag.update(1 / 60);
  const settled = rag.parts.every((p) => p.mesh.position.y >= 0.17 && p.mesh.position.y < 2.5);
  assert.ok(settled, 'Teile müssen auf dem Boden landen, nicht durchfallen');
  assert.ok(rag.parts.some((p, i) => Math.abs(p.mesh.position.y - y0[i]) > 0.01), 'Physik bewegt nichts');

  rag.clear();
  assert.equal(dummy.visible, true);
  assert.equal(rag.parts.length, 0);
});

test('Hardcore: Stage-Objekte werden gebaut, geworfen und treffen', maybe, async () => {
  const { scene, dummy, spawned } = await makeRig();
  const { StageProps } = await import('../src/lab/stage.js');
  const particles = { spawn: (n, p) => spawned.push({ name: n, pos: p.clone() }) };
  const stage = new StageProps(scene, particles);

  const cfg = validateConfig(PRESETS.nicro_viper.config).config;
  const built = stage.build(cfg.stageInteractions);
  assert.equal(built, 4);

  const hits = [];
  stage.on('hit', (h) => hits.push(h));
  dummy.position.set(-3.0, 0, -2.0);
  const res = stage.interact(new THREE.Vector3(-4.2, 0, -2.0), dummy.position, 'THROWABLE');
  assert.ok(res && res.type === 'THROW', 'Wurf nicht ausgelöst');
  assert.equal(res.object, 'burning_barrel');

  for (let i = 0; i < 180 && !hits.length; i++) stage.update(1 / 60, dummy);
  assert.equal(hits.length, 1, 'Geworfenes Objekt trifft nicht');
  assert.ok(hits[0].damage >= 10);

  // Wandsprung
  const esc = stage.interact(new THREE.Vector3(-6.0, 0, 3.0), dummy.position, 'ESCAPE_PAD');
  assert.equal(esc.type, 'ESCAPE');
  // Nichts in Reichweite
  assert.equal(stage.interact(new THREE.Vector3(0, 0, 0), dummy.position, 'HAZARD'), null);
  stage.clear();
});

test('Hardcore: Regisseur friert bei Hitstop ein und dehnt in Zeitlupe', maybe, async () => {
  const { CinematicDirector } = await import('../src/lab/cinematic.js');
  const camera = new THREE.PerspectiveCamera(52, 16 / 9, 0.1, 100);
  camera.position.set(3, 2, 5);
  const dir = new CinematicDirector(camera, { position: camera.position.clone(), lookAt: new THREE.Vector3() });

  assert.equal(dir.update(1 / 60), 1 / 60, 'Normalbetrieb darf dt nicht verändern');

  dir.hitstop(5);
  assert.equal(dir.update(1 / 60), 0, 'Hitstop muss die Spielzeit anhalten');
  for (let i = 0; i < 5; i++) dir.update(1 / 60);
  assert.ok(dir.update(1 / 60) > 0, 'Hitstop muss auslaufen');

  dir.play({
    path: 'orbit_victim', victim: new THREE.Vector3(2, 0, 0), attacker: new THREE.Vector3(0, 0, 0),
    durationFrames: 60, slowMotionFactor: 0.2, zoomFrame: 6, label: 'Test X-Ray',
  });
  assert.ok(dir.isPlaying);
  assert.ok(Math.abs(dir.update(1 / 60) - (1 / 60) * 0.2) < 1e-9, 'Zeitlupe skaliert dt nicht');
  const camDuringXray = camera.position.clone();
  for (let i = 0; i < 70; i++) dir.update(1 / 60);
  assert.ok(!dir.isPlaying, 'Kamerafahrt endet nicht');
  assert.ok(camera.position.distanceTo(camDuringXray) > 0.1, 'Kamera bewegt sich nicht');
  assert.ok(camera.position.distanceTo(new THREE.Vector3(3, 2, 5)) < 0.001, 'Kamera kehrt nicht heim');
  assert.equal(dir.timeScale, 1);
});

test('Hardcore: Export/Reimport erhält X-Ray, Fatality, Arena und Wucht', maybe, async () => {
  const { fighter } = await makeRig();
  fighter.injectAiConfiguration(PRESETS.nicro_viper.config);
  const exported = fighter.exportConfiguration();
  assert.equal(exported.cinematicMoves.length, 1);
  assert.equal(exported.fatalities.length, 1);
  assert.equal(exported.stageInteractions.length, 4);
  assert.equal(exported.impactProfile.heavyHitstopFrames, 8);

  const res = validateConfig(exported);
  assert.ok(res.ok);
  const { fighter: fresh } = await makeRig();
  fresh.applyAiConfiguration(res.config);
  assert.ok(fresh.cinematics['Spine Shatter X-Ray']);
  assert.ok(fresh.fatalities['Acid Meltdown']);
  assert.equal(fresh.impactProfile.shakeStrength, 1.5);
});

test('Hardcore: Chat-Prompts erzeugen X-Ray, Fatality und Arena', maybe, async () => {
  const { AIEngine } = await import('../src/lab/aiEngine.js');
  const { fighter } = await makeRig();
  const ai = new AIEngine({ mode: 'local' });

  const x = await ai.request('Gib ihm einen X-Ray Move auf die Rippen mit 40 Schaden');
  fighter.applyAiConfiguration(x.config);
  const xray = Object.values(fighter.cinematics)[0];
  assert.equal(xray.boneTarget, 'RIBCAGE');
  assert.equal(xray.damage, 40);

  const f = await ai.request('Fatality die den Gegner mit Säure auflöst');
  fighter.applyAiConfiguration(f.config);
  assert.equal(Object.values(fighter.fatalities)[0].finisherType, 'MELTDOWN');

  const s = await ai.request('Stell brennende Fässer und eine Gasflasche in die Arena');
  fighter.applyAiConfiguration(s.config);
  assert.deepEqual(fighter.stageInteractions.map((o) => o.object), ['burning_barrel', 'gas_bottle']);
});
