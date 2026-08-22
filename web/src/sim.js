/**
 * sim.js — die komplette Kampflogik, ohne DOM und ohne Zeichnen.
 *
 * Bewusst getrennt, damit sie in Node getestet werden kann
 * (`node --test web/test/`). Feste Schrittweite: 1/60 s, wie in Unity.
 */

import { K, STATURE, AI_LEVELS, byId } from './data.js';

export const STEP = 1 / 60;

const clamp = (v, a, b) => Math.min(b, Math.max(a, v));
const dist = (a, b) => Math.hypot(a.x - b.x, a.z - b.z);

/** Kleiner deterministischer Zufall — Tests bleiben reproduzierbar. */
export function makeRandom(seed = 12345) {
  let s = seed >>> 0;
  return () => {
    s = (s * 1664525 + 1013904223) >>> 0;
    return s / 4294967296;
  };
}

export class Fighter {
  constructor(config, index, rng) {
    this.cfg = config;
    this.index = index;
    this.rng = rng;
    const st = STATURE[config.stature] || STATURE.normal;
    this.height = st.height;
    this.radius = st.radius;
    this.mass = st.mass;
    this.hitboxScale = st.hitbox;

    this.maxHP = config.maxHP;
    this.moveSpeed = config.moveSpeed;
    this.light = config.light;
    this.heavy = config.heavy;
    this.range = config.range;

    this.reset(index === 0 ? -4 : 4);
  }

  reset(x) {
    this.x = x;
    this.z = 0;
    this.y = 0;
    this.vy = 0;
    this.facing = this.index === 0 ? 1 : -1;
    this.hp = this.maxHP;
    this.blocking = false;
    this.attackTimer = 0;
    this.stun = 0;
    this.attacking = null;      // { kind, damage, activeAt, endsAt, hasHit }
    this.combo = 0;
    this.lastHit = -99;
    this.meter = 0;
    this.rollTimer = 0;
    this.rollCd = 0;
    this.invuln = 0;
    this.med = K.medCharges;
    this.medCd = 0;
    this.medTimer = 0;
    this.cooldowns = {};
    this.effects = {};          // { burn: {t, dps}, slow: {t}, ... }
    this.damageMul = 1;
    this.dead = false;
    this.flash = 0;
  }

  get grounded() { return this.y <= 0.001; }
  get busy() { return this.stun > 0 || this.attacking !== null || this.medTimer > 0; }
  get alive() { return this.hp > 0; }

  cooldown(key) { return this.cooldowns[key] || 0; }

  // ---------------- Eingaben ----------------

  move(dx, dz, dt) {
    if (this.busy || this.rollTimer > 0) return;
    const len = Math.hypot(dx, dz);
    if (len < 0.01) return;
    let speed = this.moveSpeed * (this.effects.slow ? 0.6 : 1);
    if (this.blocking) speed = K.strafeSpeed * K.blockMoveScale;
    this.x += (dx / len) * speed * dt;
    this.z += (dz / len) * speed * dt;
    const r = K.arenaRadius - this.radius;
    this.x = clamp(this.x, -r, r);
    this.z = clamp(this.z, -r, r);
  }

  jump() {
    if (!this.grounded || this.busy) return;
    this.vy = K.jumpForce;
  }

  roll(dx, dz) {
    if (this.rollCd > 0 || this.busy || this.rollTimer > 0) return false;
    const len = Math.hypot(dx, dz) || 1;
    this.rollDir = { x: (dx || this.facing) / len, z: dz / len };
    this.rollTimer = K.rollDuration;
    this.rollCd = K.rollCooldown;
    this.invuln = K.rollInvulnerable;
    return true;
  }

  attack(heavy) {
    if (this.attackTimer > 0 || this.busy || this.blocking) return false;
    const damage = heavy ? this.heavy : this.light;
    this.attackTimer = heavy ? K.heavyCooldown : K.lightCooldown;
    this.attacking = {
      kind: heavy ? 'heavy' : 'light',
      damage,
      activeAt: heavy ? 0.20 : 0.12,   // Startup wie in FighterController
      endsAt: heavy ? 0.45 : 0.32,
      t: 0,
      hasHit: false,
      range: this.range,
    };
    return true;
  }

