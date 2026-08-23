/**
 * assetLibrary.js — Zentrales technisches Asset-Manifest (Schritt 5 der Architektur)
 *
 * Die KI kann KEINE neue Geometrie, keine Shader und keine Sounds aus Text erzeugen.
 * Stattdessen liest sie dieses Manifest und referenziert nur vorhandene Bausteine.
 * Jeder Eintrag ist ein vorkompilierter, zur Laufzeit sofort spawnbarer Block.
 *
 * Alles hier ist reine Daten — kein Three.js-Import, damit das Manifest auch
 * serverseitig (LLM-Systemprompt) und in Tests nutzbar ist.
 */

/** VFX-Pakete — von vfx.js implementiert (GPU-Points, additive Blends). */
export const VFX_LIBRARY = {
  fire_blast:          { label: 'Feuerball',        color: 0xff6b00, color2: 0xffd700, count: 120, speed: 7.5,  life: 0.9, gravity: -1.2, size: 0.20, blend: 'add',    travels: true },
  fire_particle_stream:{ label: 'Feueratem',        color: 0xff3300, color2: 0xffcc33, count: 220, speed: 5.0,  life: 0.7, gravity: -0.4, size: 0.16, blend: 'add',    travels: true, cone: 0.45 },
  ice_shards:          { label: 'Eissplitter',      color: 0x66ddff, color2: 0xffffff, count: 90,  speed: 9.0,  life: 0.8, gravity: 4.0,  size: 0.14, blend: 'normal', travels: true },
  shadow_aura:         { label: 'Schattenaura',     color: 0x2a0a3a, color2: 0x8000ff, count: 140, speed: 1.2,  life: 1.6, gravity: -0.8, size: 0.26, blend: 'normal', travels: false },
  blood_splatter:      { label: 'Blutfontäne',      color: 0x8b0000, color2: 0xff2222, count: 80,  speed: 6.0,  life: 0.6, gravity: 14.0, size: 0.11, blend: 'normal', travels: false },
  electricity:         { label: 'Elektroschock',    color: 0x00bfff, color2: 0xffffff, count: 110, speed: 11.0, life: 0.35,gravity: 0.0,  size: 0.10, blend: 'add',    travels: true },
  poison_cloud:        { label: 'Giftwolke',        color: 0x228b22, color2: 0xaaff55, count: 130, speed: 1.6,  life: 1.8, gravity: -0.5, size: 0.30, blend: 'normal', travels: false },
  dust_burst:          { label: 'Staubwolke',       color: 0x8b7355, color2: 0xd9c9a3, count: 70,  speed: 3.2,  life: 0.7, gravity: 2.0,  size: 0.22, blend: 'normal', travels: false },
  gold_sparks:         { label: 'Goldfunken',       color: 0xffd700, color2: 0xffffff, count: 100, speed: 8.0,  life: 0.5, gravity: 9.0,  size: 0.09, blend: 'add',    travels: false },
  beer_spray:          { label: 'Bierfontäne',      color: 0xd9a441, color2: 0xfff2c4, count: 95,  speed: 5.5,  life: 0.8, gravity: 11.0, size: 0.12, blend: 'normal', travels: true },
  magnet_pull:         { label: 'Magnetsog',         color: 0x00e5cc, color2: 0xffffff, count: 120, speed: 10.0, life: 0.45,gravity: 0.0,  size: 0.12, blend: 'add',    travels: true, inward: true },
  void_rift:           { label: 'Abgrundriss',       color: 0x120024, color2: 0xaa33ff, count: 160, speed: 3.0,  life: 1.2, gravity: -2.5, size: 0.28, blend: 'normal', travels: false },
  radiation_burst:     { label: 'Radioaktiver Ausbruch', color: 0x66ff00, color2: 0xdfffa0, count: 170, speed: 8.5, life: 1.0, gravity: 3.0, size: 0.20, blend: 'add',   travels: false },
  acid_spray:          { label: 'Säurespucke',       color: 0x7fff2a, color2: 0x2a5c00, count: 150, speed: 6.5,  life: 0.9, gravity: 5.0,  size: 0.17, blend: 'normal', travels: true, cone: 0.7 },
};

/** Audio-Pools — synthetisiert in audioPool.js (WebAudio, keine Dateien nötig). */
export const AUDIO_LIBRARY = {
  whoosh_light:  { label: 'Schlag leicht',  type: 'noise', freq: 900,  dur: 0.12, gain: 0.20, sweep: -600 },
  whoosh_heavy:  { label: 'Schlag schwer',  type: 'noise', freq: 500,  dur: 0.22, gain: 0.28, sweep: -320 },
  impact_flesh:  { label: 'Treffer Körper', type: 'thud',  freq: 130,  dur: 0.18, gain: 0.35, sweep: -90  },
  impact_metal:  { label: 'Treffer Metall', type: 'tone',  freq: 1400, dur: 0.30, gain: 0.22, sweep: -900 },
  grunt_male:    { label: 'Grunzen',        type: 'tone',  freq: 190,  dur: 0.25, gain: 0.25, sweep: -70  },
  energy_charge: { label: 'Energie laden',  type: 'tone',  freq: 220,  dur: 0.55, gain: 0.20, sweep: 700  },
  fire_roar:     { label: 'Feuerbrüllen',   type: 'noise', freq: 300,  dur: 0.60, gain: 0.30, sweep: -180 },
  ice_crack:     { label: 'Eisknacken',     type: 'tone',  freq: 2100, dur: 0.20, gain: 0.18, sweep: -1500 },
  zap:           { label: 'Blitzschlag',    type: 'noise', freq: 2600, dur: 0.18, gain: 0.24, sweep: -2200 },
  coin_ding:     { label: 'Münze',          type: 'tone',  freq: 1750, dur: 0.35, gain: 0.20, sweep: 240  },
  magnet_hum:    { label: 'Magnetsog',       type: 'tone',  freq: 90,   dur: 0.45, gain: 0.26, sweep: 520  },
  acid_sizzle:   { label: 'Säurezischen',    type: 'noise', freq: 1800, dur: 0.55, gain: 0.24, sweep: -1500 },
  void_whisper:  { label: 'Void-Flüstern',   type: 'tone',  freq: 320,  dur: 0.70, gain: 0.22, sweep: -260 },
  geiger_click:  { label: 'Geigerzähler',    type: 'noise', freq: 3200, dur: 0.28, gain: 0.20, sweep: -1200 },
  quake_boom:    { label: 'Erdbeben',        type: 'thud',  freq: 70,   dur: 0.75, gain: 0.40, sweep: -45  },
};

