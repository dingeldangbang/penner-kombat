// Tests der Kampflogik — laufen ohne Browser: node --test web/test/
import test from 'node:test';
import assert from 'node:assert/strict';
import { Match, Fighter, STEP, makeRandom } from '../src/sim.js';
import { K, ROSTER, byId, AI_LEVELS, STATURE } from '../src/data.js';

const idle = { p1: {} };

test('Roster hat 9 Kaempfer mit den Werten aus FighterDatabase', () => {
  assert.equal(ROSTER.length, 9);
  const le = byId('le_binde');
  assert.equal(le.maxHP, 130);
  assert.equal(le.moveSpeed, 4.2);
  assert.equal(le.stature, 'adipoes');
  assert.equal(byId('mell').maxHP, 85);
  assert.equal(byId('mojo_bob').range, 3.5);
});

test('Statur-Tabelle spiegelt StatureTable.cs', () => {
  assert.equal(STATURE.adipoes.mass, 1.45);
  assert.equal(STATURE.hager.height, 1.90);
  assert.equal(STATURE.breit.hitbox, 1.2);
});

test('KI-Stufen entsprechen AIController.ApplyDifficulty', () => {
  assert.equal(AI_LEVELS.length, 6);
  assert.equal(AI_LEVELS[0].damage, 0.6);
  assert.equal(AI_LEVELS[5].block, 0.80);
  assert.equal(AI_LEVELS[2].name, 'Mittel');
});

test('Block reduziert Schaden um 78 Prozent', () => {
  const rng = makeRandom(1);
  const f = new Fighter(byId('dieter'), 1, rng);
  f.blocking = true;
  const dealt = f.takeDamage(100, -10, null);
  assert.ok(Math.abs(dealt - 22) < 0.001, `erwartet 22, war ${dealt}`);
});

test('Rolle gibt i-Frames und blockt Schaden komplett', () => {
  const f = new Fighter(byId('mell'), 0, makeRandom(2));
  assert.equal(f.roll(1, 0), true);
  assert.ok(f.invuln > 0);
  assert.equal(f.takeDamage(50, 10, null), 0);
  assert.equal(f.hp, f.maxHP);
});

test('Ein Treffer landet und laedt die Fatal-Blow-Leiste', () => {
  const m = new Match({ p1: 'kalle', p2: 'uschi', p2IsAI: false, seed: 7 });
  m.p1.x = 0; m.p2.x = 1.6;
  let hit = null;
  for (let i = 0; i < 60 && !hit; i++) {
    const ev = m.step({ p1: { heavy: i === 0 }, p2: {} });
    hit = ev.find((e) => e.type === 'hit');
  }
  assert.ok(hit, 'kein Treffer registriert');
  assert.ok(hit.damage > 0);
  assert.equal(m.p1.meter, K.fatalBlowChargePerHit);
  assert.ok(m.p2.hp < m.p2.maxHP);
});

test('Med-Kapsel heilt 25 HP nach 1,2 s und verbraucht eine Ladung', () => {
  const m = new Match({ p1: 'rolf', p2: 'sigi', p2IsAI: false, seed: 3 });
  m.p1.hp = 40;
  m.step({ p1: { med: true }, p2: {} });
  assert.equal(m.p1.med, K.medCharges - 1);
  for (let i = 0; i < 80; i++) m.step(idle);
  assert.ok(m.p1.hp >= 65 - 0.01, `HP war ${m.p1.hp}`);
});

test('Rundenende bei Zeitablauf, mehr HP gewinnt', () => {
  const m = new Match({ p1: 'le_binde', p2: 'mell', p2IsAI: false, roundTime: 1, seed: 5 });
  m.p2.hp = 10;
  let end = null;
  for (let i = 0; i < 120 && !end; i++) {
    end = m.step(idle).find((e) => e.type === 'roundEnd');
  }
  assert.ok(end, 'kein Rundenende');
  assert.equal(end.winnerIndex, 0);
});

test('Ein komplettes Match gegen die Boss-KI laeuft bis zum Ende durch', () => {
  const m = new Match({ p1: 'le_binde', p2: 'mojo_bob', aiLevel: 5, roundTime: 20, seed: 42 });
  let steps = 0, matchEnd = null;
  while (!matchEnd && steps < 60 * 300) {
    const ev = m.step({ p1: { dx: 1, light: steps % 30 === 0, block: steps % 97 < 20 } });
    matchEnd = ev.find((e) => e.type === 'matchEnd');
    steps++;
  }
  assert.ok(matchEnd, 'Match endete nie');
  assert.ok(m.wins[0] + m.wins[1] >= 2, `Rundenstand ${m.wins}`);
});

test('Boss-KI trifft haerter als die leichteste Stufe', () => {
  const easy = new Match({ p1: 'uschi', p2: 'kalle', aiLevel: 0, seed: 9 });
  const boss = new Match({ p1: 'uschi', p2: 'kalle', aiLevel: 5, seed: 9 });
  assert.ok(boss.p2.heavy > easy.p2.heavy);
  assert.ok(Math.abs(boss.p2.heavy / easy.p2.heavy - 1.6 / 0.6) < 0.001);
});

test('Kaempfer bleiben in der Arena und stehen nicht ineinander', () => {
  const m = new Match({ p1: 'tetrapak', p2: 'dieter', p2IsAI: false, seed: 11 });
  for (let i = 0; i < 600; i++) m.step({ p1: { dx: 1, dz: 1 }, p2: { dx: -1 } });
  const r = K.arenaRadius;
  for (const f of [m.p1, m.p2]) {
    assert.ok(Math.abs(f.x) <= r, `x ausserhalb: ${f.x}`);
    assert.ok(Math.abs(f.z) <= r, `z ausserhalb: ${f.z}`);
  }
  const d = Math.hypot(m.p1.x - m.p2.x, m.p1.z - m.p2.z);
  assert.ok(d >= (m.p1.radius + m.p2.radius) * 0.9, `Abstand zu klein: ${d}`);
});