  special(slot) {
    const sp = this.cfg.specials[slot];
    if (!sp || this.busy || this.cooldown(sp.key) > 0 || this.attackTimer > 0) return false;
    this.cooldowns[sp.key] = sp.cooldown;
    this.attacking = {
      kind: 'special',
      special: sp,
      damage: sp.damage,
      activeAt: 0.15,
      endsAt: 0.45,
      t: 0,
      hasHit: false,
      range: sp.range,
    };
    this.attackTimer = 0.5;
    if (sp.effect === 'dash') { this.x += this.facing * 2.5; }
    if (sp.effect === 'heal') { this.hp = Math.min(this.maxHP, this.hp + 12); }
    return true;
  }

  fatalBlow() {
    if (this.meter < K.fatalBlowMeterMax || this.busy) return false;
    this.meter = 0;
    const dmg = K.fatalBlowDamageMin + this.rng() * (K.fatalBlowDamageMax - K.fatalBlowDamageMin);
    this.attacking = {
      kind: 'fatal', damage: dmg, activeAt: 0.25, endsAt: 0.9, t: 0, hasHit: false,
      range: this.range + 1.5,
    };
    this.attackTimer = 1.0;
    return true;
  }

  useMed() {
    if (this.med <= 0 || this.medCd > 0 || this.busy) return false;
    this.med--;
    this.medCd = K.medCooldown;
    this.medTimer = K.medDuration;
    return true;
  }

  // ---------------- Schaden ----------------

  takeDamage(amount, fromX, attacker) {
    if (this.invuln > 0 || this.dead) return 0;

    let dmg = amount;
    if (this.blocking) {
      dmg *= K.blockDamageReduction;
      this.stun = Math.max(this.stun, 0.1);
    } else {
      this.stun = Math.max(this.stun, K.hitStun);
      this.combo = 0;
    }
    if (this.cfg.id === 'le_binde' && !this.blocking) dmg *= 0.4;  // Schmier-Schlüppa

    this.hp = Math.max(0, this.hp - dmg);
    this.meter = Math.min(K.fatalBlowMeterMax, this.meter + K.fatalBlowChargePerHitTaken);
    this.flash = 0.12;

    // Rückstoß, schwerere Kämpfer fliegen weniger weit
    const dir = Math.sign(this.x - fromX) || 1;
    this.x += (dir * K.knockback * 0.12) / this.mass;
    if (!this.blocking) this.vy = Math.max(this.vy, K.knockbackUp * 0.5);

    if (this.hp <= 0) this.dead = true;
    return dmg;
  }

  tick(dt) {
    if (this.attackTimer > 0) this.attackTimer -= dt;
    if (this.stun > 0) this.stun -= dt;
    if (this.invuln > 0) this.invuln -= dt;
    if (this.rollCd > 0) this.rollCd -= dt;
    if (this.medCd > 0) this.medCd -= dt;
    if (this.flash > 0) this.flash -= dt;

    for (const key of Object.keys(this.cooldowns)) {
      if (this.cooldowns[key] > 0) this.cooldowns[key] -= dt;
    }

    // Med-Kapsel
    if (this.medTimer > 0) {
      this.medTimer -= dt;
      if (this.medTimer <= 0) this.hp = Math.min(this.maxHP, this.hp + K.medHeal);
    }

    // Rolle
    if (this.rollTimer > 0) {
      const speed = K.rollDistance / K.rollDuration;
      this.x += this.rollDir.x * speed * dt;
      this.z += this.rollDir.z * speed * dt;
      this.rollTimer -= dt;
    }

    // Schwerkraft
    if (!this.grounded || this.vy > 0) {
      this.vy -= K.gravity * dt;
      this.y = Math.max(0, this.y + this.vy * dt);
      if (this.y === 0) this.vy = 0;
    }

    // Effekte über Zeit
    for (const [name, e] of Object.entries(this.effects)) {
      e.t -= dt;
      if (e.dps) this.hp = Math.max(0, this.hp - e.dps * dt);
      if (e.t <= 0) delete this.effects[name];
    }
    if (this.hp <= 0) this.dead = true;

    // Combo-Fenster
    if (this.combo > 0 && performanceNow() - this.lastHit > K.comboWindow) this.combo = 0;

    const r = K.arenaRadius - this.radius;
    this.x = clamp(this.x, -r, r);
    this.z = clamp(this.z, -r, r);
  }
}

let simClock = 0;
function performanceNow() { return simClock; }

