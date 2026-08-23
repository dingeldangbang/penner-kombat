/**
 * fighter.js — AIConfigurableFighter
 *
 * Nimmt ein hochgeladenes GLB (oder einen prozeduralen Platzhalter) und bindet
 * zur Laufzeit die von der KI gelieferten JSON-Konfigurationsblöcke daran:
 * Move-Katalog, Framedaten, VFX/SFX/Hitbox-Referenzen, Optik, Werte.
 *
 * Wichtig: Hier läuft niemals KI-Code — nur validierte Daten aus schema.js.
 */

import * as THREE from 'three';
import { HITBOX_LIBRARY, VFX_LIBRARY } from './assetLibrary.js';
import * as AudioPool from './audioPool.js';
import { InputBuffer, bestMatch } from './inputBuffer.js';
import { validateConfig } from './schema.js';

const FRAME = 1 / 60;

export class AIConfigurableFighter {
  /**
   * @param {THREE.Object3D} glbScene   Modellwurzel
   * @param {{particles:import('./vfx.js').ParticleSystem, scene:THREE.Scene, clips?:THREE.AnimationClip[]}} deps
   */
  constructor(glbScene, deps = {}) {
    this.model = glbScene;
    this.scene = deps.scene;
    this.particles = deps.particles;

    this.moveCatalog = {};      // name -> {kind, sequence, damage, startup, active, recovery, vfxType, sfx, hitbox, ...}
    this.cinematics = {};       // X-Ray-/Kino-Moves (Meter-gebunden)
    this.fatalities = {};       // Finisher, nur im FINISH-HIM-Modus
    this.meter = 0;             // 0..100, füllt sich durch Treffer
    this.finisherMode = false;  // "FINISH HIM!"
    this.impactProfile = { lightHitstopFrames: 2, heavyHitstopFrames: 5, shakeStrength: 0.8, zoomPunch: true };
    this.stats = { maxHP: 100, moveSpeed: 5, defense: 1, jumpForce: 9.5 };
    this.hp = this.stats.maxHP;

    this.inputBuffer = new InputBuffer(16, 700);
    this.state = { phase: 'idle', move: null, timer: 0, hitApplied: false };
    this.aura = null;
    this.baseColors = new Map();
    this.hitboxHelper = null;
    this.targetTween = null;   // Reaktion des Gegners (Sog, Wandbounce, Knockdown)
    this.profileName = '';
    this.archetype = '';
    this.showHitboxes = false;
    this.listeners = {};

    // Animationen aus dem GLB (falls vorhanden)
    this.mixer = deps.clips && deps.clips.length ? new THREE.AnimationMixer(glbScene) : null;
    this.clips = {};
    (deps.clips || []).forEach((c) => { this.clips[c.name] = c; });
    this.currentAction = null;

    this.#cacheMaterials();
  }

  on(evt, fn) { (this.listeners[evt] ||= []).push(fn); return this; }
  emit(evt, payload) { (this.listeners[evt] || []).forEach((f) => f(payload)); }

