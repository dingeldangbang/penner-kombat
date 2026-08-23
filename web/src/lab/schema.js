/**
 * schema.js — Validierung & Reparatur der KI-Ausgabe
 *
 * Die KI liefert Text. Bevor irgendetwas in die Game-Loop wandert, wird der
 * JSON-Block hier hart validiert, geklemmt und auf existierende Assets gemappt.
 * Es gibt keinen Pfad, auf dem KI-Text als Code ausgeführt wird — nur Daten.
 */

import {
  INPUT_TOKENS, STATUS_EFFECTS, SPAWN_POINTS, ANIMATION_LIBRARY, resolveAsset,
  BONE_TARGETS, TRIGGER_CONDITIONS, CAMERA_PATHS, FINISHER_TYPES, FINISHER_DISTANCES,
  STAGE_OBJECTS, STAGE_ROLES,
} from './assetLibrary.js';

export const REQUEST_TYPES = [
  'UPDATE_ABILITIES',
  'UPDATE_VISUALS',
  'UPDATE_STATS',
  'UPDATE_CHARACTER_SPECS',
  'UPDATE_CINEMATICS',
  'UPDATE_FATALITIES',
  'UPDATE_STAGE',
  'REMOVE_MOVE',
  'BATCH',
  'NOOP',
];

const LIMITS = {
  damage: [0, 60],
  totalDamage: [0, 120],
  startupFrames: [1, 60],
  activeFrames: [1, 90],
  recoveryFrames: [1, 90],
  meterCost: [0, 100],
  maxHP: [50, 400],
  moveSpeed: [1, 12],
  defense: [0.2, 3],
  jumpForce: [4, 18],
  scale: [0.2, 3],
  sequence: [1, 6],
  moves: [0, 12],
  cinematicDamage: [10, 90],
  zoomFrame: [1, 120],
  slowMotion: [0.05, 1],
  hitstopFrames: [0, 30],
  shake: [0, 3],
  stageObjects: [0, 8],
};

const clamp = (v, [lo, hi], dflt) => {
  const n = Number(v);
  if (!Number.isFinite(n)) return dflt;
  return Math.min(hi, Math.max(lo, n));
};

/** "Down, Forward, HP" / "236P" -> kanonische Tokens */
export function normalizeSequence(seq) {
  if (typeof seq === 'string') {
    // Numpad-Notation 236P unterstützen
    if (/^[1-9]+\s*[a-zA-Z_]*$/.test(seq.trim())) {
      const m = seq.trim().match(/^([1-9]+)\s*([a-zA-Z_]*)$/);
      const dirs = { 1: 'DOWN', 2: 'DOWN', 3: 'FORWARD', 4: 'BACK', 6: 'FORWARD', 7: 'BACK', 8: 'UP', 9: 'UP' };
      const out = [...m[1]].map((d) => dirs[d]).filter(Boolean);
      const btn = normalizeToken(m[2]);
      if (btn) out.push(btn);
      return dedupeRuns(out);
    }
    seq = seq.split(/[,+>\s]+/);
  }
  if (!Array.isArray(seq)) return [];
  return seq.map(normalizeToken).filter(Boolean).slice(0, LIMITS.sequence[1]);
}

const TOKEN_ALIASES = {
  u: 'UP', up: 'UP', hoch: 'UP', oben: 'UP', jump: 'UP',
  d: 'DOWN', down: 'DOWN', runter: 'DOWN', unten: 'DOWN', duck: 'DOWN',
  f: 'FORWARD', forward: 'FORWARD', vor: 'FORWARD', vorne: 'FORWARD', vorwaerts: 'FORWARD',
  b: 'BACK', back: 'BACK', zurueck: 'BACK', hinten: 'BACK',
  lp: 'LIGHT_PUNCH', p: 'LIGHT_PUNCH', punch: 'LIGHT_PUNCH', schlag: 'LIGHT_PUNCH',
  light: 'LIGHT_PUNCH', light_punch: 'LIGHT_PUNCH', leichterschlag: 'LIGHT_PUNCH',
  hp: 'HEAVY_PUNCH', heavy: 'HEAVY_PUNCH', heavy_punch: 'HEAVY_PUNCH', schwererschlag: 'HEAVY_PUNCH',
  lk: 'LIGHT_KICK', kick: 'LIGHT_KICK', light_kick: 'LIGHT_KICK', tritt: 'LIGHT_KICK',
  hk: 'HEAVY_KICK', heavy_kick: 'HEAVY_KICK', schwerertritt: 'HEAVY_KICK',
  block: 'BLOCK', grab: 'GRAB', wurf: 'GRAB', special: 'SPECIAL', ex: 'SPECIAL',
};