export class Match {
  constructor(opts = {}) {
    this.rng = makeRandom(opts.seed ?? Date.now() % 100000);
    this.p1 = new Fighter(byId(opts.p1 || 'le_binde'), 0, this.rng);
    this.p2 = new Fighter(byId(opts.p2 || 'mojo_bob'), 1, this.rng);
    this.bestOf = opts.bestOf ?? K.defaultBestOfRounds;
    this.roundTime = opts.roundTime ?? K.defaultRoundTime;
    this.aiLevel = opts.aiLevel ?? 2;
    this.p2IsAI = opts.p2IsAI !== false;

    if (this.p2IsAI) {
      const level = AI_LEVELS[clamp(this.aiLevel, 0, AI_LEVELS.length - 1)];
      this.p2.light *= level.damage;
      this.p2.heavy *= level.damage;
      this.ai = { level, think: 0, plan: [], step: 0 };
    }

    this.wins = [0, 0];
    this.round = 1;
    this.timer = this.roundTime;
    this.state = 'fight';       // fight | roundEnd | matchEnd
    this.stateTimer = 0;
    this.winner = null;
    this.events = [];           // { type, ... } — für Ton und Effekte
    simClock = 0;
  }

  emit(type, data = {}) { this.events.push({ type, ...data }); }

  /** Ein Simulationsschritt. `input` = Steuerung von Spieler 1 (und ggf. 2). */
  step(input, dt = STEP) {
    simClock += dt;
    this.events.length = 0;

    if (this.state === 'fight') {
      this.applyInput(this.p1, input.p1);
      if (this.p2IsAI) this.thinkAI(dt);
      else this.applyInput(this.p2, input.p2 || {});

      this.p1.tick(dt);
      this.p2.tick(dt);
      this.faceEachOther();
      this.resolveAttack(this.p1, this.p2);
      this.resolveAttack(this.p2, this.p1);
      this.separate();

      this.timer -= dt;
      if (this.timer <= 0 || this.p1.dead || this.p2.dead) this.endRound();
    } else {
      this.stateTimer -= dt;
      if (this.stateTimer <= 0 && this.state === 'roundEnd') this.startRound();
    }
    return this.events;
  }

  applyInput(f, i = {}) {
    if (!f.alive) return;
    f.blocking = !!i.block && f.grounded && !f.busy;
    f.move(i.dx || 0, i.dz || 0, STEP);
    if (i.jump) f.jump();
    if (i.roll && f.roll(i.dx || 0, i.dz || 0)) this.emit('roll', { f });
    if (i.light && f.attack(false)) this.emit('swing', { f, heavy: false });
    if (i.heavy && f.attack(true)) this.emit('swing', { f, heavy: true });
    if (i.special1 && f.special(0)) this.emit('special', { f, slot: 0 });
    if (i.special2 && f.special(1)) this.emit('special', { f, slot: 1 });
    if (i.fatal && f.fatalBlow()) this.emit('fatal', { f });
    if (i.med && f.useMed()) this.emit('med', { f });
  }

  thinkAI(dt) {
    const ai = this.ai;
    const me = this.p2, foe = this.p1;
    if (!me.alive) return;

    ai.think -= dt;
    const d = dist(me, foe);

    // Dauerhafte Annäherung, Entscheidungen im Denkintervall
    const towards = { x: foe.x - me.x, z: foe.z - me.z };
    if (d > me.range * 0.9) me.move(towards.x, towards.z, dt);

    if (ai.think > 0) return;
    ai.think = ai.level.thinkMin + this.rng() * (ai.level.thinkMax - ai.level.thinkMin);

    if (d <= me.range + 0.4) {
      const r = this.rng();
      if (r < ai.level.special) { if (me.special(this.rng() < 0.5 ? 0 : 1)) this.emit('special', { f: me }); }
      else if (r < ai.level.special + ai.level.aggression * 0.6) {
        const heavy = this.rng() < ai.level.combo;
        if (me.attack(heavy)) this.emit('swing', { f: me, heavy });
      } else if (this.rng() < ai.level.block) me.blocking = true;
      else me.blocking = false;
    } else {
      me.blocking = false;
      if (me.hp < me.maxHP * 0.35 && this.rng() < 0.5 && me.useMed()) this.emit('med', { f: me });
    }
  }