/** Hitbox-Formen — Kollisions-Primitive, vom Kampfmanager ausgewertet. */
export const HITBOX_LIBRARY = {
  small_sphere: { label: 'Kleine Kugel (schnelle Stiche)', shape: 'sphere', radius: 0.35, offset: [0.6, 1.2, 0] },
  medium_sphere:{ label: 'Mittlere Kugel (Standardfaust)', shape: 'sphere', radius: 0.55, offset: [0.8, 1.2, 0] },
  wide_cone:    { label: 'Breiter Kegel (Bodenschlag/Atem)', shape: 'cone', radius: 1.6, length: 3.2, offset: [1.2, 1.0, 0] },
  long_box:     { label: 'Langer Kasten (Schwerthieb)', shape: 'box', size: [3.0, 0.5, 0.6], offset: [1.6, 1.3, 0] },
  ground_slam:  { label: 'Bodenwelle (Ring)', shape: 'ring', radius: 2.6, offset: [0, 0.15, 0] },
  full_body:    { label: 'Ganzkörper (Grab)', shape: 'box', size: [1.2, 1.9, 1.2], offset: [0.9, 0.95, 0] },
  screen_long_box: { label: 'Bildschirmlanger Kasten (Magnet-Grab)', shape: 'box', size: [7.0, 0.9, 0.8], offset: [3.5, 1.2, 0] },
  aoe_sphere:   { label: 'AoE-Kugel am Zielpunkt', shape: 'sphere', radius: 1.5, offset: [0, 0.9, 0] },
  shockwave_ring: { label: 'Schockwelle (weiter Ring)', shape: 'ring', radius: 4.2, offset: [0, 0.15, 0] },
};

/** Kanonische Eingabe-Tokens. Die KI darf nur diese in inputSequence benutzen. */
export const INPUT_TOKENS = [
  'UP', 'DOWN', 'FORWARD', 'BACK',
  'LIGHT_PUNCH', 'HEAVY_PUNCH', 'LIGHT_KICK', 'HEAVY_KICK',
  'BLOCK', 'GRAB', 'SPECIAL',
];

/** Fallback-Posen/Clips, falls das GLB keine passende Animation mitbringt. */
export const ANIMATION_LIBRARY = [
  'idle', 'walk', 'punch_combo_1', 'punch_combo_2', 'kick_combo_1',
  'heavy_swing', 'uppercut', 'cast_forward', 'ground_slam', 'hit_react', 'block', 'death',
];

/** Status-Effekte, die ein Move zusätzlich anhängen kann. */
export const STATUS_EFFECTS = [
  'none', 'burn', 'bleed', 'poison', 'slow', 'stun', 'launch', 'armor', 'invert',
  'pull',        // zieht den Gegner heran (Magnet-Grab)
  'guard_break', // durchschlägt Blocken (Säurespucke)
  'wallbounce',  // schleudert in die Wand, verlängert Kombos
  'corrode',     // Verteidigung sinkt auf Zeit
  'knockdown',   // legt flach, langes Aufstehen
];

/** Wo die Wirkung entsteht: beim Kämpfer, am Gegner oder am Boden unter dem Gegner. */
export const SPAWN_POINTS = ['self', 'target', 'ground_target'];

/**
 * Kompaktes Manifest für den LLM-Systemprompt (Token-sparsam).
 * @returns {object}
 */
export function manifest() {
  return {
    vfxAssets: Object.keys(VFX_LIBRARY),
    audioAssets: Object.keys(AUDIO_LIBRARY),
    hitboxShapes: Object.keys(HITBOX_LIBRARY),
    inputTokens: INPUT_TOKENS,
    animationClips: ANIMATION_LIBRARY,
    statusEffects: STATUS_EFFECTS,
    spawnPoints: SPAWN_POINTS,
    materialOverrides: ['tintColor', 'emissiveColor', 'emissiveIntensity', 'metalness', 'roughness', 'scale', 'wireframe', 'auraVfx'],
  };
}

/** Nächstbester Treffer, falls die KI einen unbekannten Asset-Namen erfindet. */
export function resolveAsset(kind, name) {
  const table = kind === 'vfx' ? VFX_LIBRARY : kind === 'audio' ? AUDIO_LIBRARY : HITBOX_LIBRARY;
  if (!name) return null;
  const key = String(name).toLowerCase().replace(/[\s-]+/g, '_');
  if (table[key]) return key;
  // Teilstring-Match ("fire" -> fire_blast)
  const keys = Object.keys(table);
  const hit = keys.find((k) => k.includes(key) || key.includes(k));
  if (hit) return hit;
  // Wortweise Ähnlichkeit
  const words = key.split('_').filter(Boolean);
  const scored = keys
    .map((k) => ({ k, score: words.filter((w) => k.includes(w)).length }))
    .sort((a, b) => b.score - a.score);
  return scored[0] && scored[0].score > 0 ? scored[0].k : null;
}