  #cacheMaterials() {
    this.model.traverse((child) => {
      if (child.isMesh && child.material) {
        const mats = Array.isArray(child.material) ? child.material : [child.material];
        mats.forEach((m) => {
          if (!this.baseColors.has(m)) {
            this.baseColors.set(m, {
              color: m.color ? m.color.clone() : null,
              emissive: m.emissive ? m.emissive.clone() : null,
              emissiveIntensity: m.emissiveIntensity ?? 1,
              metalness: m.metalness ?? 0,
              roughness: m.roughness ?? 1,
              wireframe: !!m.wireframe,
            });
          }
        });
      }
    });
    this.baseScale = this.model.scale.clone();
  }

  // -------------------------------------------------------------------------
  //  Der Live-Konfigurator
  // -------------------------------------------------------------------------

  /**
   * Wird direkt aufgerufen, sobald die KI-Schnittstelle validiertes JSON liefert.
   * @param {object} cfg Ergebnis von validateConfig().config
   * @returns {string[]} Liste der Änderungen (fürs Chat-Log)
   */
  applyAiConfiguration(cfg) {
    const changes = [];
    if (!cfg) return changes;
    if (cfg.profileName) {
      this.profileName = cfg.profileName;
      this.archetype = cfg.archetype || '';
      changes.push(`Profil „${cfg.profileName}"${cfg.archetype ? ' — ' + cfg.archetype : ''}`);
    }

    // 1) Specials binden
    for (const s of cfg.specialAttacks || []) {
      this.moveCatalog[s.name] = {
        kind: 'special',
        sequence: s.inputSequence,
        damage: s.damage,
        startup: s.startupFrames,
        active: s.activeFrames,
        recovery: s.recoveryFrames,
        meterCost: s.meterCost,
        vfxType: s.vfxAsset,          // Asset-String -> Preloader-Pool
        sfx: s.sfxAsset,
        hitbox: s.hitboxShape,
        projectile: s.projectile,
        status: s.statusEffect,
        spawnAt: s.spawnAt || 'self',
        guardBreak: !!s.guardBreak,
        pullStrength: s.pullStrength || 0,
        clip: s.animationClipName,
      };
      changes.push(`Special „${s.name}" (${s.inputSequence.join(' → ')}, ${s.damage} DMG, ${s.vfxAsset})`);
    }

    // 2) Combos binden
    for (const c of cfg.combos || []) {
      this.moveCatalog[c.name] = {
        kind: 'combo',
        sequence: c.inputSequence,
        damage: c.totalDamage,
        hits: c.hits,
        startup: c.startupFrames ?? 6,
        active: Math.max(8, c.hits * 6),
        recovery: c.recoveryFrames ?? 10,
        hitStun: c.hitStunFrames ?? 12,
        wallBounce: !!c.wallBounce,
        status: c.statusEffect || 'none',
        vfxType: c.vfxAsset || null,
        sfx: c.sfxAsset,
        hitbox: c.hitboxShape,
        cancelWindow: c.cancelWindowFrames,
        clip: c.animationClipName,
      };
      changes.push(`Kombo „${c.name}" (${c.inputSequence.join(' → ')}, ${c.totalDamage} DMG auf ${c.hits} Treffer)`);
    }

    // 3) Optik-Overrides
    if (cfg.visualOverrides) {
      const v = cfg.visualOverrides;
      this.model.traverse((child) => {
        if (!child.isMesh || !child.material) return;
        const mats = Array.isArray(child.material) ? child.material : [child.material];
        mats.forEach((m) => {
          if (v.tintColor != null && m.color) m.color.setHex(v.tintColor);
          if (v.emissiveColor != null && m.emissive) m.emissive.setHex(v.emissiveColor);
          if (v.emissiveIntensity != null && 'emissiveIntensity' in m) m.emissiveIntensity = v.emissiveIntensity;
          if (v.metalness != null && 'metalness' in m) m.metalness = v.metalness;
          if (v.roughness != null && 'roughness' in m) m.roughness = v.roughness;
          if (v.wireframe != null) m.wireframe = v.wireframe;
          m.needsUpdate = true;
        });
      });
      if (v.scale != null) this.model.scale.copy(this.baseScale).multiplyScalar(v.scale);
      if (v.auraVfx) this.aura = v.auraVfx;
      changes.push('Optik: ' + Object.entries(v).map(([k, val]) => `${k}=${typeof val === 'number' && /color/i.test(k) ? '#' + val.toString(16).padStart(6, '0') : val}`).join(', '));
    }

    // 4) Werte
    if (cfg.stats) {
      Object.assign(this.stats, cfg.stats);
      this.hp = Math.min(this.hp, this.stats.maxHP);
      if (cfg.stats.maxHP) this.hp = this.stats.maxHP;
      changes.push('Werte: ' + Object.entries(cfg.stats).map(([k, v]) => `${k}=${v}`).join(', '));
    }

    // 4b) X-Ray-/Cinematic-Moves
    for (const c of cfg.cinematicMoves || []) {
      this.cinematics[c.name] = { ...c, kind: 'cinematic' };
      changes.push(`X-Ray „${c.name}" (${c.inputSequence.join(' → ')}, ${c.damage} DMG, ${c.boneTarget}, Slow-Mo ${c.slowMotionFactor})`);
    }

    // 4c) Fatalities
    for (const f of cfg.fatalities || []) {
      this.fatalities[f.name] = { ...f, kind: 'fatality' };
      changes.push(`Fatality „${f.name}" (${f.inputSequence.join(' → ')}, ${f.finisherType}, ${f.distance})`);
    }

    // 4d) Stage-Interaktionen (die Arena baut sie, wir merken sie uns)
    if (cfg.stageInteractions) {
      this.stageInteractions = cfg.stageInteractions;
      changes.push('Arena-Objekte: ' + cfg.stageInteractions.map((o) => `${o.object} (${o.role})`).join(', '));
      this.emit('stage', { interactions: cfg.stageInteractions });
    }

    // 4e) Wucht-Profil
    if (cfg.impactProfile) {
      this.impactProfile = { ...this.impactProfile, ...cfg.impactProfile };
      changes.push(`Hitstop: leicht ${this.impactProfile.lightHitstopFrames}f, schwer ${this.impactProfile.heavyHitstopFrames}f`);
    }

    // 5) Entfernen
    for (const name of cfg.removeMoves || []) {
      for (const table of [this.moveCatalog, this.cinematics, this.fatalities]) {
        const key = Object.keys(table).find((k) => k.toLowerCase() === String(name).toLowerCase());
        if (key) { delete table[key]; changes.push(`„${key}" entfernt`); }
      }
    }

    this.emit('configured', { cfg, changes, catalog: this.moveCatalog });
    return changes;
  }

  /** Kompletter Katalog als JSON (Speichern/Teilen). */
  exportConfiguration() {
    const specials = [], combos = [];
    for (const [name, d] of Object.entries(this.moveCatalog)) {
      if (d.kind === 'special') {
        specials.push({
          name, inputSequence: d.sequence, vfxAsset: d.vfxType, sfxAsset: d.sfx,
          hitboxShape: d.hitbox, damage: d.damage, startupFrames: d.startup,
          activeFrames: d.active, recoveryFrames: d.recovery, meterCost: d.meterCost,
          projectile: d.projectile, statusEffect: d.status, spawnAt: d.spawnAt,
          guardBreak: d.guardBreak, pullStrength: d.pullStrength, animationClipName: d.clip,
        });
      } else {
        combos.push({
          name, inputSequence: d.sequence, totalDamage: d.damage, hits: d.hits,
          animationClipName: d.clip, hitboxShape: d.hitbox, sfxAsset: d.sfx,
          vfxAsset: d.vfxType || undefined, cancelWindowFrames: d.cancelWindow,
          startupFrames: d.startup, hitStunFrames: d.hitStun,
          recoveryFrames: d.recovery, wallBounce: d.wallBounce, statusEffect: d.status,
        });
      }
    }
    const cinematicMoves = Object.values(this.cinematics).map(({ kind, ...c }) => c);
    const fatalities = Object.values(this.fatalities).map(({ kind, ...f }) => f);
    return {
      requestType: 'BATCH',
      profileName: this.profileName || undefined,
      archetype: this.archetype || undefined,
      combos, specialAttacks: specials, stats: { ...this.stats },
      cinematicMoves: cinematicMoves.length ? cinematicMoves : undefined,
      fatalities: fatalities.length ? fatalities : undefined,
      stageInteractions: this.stageInteractions || undefined,
      impactProfile: { ...this.impactProfile },
    };
  }

  // -------------------------------------------------------------------------
  //  Eingaben & Ausführung
  // -------------------------------------------------------------------------

  pushInput(token) {
    if (!token) return null;
    this.inputBuffer.push(token);
    this.emit('input', { token, buffer: this.inputBuffer.tokens() });
    return this.checkInputs();
  }

  /**
   * Prüft, ob der Puffer eine KI-generierte Sequenz abschließt.
   * Reihenfolge: Fatality (nur im Finisher-Modus) → X-Ray (nur mit Meter) → Move.
   */
  checkInputs() {
    if (this.state.phase !== 'idle') return null;
    const tokens = this.inputBuffer.recent(6);

    if (this.finisherMode) {
      const fat = bestMatch(tokens, this.#seqTable(this.fatalities));
      if (fat) {
        this.inputBuffer.consume();
        this.executeFatality(fat.name, this.fatalities[fat.name]);
        return fat.name;
      }
    }

    const cine = bestMatch(tokens, this.#seqTable(this.cinematics));
    if (cine) {
      const data = this.cinematics[cine.name];
      if (this.canTriggerCinematic(data)) {
        this.inputBuffer.consume();
        this.executeCinematic(cine.name, data);
        return cine.name;
      }
      this.emit('blocked', {
        name: cine.name,
        reason: data.triggerCondition === 'METERS_FULL'
          ? `Leiste erst bei 100 % (aktuell ${Math.round(this.meter)} %)`
          : data.triggerCondition === 'LOW_HEALTH' ? 'Nur unter 30 % HP' : 'Bedingung nicht erfüllt',
      });
    }

    const hit = bestMatch(tokens, this.moveCatalog);
    if (!hit) return null;
    this.inputBuffer.consume();
    this.executeSpecialMove(hit.name, hit.data);
    return hit.name;
  }

  #seqTable(table) {
    return Object.fromEntries(Object.entries(table).map(([k, v]) => [k, { sequence: v.inputSequence }]));
  }

  /** Bedingung des X-Ray-Moves erfüllt? */
  canTriggerCinematic(data) {
    switch (data.triggerCondition) {
      case 'METERS_FULL': return this.meter >= 100;
      case 'LOW_HEALTH': return this.hp / this.stats.maxHP <= 0.3;
      case 'COUNTER_HIT': return this.state.phase === 'idle';
      default: return true;
    }
  }

  /** Meter füllen (Treffer geben, Treffer kassieren). */
  addMeter(amount) {
    const before = this.meter;
    this.meter = Math.max(0, Math.min(100, this.meter + amount));
    if (this.meter !== before) this.emit('meter', { meter: this.meter, full: this.meter >= 100 });
    return this.meter;
  }

  /** „FINISH HIM!" ein-/ausschalten. */
  setFinisherMode(on) {
    this.finisherMode = !!on;
    this.emit('finisher-mode', { active: this.finisherMode, moves: Object.keys(this.fatalities) });
  }

  /** X-Ray/Cinematic: Zeitlupe, Kamerafahrt, Knochenbruch. */
  executeCinematic(name, data) {
    this.meter = 0;
    this.emit('meter', { meter: 0, full: false });
    this.state = { phase: 'cinematic', move: { name, ...data, kind: 'cinematic' }, timer: data.durationFrames / 60, hitApplied: false };
    this.playClip(data.animationClipName);
    if (data.sfxAsset) AudioPool.play(data.sfxAsset);
    AudioPool.play('slowmo_drone');
    this.emit('cinematic', {
      name, kind: 'xray', data,
      boneTarget: data.boneTarget,
      cameraPath: data.cameraPath,
      slowMotionFactor: data.slowMotionFactor,
      zoomFrame: data.cinematicZoomFrame,
      durationFrames: data.durationFrames,
      damage: data.damage,
    });
    return name;
  }

  /** Fatality: Finisher-Kamera, Gore, Ragdoll. */
  executeFatality(name, data) {
    this.state = { phase: 'fatality', move: { name, ...data, kind: 'fatality' }, timer: data.durationFrames / 60, hitApplied: false };
    this.playClip(data.animationClipName);
    if (data.sfxAsset) AudioPool.play(data.sfxAsset);
    this.emit('fatality', {
      name, kind: 'fatality', data,
      finisherType: data.finisherType,
      cameraPath: data.cameraPath,
      slowMotionFactor: data.slowMotionFactor,
      durationFrames: data.durationFrames,
      vfxExplosionAsset: data.vfxExplosionAsset,
      ragdoll: data.ragdoll,
    });
    return name;
  }

  executeSpecialMove(name, moveData) {
    this.state = { phase: 'startup', move: { name, ...moveData }, timer: moveData.startup * FRAME, hitApplied: false };
    this.playClip(moveData.clip);
    if (moveData.sfx) AudioPool.play(moveData.sfx);
    this.emit('move', { name, phase: 'startup', data: moveData });
  }

  playClip(clipName) {
    if (!this.mixer || !clipName) return false;
    const clip = this.clips[clipName]
      || Object.values(this.clips).find((c) => c.name.toLowerCase().includes(String(clipName).toLowerCase()));
    if (!clip) return false;
    const action = this.mixer.clipAction(clip);
    if (this.currentAction && this.currentAction !== action) this.currentAction.fadeOut(0.15);
    action.reset().setLoop(THREE.LoopOnce, 1).fadeIn(0.12).play();
    action.clampWhenFinished = true;
    this.currentAction = action;
    return true;
  }

  playIdle() {
    if (!this.mixer) return;
    const idle = this.clips.idle || Object.values(this.clips).find((c) => /idle|stand/i.test(c.name)) || Object.values(this.clips)[0];
    if (!idle) return;
    const action = this.mixer.clipAction(idle);
    if (this.currentAction && this.currentAction !== action) this.currentAction.fadeOut(0.2);
    action.reset().setLoop(THREE.LoopRepeat, Infinity).fadeIn(0.2).play();
    this.currentAction = action;
  }

  /** Frame-Update: Framedaten abarbeiten, VFX/Hitbox zum aktiven Fenster spawnen. */
  update(dt, target) {
    if (this.mixer) this.mixer.update(dt);
    this.#updateTween(dt);
    if (this.aura && this.particles && Math.random() < dt * 6) {
      this.particles.spawn(this.aura, this.model.position.clone().setY(1.0), { scale: 0.25 });
    }
    if (this.state.phase === 'idle') return;

    this.state.timer -= dt;

    // Kino-Phasen laufen als Ganzes ab; der Treffer sitzt beim Zoom-Frame
    if (this.state.phase === 'cinematic' || this.state.phase === 'fatality') {
      const mv = this.state.move;
      const elapsed = mv.durationFrames / 60 - this.state.timer;
      if (!this.state.hitApplied && elapsed >= (mv.cinematicZoomFrame ?? 8) / 60) {
        this.state.hitApplied = true;
        if (this.particles && target) {
          this.particles.spawn(mv.vfxAsset || mv.vfxExplosionAsset || 'bone_shards',
            target.position.clone().setY(1.2), { scale: 1.1 });
        }
        this.emit('hit', {
          name: mv.name,
          damage: mv.damage ?? 0,
          status: 'knockdown',
          hits: 1,
          cinematic: this.state.phase,
          boneTarget: mv.boneTarget,
          hitStun: 30,
          guardBreak: true,
          wallBounce: false,
        });
      }
      if (this.state.timer <= 0) {
        const finished = this.state.phase;
        this.state = { phase: 'idle', move: null, timer: 0, hitApplied: false };
        this.playIdle();
        this.emit('move', { name: mv.name, phase: 'idle', data: mv });
        this.emit(finished === 'fatality' ? 'fatality-end' : 'cinematic-end', { name: mv.name });
      }
      return;
    }

    if (this.state.timer > 0) return;
    const mv = this.state.move;

    if (this.state.phase === 'startup') {
      this.state.phase = 'active';
      this.state.timer = mv.active * FRAME;
      this.#spawnEffects(mv, target);
    } else if (this.state.phase === 'active') {
      this.state.phase = 'recovery';
      this.state.timer = (mv.recovery || 12) * FRAME;
      this.hideHitbox();
      this.emit('move', { name: mv.name, phase: 'recovery', data: mv });
    } else {
      this.state = { phase: 'idle', move: null, timer: 0, hitApplied: false };
      this.playIdle();
      this.emit('move', { name: mv.name, phase: 'idle', data: mv });
    }
  }

  #spawnEffects(mv, target) {
    const origin = this.model.position.clone();
    const dir = target
      ? target.position.clone().sub(origin).setY(0).normalize()
      : new THREE.Vector3(1, 0, 0);

    // Wo entsteht die Wirkung? self | target | ground_target
    let spawnAt;
    if (mv.spawnAt === 'target' && target) spawnAt = target.position.clone().setY(1.15);
    else if (mv.spawnAt === 'ground_target' && target) spawnAt = target.position.clone().setY(0.12);
    else spawnAt = origin.clone().add(dir.clone().multiplyScalar(0.8)).setY(1.15);

    if (mv.vfxType && this.particles) {
      this.particles.spawn(mv.vfxType, spawnAt, { direction: mv.spawnAt === 'self' ? dir : dir.clone().negate() });
    } else if (this.particles && mv.kind === 'combo') {
      this.particles.spawn('dust_burst', spawnAt, { direction: dir, scale: 0.5 });
    }
    this.showHitbox(mv.hitbox, spawnAt, dir);
    this.emit('move', { name: mv.name, phase: 'active', data: mv });

    // Trefferauswertung gegen das Dummy-Ziel
    if (target) {
      const range = this.#hitRange(mv.hitbox);
      const dist = target.position.distanceTo(origin);
      if (dist <= range) {
        const dmg = (mv.damage || 0);
        this.reactTarget(mv, target, dir);
        this.addMeter(mv.kind === 'combo' ? 6 + (mv.hits || 1) * 2 : 12);
        this.emit('hit', {
          name: mv.name, damage: dmg, status: mv.status, hits: mv.hits || 1,
          guardBreak: !!mv.guardBreak, wallBounce: !!mv.wallBounce,
          hitStun: mv.hitStun || 0, spawnAt: mv.spawnAt || 'self',
        });
        if (this.particles) {
          this.particles.spawn(mv.status === 'burn' ? 'fire_blast' : 'blood_splatter',
            target.position.clone().setY(1.1), { scale: 0.5 });
        }
        AudioPool.play('impact_flesh');
      }
    }
  }

  /** Sog, Wandbounce, Knockdown: bewegt den Gegner sichtbar. */
  reactTarget(mv, target, dir) {
    if (!target) return;
    const from = target.position.clone();
    let to = from.clone();
    let tilt = 0;

    if (mv.status === 'pull' || mv.pullStrength > 0) {
      const strength = Math.min(mv.pullStrength || 6, from.distanceTo(this.model.position) - 1.2);
      to = from.clone().sub(dir.clone().multiplyScalar(Math.max(0, strength)));
    } else if (mv.wallBounce || mv.status === 'wallbounce' || mv.status === 'launch') {
      to = from.clone().add(dir.clone().multiplyScalar(2.4));
      tilt = 0.5;
    } else if (mv.status === 'knockdown') {
      to = from.clone().add(dir.clone().multiplyScalar(0.8));
      tilt = 1.35;
    } else {
      to = from.clone().add(dir.clone().multiplyScalar(0.35));
    }
    // Innerhalb der Arena bleiben
    to.clampLength(0, 9.5);
    this.targetTween = { obj: target, from, to, tilt, t: 0, dur: mv.status === 'pull' ? 0.28 : 0.22, back: true };
  }

  #updateTween(dt) {
    const tw = this.targetTween;
    if (!tw) return;
    tw.t += dt;
    const k = Math.min(1, tw.t / tw.dur);
    const ease = 1 - Math.pow(1 - k, 3);
    tw.obj.position.lerpVectors(tw.from, tw.to, ease);
    if (tw.tilt) tw.obj.rotation.z = Math.sin(ease * Math.PI) * -tw.tilt;
    if (k >= 1) {
      if (tw.back && tw.tilt) { tw.obj.rotation.z = 0; }
      this.targetTween = null;
    }
  }

  #hitRange(shape) {
    const def = HITBOX_LIBRARY[shape] || HITBOX_LIBRARY.medium_sphere;
    if (def.shape === 'cone') return def.length;
    if (def.shape === 'box') return def.size[0];
    if (def.shape === 'ring') return def.radius;
    return def.radius + 1.2;
  }

  showHitbox(shape, at, dir) {
    if (!this.showHitboxes || !this.scene) return;
    this.hideHitbox();
    const def = HITBOX_LIBRARY[shape] || HITBOX_LIBRARY.medium_sphere;
    let geo;
    if (def.shape === 'cone') geo = new THREE.ConeGeometry(def.radius, def.length, 10, 1, true);
    else if (def.shape === 'box') geo = new THREE.BoxGeometry(...def.size);
    else if (def.shape === 'ring') geo = new THREE.TorusGeometry(def.radius, 0.08, 6, 20);
    else geo = new THREE.SphereGeometry(def.radius, 10, 8);
    const mesh = new THREE.Mesh(geo, new THREE.MeshBasicMaterial({ color: 0xff2244, wireframe: true, transparent: true, opacity: 0.75 }));
    mesh.position.copy(at);
    if (def.shape === 'cone') {
      mesh.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), dir.clone().normalize());
      mesh.position.add(dir.clone().multiplyScalar(def.length * 0.4));
    }
    if (def.shape === 'ring') { mesh.rotation.x = Math.PI / 2; mesh.position.copy(this.model.position).setY(0.15); }
    this.scene.add(mesh);
    this.hitboxHelper = mesh;
  }

  hideHitbox() {
    if (this.hitboxHelper && this.scene) {
      this.scene.remove(this.hitboxHelper);
      this.hitboxHelper.geometry.dispose();
      this.hitboxHelper.material.dispose();
      this.hitboxHelper = null;
    this.targetTween = null;   // Reaktion des Gegners (Sog, Wandbounce, Knockdown)
    this.profileName = '';
    this.archetype = '';
    }
  }

  /** Setzt Optik & Katalog auf den Ausgangszustand zurück. */
  reset() {
    this.moveCatalog = {};
    this.cinematics = {};
    this.fatalities = {};
    this.stageInteractions = null;
    this.meter = 0;
    this.finisherMode = false;
    this.profileName = '';
    this.archetype = '';
    this.aura = null;
    this.stats = { maxHP: 100, moveSpeed: 5, defense: 1, jumpForce: 9.5 };
    this.hp = this.stats.maxHP;
    this.model.scale.copy(this.baseScale);
    for (const [m, base] of this.baseColors) {
      if (base.color && m.color) m.color.copy(base.color);
      if (base.emissive && m.emissive) m.emissive.copy(base.emissive);
      if ('emissiveIntensity' in m) m.emissiveIntensity = base.emissiveIntensity;
      if ('metalness' in m) m.metalness = base.metalness;
      if ('roughness' in m) m.roughness = base.roughness;
      m.wireframe = base.wireframe;
      m.needsUpdate = true;
    }
    this.hideHitbox();
    this.emit('configured', { cfg: null, changes: ['Zurückgesetzt'], catalog: this.moveCatalog });
  }
}

/**
 * Alias-API: `injectAiConfiguration()` ist identisch zu `applyAiConfiguration()`.
 * Damit lassen sich Dokumentations-Snippets und die Profile aus presets.js
 * unverändert einspeisen.
 */
AIConfigurableFighter.prototype.injectAiConfiguration = function injectAiConfiguration(cfg) {
  // Nimmt rohes JSON (Objekt oder String) entgegen und härtet es zuerst.
  const res = validateConfig(cfg);
  if (!res.ok) {
    const msg = 'injectAiConfiguration: ' + [...res.errors, ...res.warnings].join(' ');
    this.emit('configured', { cfg: null, changes: [msg], catalog: this.moveCatalog });
    return [msg];
  }
  return this.applyAiConfiguration(res.config);
};

/** Zweitname der Engine, wie in der Doku verwendet. */
export { AIConfigurableFighter as LiveFighterEngine };

export const VFX_NAMES = Object.keys(VFX_LIBRARY);