  resolveAttack(attacker, target) {
    const a = attacker.attacking;
    if (!a) return;
    a.t += STEP;

    if (!a.hasHit && a.t >= a.activeAt) {
      const d = dist(attacker, target);
      const reach = a.range * attacker.hitboxScale + target.radius;
      const inFront = Math.sign(target.x - attacker.x) === attacker.facing || Math.abs(target.x - attacker.x) < 0.6;
      const heightOk = Math.abs(attacker.y - target.y) < 1.6;

      if (d <= reach && inFront && heightOk) {
        a.hasHit = true;
        let dmg = a.damage * attacker.damageMul;

        // Mojo Bobs Krit (15 %, ×2,4)
        let crit = false;
        if (attacker.cfg.id === 'mojo_bob' && this.rng() < 0.15) { dmg *= 2.4; crit = true; }

        const blocked = target.blocking && !(a.special && a.special.effect === 'pierce');
        const dealt = target.takeDamage(blocked ? dmg : dmg, attacker.x, attacker);

        attacker.combo = simClock - attacker.lastHit < K.comboWindow ? attacker.combo + 1 : 1;
        attacker.lastHit = simClock;
        attacker.meter = Math.min(K.fatalBlowMeterMax, attacker.meter + K.fatalBlowChargePerHit);

        // Spezialeffekte
        const eff = a.special && a.special.effect;
        if (eff === 'burn') target.effects.burn = { t: 4, dps: 2 };
        if (eff === 'poison') target.effects.poison = { t: 5, dps: 1.5 };
        if (eff === 'dot') target.effects.dot = { t: 6, dps: 1 };
        if (eff === 'bleed') target.effects.bleed = { t: 5, dps: 1 };
        if (eff === 'slow') target.effects.slow = { t: 3 };
        if (eff === 'stun') target.stun = Math.max(target.stun, 0.6);

        this.emit('hit', {
          attacker, target, damage: dealt, blocked: target.blocking, crit,
          kind: a.kind, combo: attacker.combo,
        });
        if (target.dead) this.emit('ko', { target });
      }
    }

    if (a.t >= a.endsAt) attacker.attacking = null;
  }

  faceEachOther() {
    this.p1.facing = this.p2.x >= this.p1.x ? 1 : -1;
    this.p2.facing = this.p1.x >= this.p2.x ? 1 : -1;
  }

  /** Kämpfer sollen nicht ineinander stehen — schwerere schieben leichtere. */
  separate() {
    const d = dist(this.p1, this.p2);
    const min = this.p1.radius + this.p2.radius;
    if (d >= min || d === 0) return;
    const push = (min - d) / 2;
    const nx = (this.p1.x - this.p2.x) / d;
    const nz = (this.p1.z - this.p2.z) / d;
    const total = this.p1.mass + this.p2.mass;
    this.p1.x += nx * push * (this.p2.mass / total) * 2;
    this.p1.z += nz * push * (this.p2.mass / total) * 2;
    this.p2.x -= nx * push * (this.p1.mass / total) * 2;
    this.p2.z -= nz * push * (this.p1.mass / total) * 2;
  }

  endRound() {
    let winnerIndex = null;
    if (this.p1.hp > this.p2.hp) winnerIndex = 0;
    else if (this.p2.hp > this.p1.hp) winnerIndex = 1;

    if (winnerIndex !== null) this.wins[winnerIndex]++;
    this.emit('roundEnd', { winnerIndex, wins: [...this.wins] });

    const needed = Math.ceil(this.bestOf / 2);
    if (this.wins[0] >= needed || this.wins[1] >= needed) {
      this.state = 'matchEnd';
      this.winner = this.wins[0] > this.wins[1] ? this.p1 : this.p2;
      this.emit('matchEnd', { winner: this.winner });
    } else if (this.round >= this.bestOf) {
      this.state = 'matchEnd';
      this.winner = this.wins[0] === this.wins[1] ? null : (this.wins[0] > this.wins[1] ? this.p1 : this.p2);
      this.emit('matchEnd', { winner: this.winner });
    } else {
      this.state = 'roundEnd';
      this.stateTimer = K.roundEndDelay;
    }
  }

  startRound() {
    this.round++;
    this.p1.reset(-4);
    this.p2.reset(4);
    this.timer = this.roundTime;
    this.state = 'fight';
    this.emit('roundStart', { round: this.round });
  }
}