function normalizeToken(t) {
  if (!t) return null;
  const raw = String(t).trim();
  if (!raw) return null;
  const up = raw.toUpperCase().replace(/[\s-]+/g, '_');
  if (INPUT_TOKENS.includes(up)) return up;
  const key = raw.toLowerCase().replace(/[\s_-]+/g, '').replace(/ü/g, 'ue').replace(/ä/g, 'ae').replace(/ö/g, 'oe');
  return TOKEN_ALIASES[key] || null;
}

function dedupeRuns(arr) {
  return arr.filter((v, i) => i === 0 || v !== arr[i - 1] || i === arr.length - 1);
}

function slug(name, fallback) {
  const s = String(name || '').trim();
  return s ? s.slice(0, 48) : fallback;
}

function toHex(color, dflt = null) {
  if (color == null) return dflt;
  if (typeof color === 'number' && Number.isFinite(color)) return color & 0xffffff;
  const s = String(color).trim().replace(/^#/, '').replace(/^0x/i, '');
  if (/^[0-9a-f]{6}$/i.test(s)) return parseInt(s, 16);
  if (/^[0-9a-f]{3}$/i.test(s)) return parseInt(s.split('').map((c) => c + c).join(''), 16);
  return dflt;
}

/**
 * Validiert und repariert einen KI-Konfigurationsblock.
 * @returns {{ok:boolean, config:object, warnings:string[], errors:string[]}}
 */
export function validateConfig(input) {
  const warnings = [];
  const errors = [];
  let data = input;

  if (typeof data === 'string') {
    try {
      data = JSON.parse(extractJson(data));
    } catch (e) {
      return { ok: false, config: null, warnings, errors: ['Kein gültiges JSON: ' + e.message] };
    }
  }
  if (!data || typeof data !== 'object' || Array.isArray(data)) {
    return { ok: false, config: null, warnings, errors: ['Konfiguration ist kein Objekt.'] };
  }

  const cfg = { requestType: 'BATCH' };
  const rt = String(data.requestType || '').toUpperCase();
  cfg.requestType = REQUEST_TYPES.includes(rt) ? rt : 'BATCH';
  if (rt && !REQUEST_TYPES.includes(rt)) warnings.push(`Unbekannter requestType "${data.requestType}" -> BATCH.`);

  // --- Combos ---
  if (Array.isArray(data.combos) && data.combos.length) {
    cfg.combos = data.combos.slice(0, LIMITS.moves[1]).map((c, i) => {
      const sequence = normalizeSequence(c.inputSequence || c.sequence);
      if (!sequence.length) warnings.push(`Combo #${i + 1} ohne gültige Eingabefolge -> LIGHT_PUNCH x2.`);
      const clip = ANIMATION_LIBRARY.includes(c.animationClipName) ? c.animationClipName : (c.animationClipName || 'punch_combo_1');
      return {
        name: slug(c.name, `Combo ${i + 1}`),
        inputSequence: sequence.length ? sequence : ['LIGHT_PUNCH', 'LIGHT_PUNCH'],
        totalDamage: clamp(c.totalDamage ?? c.damage, LIMITS.totalDamage, 12),
        hits: clamp(c.hits ?? (sequence.length || 2), [1, 8], sequence.length || 2),
        animationClipName: String(clip).slice(0, 64),
        hitboxShape: resolveAsset('hitbox', c.hitboxShape) || 'medium_sphere',
        sfxAsset: resolveAsset('audio', c.sfxAsset) || 'whoosh_light',
        cancelWindowFrames: clamp(c.cancelWindowFrames, [0, 40], 14),
        startupFrames: clamp(c.startupFrames, LIMITS.startupFrames, 6),
        hitStunFrames: clamp(c.hitStunFrames, [0, 60], 12),
        recoveryFrames: clamp(c.recoveryFrames, LIMITS.recoveryFrames, 10),
        wallBounce: !!c.wallBounce,
        statusEffect: STATUS_EFFECTS.includes(c.statusEffect) ? c.statusEffect : 'none',
        vfxAsset: c.vfxAsset ? (resolveAsset('vfx', c.vfxAsset) || undefined) : undefined,
      };
    });
  }

  // --- Special Attacks ---
  if (Array.isArray(data.specialAttacks) && data.specialAttacks.length) {
    cfg.specialAttacks = data.specialAttacks.slice(0, LIMITS.moves[1]).map((s, i) => {
      const sequence = normalizeSequence(s.inputSequence || s.sequence);
      const vfx = resolveAsset('vfx', s.vfxAsset);
      if (s.vfxAsset && !vfx) warnings.push(`VFX "${s.vfxAsset}" existiert nicht -> fire_blast.`);
      const status = STATUS_EFFECTS.includes(s.statusEffect) ? s.statusEffect : 'none';
      return {
        name: slug(s.name, `Special ${i + 1}`),
        inputSequence: sequence.length ? sequence : ['DOWN', 'FORWARD', 'HEAVY_PUNCH'],
        vfxAsset: vfx || 'fire_blast',
        sfxAsset: resolveAsset('audio', s.sfxAsset) || 'energy_charge',
        hitboxShape: resolveAsset('hitbox', s.hitboxShape) || 'wide_cone',
        damage: clamp(s.damage, LIMITS.damage, 20),
        startupFrames: clamp(s.startupFrames, LIMITS.startupFrames, 12),
        activeFrames: clamp(s.activeFrames, LIMITS.activeFrames, 24),
        recoveryFrames: clamp(s.recoveryFrames, LIMITS.recoveryFrames, 18),
        meterCost: clamp(s.meterCost, LIMITS.meterCost, 0),
        projectile: !!s.projectile,
        statusEffect: status,
        spawnAt: SPAWN_POINTS.includes(s.spawnAt) ? s.spawnAt : 'self',
        guardBreak: s.guardBreak != null ? !!s.guardBreak : status === 'guard_break',
        pullStrength: s.pullStrength != null ? clamp(s.pullStrength, [0, 12], 0) : (status === 'pull' ? 6 : 0),
        animationClipName: String(s.animationClipName || 'cast_forward').slice(0, 64),
      };
    });
  }

  // --- Visuals ---
  if (data.visualOverrides && typeof data.visualOverrides === 'object') {
    const v = data.visualOverrides;
    const out = {};
    const tint = toHex(v.tintColor);
    if (tint != null) out.tintColor = tint;
    const emis = toHex(v.emissiveColor);
    if (emis != null) out.emissiveColor = emis;
    if (v.emissiveIntensity != null) out.emissiveIntensity = clamp(v.emissiveIntensity, [0, 4], 0);
    if (v.metalness != null) out.metalness = clamp(v.metalness, [0, 1], 0);
    if (v.roughness != null) out.roughness = clamp(v.roughness, [0, 1], 1);
    if (v.scale != null) out.scale = clamp(v.scale, LIMITS.scale, 1);
    if (v.wireframe != null) out.wireframe = !!v.wireframe;
    if (v.auraVfx) out.auraVfx = resolveAsset('vfx', v.auraVfx) || undefined;
    if (Object.keys(out).length) cfg.visualOverrides = out;
  }

  // --- Stats ---
  if (data.stats && typeof data.stats === 'object') {
    const s = data.stats;
    const out = {};
    if (s.maxHP != null) out.maxHP = Math.round(clamp(s.maxHP, LIMITS.maxHP, 100));
    if (s.moveSpeed != null) out.moveSpeed = clamp(s.moveSpeed, LIMITS.moveSpeed, 5);
    if (s.defense != null) out.defense = clamp(s.defense, LIMITS.defense, 1);
    if (s.jumpForce != null) out.jumpForce = clamp(s.jumpForce, LIMITS.jumpForce, 9.5);
    if (Object.keys(out).length) cfg.stats = out;
  }

  // --- X-Ray / Cinematic Moves ---
  if (Array.isArray(data.cinematicMoves) && data.cinematicMoves.length) {
    cfg.cinematicMoves = data.cinematicMoves.slice(0, 6).map((c, i) => {
      const sequence = normalizeSequence(c.inputSequence || c.sequence);
      const bone = BONE_TARGETS.includes(String(c.boneTarget || '').toUpperCase())
        ? String(c.boneTarget).toUpperCase() : 'SPINE_T3';
      if (c.boneTarget && bone !== String(c.boneTarget).toUpperCase()) warnings.push(`Knochenziel "${c.boneTarget}" unbekannt -> SPINE_T3.`);
      const trigger = TRIGGER_CONDITIONS.includes(String(c.triggerCondition || '').toUpperCase())
        ? String(c.triggerCondition).toUpperCase() : 'METERS_FULL';
      const cam = CAMERA_PATHS[c.cameraPath] ? c.cameraPath : 'orbit_victim';
      return {
        name: slug(c.name, `X-Ray ${i + 1}`),
        inputSequence: sequence.length ? sequence : ['LIGHT_PUNCH', 'BLOCK'],
        triggerCondition: trigger,
        cinematicZoomFrame: Math.round(clamp(c.cinematicZoomFrame, LIMITS.zoomFrame, 14)),
        slowMotionFactor: clamp(c.slowMotionFactor, LIMITS.slowMotion, 0.15),
        boneTarget: bone,
        cameraPath: cam,
        damage: clamp(c.damage, LIMITS.cinematicDamage, 33),
        hitstopFrames: Math.round(clamp(c.hitstopFrames, LIMITS.hitstopFrames, 8)),
        shakeStrength: clamp(c.shakeStrength, LIMITS.shake, 1.2),
        vfxAsset: resolveAsset('vfx', c.vfxAsset) || 'bone_shards',
        sfxAsset: resolveAsset('audio', c.sfxAsset) || 'bone_crack',
        durationFrames: Math.round(clamp(c.durationFrames, [20, 240], 90)),
        animationClipName: String(c.animationClipName || 'xray_strike').slice(0, 64),
      };
    });
  }

  // --- Fatalities / Finisher ---
  if (Array.isArray(data.fatalities) && data.fatalities.length) {
    cfg.fatalities = data.fatalities.slice(0, 6).map((f, i) => {
      const sequence = normalizeSequence(f.inputSequence || f.sequence);
      const typeKey = String(f.finisherType || '').toUpperCase();
      const type = FINISHER_TYPES[typeKey] ? typeKey : 'EXPLOSION';
      if (f.finisherType && type !== typeKey) warnings.push(`Finisher-Typ "${f.finisherType}" unbekannt -> EXPLOSION.`);
      const dist = FINISHER_DISTANCES.includes(String(f.distance || '').toUpperCase())
        ? String(f.distance).toUpperCase() : 'CLOSE';
      return {
        name: slug(f.name, `Fatality ${i + 1}`),
        inputSequence: sequence.length ? sequence : ['DOWN', 'DOWN', 'DOWN', 'HEAVY_PUNCH'],
        distance: dist,
        finisherType: type,
        vfxExplosionAsset: resolveAsset('vfx', f.vfxExplosionAsset) || FINISHER_TYPES[type].gore,
        sfxAsset: resolveAsset('audio', f.sfxAsset) || 'gore_squelch',
        slowMotionFactor: clamp(f.slowMotionFactor, LIMITS.slowMotion, 0.25),
        cameraPath: CAMERA_PATHS[f.cameraPath] ? f.cameraPath : 'push_in_face',
        ragdoll: f.ragdoll != null ? !!f.ragdoll : true,
        durationFrames: Math.round(clamp(f.durationFrames, [30, 300], 150)),
        animationClipName: String(f.animationClipName || 'fatality_finish').slice(0, 64),
      };
    });
  }

  // --- Stage Interactions ---
  if (Array.isArray(data.stageInteractions) && data.stageInteractions.length) {
    cfg.stageInteractions = data.stageInteractions.slice(0, LIMITS.stageObjects[1]).map((o, i) => {
      const key = STAGE_OBJECTS[o.object] ? o.object
        : Object.keys(STAGE_OBJECTS).find((k) => k.includes(String(o.object || '').toLowerCase())) || 'wooden_crate';
      if (o.object && key !== o.object) warnings.push(`Arena-Objekt "${o.object}" unbekannt -> ${key}.`);
      const role = STAGE_ROLES.includes(String(o.role || '').toUpperCase())
        ? String(o.role).toUpperCase() : STAGE_OBJECTS[key].role;
      const pos = Array.isArray(o.position) && o.position.length >= 2
        ? [clamp(o.position[0], [-9, 9], 0), clamp(o.position[1] ?? 0, [-9, 9], 0)]
        : [i % 2 === 0 ? -3.5 - i : 3.5 + i, -1.5 + i * 0.8];
      return {
        object: key,
        role,
        position: pos,
        damage: Math.round(clamp(o.damage, [0, 40], STAGE_OBJECTS[key].damage)),
        interactionInput: normalizeSequence(o.interactionInput || ['FORWARD', 'GRAB']).length
          ? normalizeSequence(o.interactionInput || ['FORWARD', 'GRAB']) : ['FORWARD', 'GRAB'],
      };
    });
  }

  // --- Hitstop-/Wucht-Profil ---
  if (data.impactProfile && typeof data.impactProfile === 'object') {
    const ip = data.impactProfile;
    cfg.impactProfile = {
      lightHitstopFrames: Math.round(clamp(ip.lightHitstopFrames, LIMITS.hitstopFrames, 2)),
      heavyHitstopFrames: Math.round(clamp(ip.heavyHitstopFrames, LIMITS.hitstopFrames, 5)),
      shakeStrength: clamp(ip.shakeStrength, LIMITS.shake, 0.8),
      zoomPunch: ip.zoomPunch != null ? !!ip.zoomPunch : true,
    };
  }

  // --- Entfernen ---
  if (Array.isArray(data.removeMoves) && data.removeMoves.length) {
    cfg.removeMoves = data.removeMoves.slice(0, 12).map((n) => String(n).slice(0, 48));
  }

  if (data.profileName) cfg.profileName = String(data.profileName).slice(0, 64);
  if (data.archetype) cfg.archetype = String(data.archetype).slice(0, 64);
  if (data.notes) cfg.notes = String(data.notes).slice(0, 400);

  const touched = ['combos', 'specialAttacks', 'visualOverrides', 'stats', 'removeMoves',
    'cinematicMoves', 'fatalities', 'stageInteractions', 'impactProfile']
    .filter((k) => cfg[k]);
  if (!touched.length) {
    cfg.requestType = 'NOOP';
    warnings.push('Konfiguration enthält keine anwendbaren Felder.');
  }

  return { ok: cfg.requestType !== 'NOOP', config: cfg, warnings, errors };
}

/** Holt den ersten JSON-Block aus LLM-Fließtext (```json ... ``` oder { ... }). */
export function extractJson(text) {
  const s = String(text);
  const fence = s.match(/```(?:json)?\s*([\s\S]*?)```/i);
  const body = fence ? fence[1] : s;
  const start = body.indexOf('{');
  if (start < 0) return body.trim();
  let depth = 0, inStr = false, esc = false;
  for (let i = start; i < body.length; i++) {
    const c = body[i];
    if (esc) { esc = false; continue; }
    if (c === '\\') { esc = true; continue; }
    if (c === '"') { inStr = !inStr; continue; }
    if (inStr) continue;
    if (c === '{') depth++;
    else if (c === '}' && --depth === 0) return body.slice(start, i + 1);
  }
  return body.slice(start).trim();
}
