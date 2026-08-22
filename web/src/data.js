/**
 * data.js — Spiegel der C#-Balance-Werte.
 *
 * Jede Zahl hier hat ihre Quelle im Unity-Code. Ändert sich dort etwas,
 * muss es hier nachgezogen werden (Quelle jeweils im Kommentar).
 */

// --- GameConstants.cs ---
export const K = {
  defaultBestOfRounds: 3,
  defaultRoundTime: 99,
  roundEndDelay: 2.2,
  roundStartDelay: 1.2,
  blockDamageReduction: 0.22,   // 78 % Reduktion
  blockMoveScale: 0.35,
  comboWindow: 2.0,
  knockback: 8,
  knockbackUp: 3,
  gravity: 26,
  jumpForce: 9.5,
  fatalBlowMeterMax: 100,
  fatalBlowDamageMin: 28,
  fatalBlowDamageMax: 44,
  fatalBlowChargePerHit: 12,
  fatalBlowChargePerHitTaken: 8,
  // FighterController.cs
  runSpeedMultiplier: 1.6,
  strafeSpeed: 4,
  rollDistance: 3.2,
  rollDuration: 0.32,
  rollInvulnerable: 0.2,
  rollCooldown: 0.8,
  lightCooldown: 0.4,
  heavyCooldown: 0.8,
  hitStun: 0.2,
  arenaRadius: 12,
  // MedSystem.cs
  medCharges: 2,
  medHeal: 25,
  medDuration: 1.2,
  medCooldown: 8,
};

// --- PennerPalette.cs ---
export const PALETTE = {
  bloodRed: '#8B0000',
  nightBlue: '#1A1C2A',
  warmOrange: '#FF6B00',
  gold: '#FFD700',
  neonBlue: '#00BFFF',
  earth: '#8B4513',
  poisonGreen: '#228B22',
  white: '#FFFFFF',
};

// --- FighterConfig.cs / StatureTable ---
export const STATURE = {
  hager:   { height: 1.90, radius: 0.33, mass: 0.85, hitbox: 0.85 },
  normal:  { height: 1.80, radius: 0.40, mass: 1.00, hitbox: 1.00 },
  breit:   { height: 1.82, radius: 0.52, mass: 1.25, hitbox: 1.20 },
  adipoes: { height: 1.74, radius: 0.62, mass: 1.45, hitbox: 1.35 },
};

/**
 * Roster — Werte aus FighterDatabase.EnsureDefaultRoster(),
 * Spezials aus den Charakterklassen in Assets/Scripts/Characters/.
 */
export const ROSTER = [
  {
    id: 'le_binde', name: 'Le Binde', color: PALETTE.bloodRed, stature: 'adipoes',
    maxHP: 130, moveSpeed: 4.2, light: 11, heavy: 18, range: 2.8,
    passive: 'Schmier-Schlüppa: −60 % Schaden, 45 % Griff-Rutsch, brennt nach 3 Feuer',
    specials: [
      { key: 'flaschenhals', name: 'Flaschenhals ↓↘→+□', damage: 11, cooldown: 5, range: 3.5, effect: 'bleed', startup: 14, active: 4, recovery: 28, desc: 'Grab, Blutung 5s, Flaschen-Projektil' },
      { key: 'flaschenhals_ex', name: 'EX-Flaschenhals', damage: 15, cooldown: 6, range: 3.8, effect: 'bleed_heavy', startup: 14, active: 4, recovery: 28, desc: 'EX: mehr Schaden, Knockback' },
      { key: 'grosser_schwung', name: 'Großer Schwung ←→+△', damage: 16, cooldown: 7, range: 3.2, effect: 'armor_knock', startup: 21, active: 6, recovery: 22, desc: 'Halbkreis mit Armor Frames 8-20, Wallbounce' },
      { key: 'reif', name: 'REIF! ↓↓+○', damage: 0, cooldown: 8, range: 0, effect: 'buff', startup: 30, active: 0, recovery: 0, desc: 'Buff: nächster Treffer +80% für 6s, Lachen' },
      { key: 'mops', name: 'Mops-Kommando ↓↘→+○', damage: 0, cooldown: 22, range: 6.0, effect: 'summon_stun', startup: 50, active: 0, recovery: 0, desc: 'Paula kommt, +50% Krit 8s, kippt alle Arena-Props, Neon Panik' },
      { key: 'pfanne', name: 'Aus der Pfanne →→+△', damage: 14, cooldown: 6, range: 2.9, effect: 'launch', startup: 11, active: 4, recovery: 18, desc: 'Anti-Air, verbraucht Schlüppa-Fett, Launch' },
    ],
  },
  {
    id: 'mell', name: 'Mell', color: PALETTE.neonBlue, stature: 'hager',
    maxHP: 85, moveSpeed: 6.5, light: 7, heavy: 12, range: 2.0,
    passive: 'Der Puls: 60–220 bpm, Tempo hoch, HP-Verlust ab 160',
    specials: [
      { key: 'doppelschicht', name: 'Doppelschicht', damage: 9, cooldown: 4, range: 3.5, effect: 'dash' },
      { key: 'defi', name: 'Defi', damage: 8, cooldown: 10, range: 2.2, effect: 'stun' },
    ],
  },
  {
    id: 'mojo_bob', name: 'Mojo Bob', color: PALETTE.gold, stature: 'normal',
    maxHP: 100, moveSpeed: 4.8, light: 9, heavy: 14, range: 3.5,
    passive: 'Das Mojo: 15 % Krit ×2,4, 0-7 Mojo-Punkte, Wette R1+Richtung',
    specials: [
      { key: 'riesenschwanz', name: 'Riesenschwanz ↓↘→+□', damage: 12, cooldown: 6, range: 4.5, effect: 'whip', startup: 19, active: 6, recovery: 24, desc: 'Längste Reichweite, Peitsche, Krit-fähig' },
      { key: 'riesenschwanz_ex', name: 'EX-Riesenschwanz', damage: 18, cooldown: 7, range: 5.0, effect: 'whip_multi', startup: 19, active: 18, recovery: 24, desc: '18 Frames aktiv, Multi-Hit' },
      { key: 'beutelchen', name: 'Beutelchen ←↙↓+○', damage: 9, cooldown: 8, range: 3.2, effect: 'gamble', startup: 22, active: 0, recovery: 0, desc: 'Zufall: Klavier 5%, Heilung, Gift, Slow, Invert, Burn' },
      { key: 'loeffelsturm', name: 'Löffelsturm →↘↓↙←+□', damage: 17, cooldown: 10, range: 6.5, effect: 'projectile_storm', startup: 24, active: 0, recovery: 0, desc: '17 Löffel, jeder würfelt Krit' },
      { key: 'fuenfzig', name: 'Fünfzig Cent ↓↓+□', damage: 8, cooldown: 5, range: 5.5, effect: 'coin', startup: 15, active: 0, recovery: 0, desc: 'Münz-Projektil, Kalle fängt' },
      { key: 'dingeneldang', name: 'Dingeneldang! →→↓↓+○', damage: 0, cooldown: 20, range: 0, effect: 'mojo_ult', startup: 30, active: 0, recovery: 0, desc: '7 Mojo -> 100% Krit 8s, dann 15s Reue -30%' },
    ],
  },
  {
    id: 'dieter', name: 'Dieter', color: PALETTE.warmOrange, stature: 'breit',
    maxHP: 110, moveSpeed: 4.5, light: 10, heavy: 16, range: 2.5,
    passive: 'Abflussreiniger: kurzzeitig +30 % Schaden',
    specials: [
      { key: 'rohrbruch', name: 'Rohrbruch', damage: 13, cooldown: 6, range: 3.4, effect: 'knock' },
      { key: 'kanalisation', name: 'Kanalisation', damage: 8, cooldown: 9, range: 4.5, effect: 'slow' },
    ],
  },
  {
    id: 'uschi', name: 'Uschi', color: '#C86432', stature: 'normal',
    maxHP: 95, moveSpeed: 5.2, light: 8, heavy: 13, range: 2.2,
    passive: 'Topfdeckel: bessere Blockwirkung',
    specials: [
      { key: 'handtasche', name: 'Handtasche', damage: 12, cooldown: 5, range: 2.6, effect: 'knock' },
      { key: 'gurke', name: 'Gurke', damage: 0, cooldown: 12, range: 0, effect: 'heal' },
    ],
  },
  {
    id: 'tetrapak', name: 'TetraPak', color: PALETTE.poisonGreen, stature: 'breit',
    maxHP: 115, moveSpeed: 4.0, light: 9, heavy: 15, range: 2.7,
    passive: 'Zweiter Wind: +30 % Schaden für 4 s',
    specials: [
      { key: 'fusel', name: 'Fusel-Atem', damage: 6, cooldown: 8, range: 3.5, effect: 'burn' },
      { key: 'leergut', name: 'Leergut', damage: 10, cooldown: 6, range: 4.0, effect: 'slow' },
    ],
  },
  {
    id: 'sigi', name: 'Sigi', color: '#39FF14', stature: 'hager',
    maxHP: 90, moveSpeed: 5.5, light: 7, heavy: 11, range: 2.3,
    passive: 'Firewall: reduziert eingehenden Schaden kurzzeitig',
    specials: [
      { key: 'sql', name: 'SQL-Injection', damage: 12, cooldown: 7, range: 3.0, effect: 'pierce' },
      { key: 'ddos', name: 'DDoS', damage: 6, cooldown: 11, range: 5.0, effect: 'invert' },
    ],
  },
  {
    id: 'rolf', name: 'Rolf', color: '#6B8E23', stature: 'normal',
    maxHP: 105, moveSpeed: 4.7, light: 8, heavy: 14, range: 2.6,
    passive: 'Ratten: beißen den Gegner über Zeit',
    specials: [
      { key: 'ratte', name: 'Ratte rufen', damage: 4, cooldown: 10, range: 6.0, effect: 'dot' },
      { key: 'gift', name: 'Rattengift', damage: 5, cooldown: 9, range: 3.0, effect: 'poison' },
    ],
  },
  {
    id: 'kalle', name: 'Kalle', color: PALETTE.earth, stature: 'breit',
    maxHP: 120, moveSpeed: 4.3, light: 10, heavy: 17, range: 2.4,
    passive: 'Arbeitshandschuh: Greifattacken durchbrechen Blocks',
    specials: [
      { key: 'rohrzange', name: 'Rohrzange', damage: 15, cooldown: 6, range: 2.6, effect: 'knock' },
      { key: 'griff', name: 'Arbeitshandschuh', damage: 11, cooldown: 7, range: 2.0, effect: 'grab' },
    ],
  },
];

// --- AIController.ApplyDifficulty (Spec-Tabelle §3.1) ---
export const AI_LEVELS = [
  { name: 'Sehr leicht', thinkMin: 0.80, thinkMax: 1.20, block: 0.10, combo: 0.00, special: 0.05, aggression: 0.30, damage: 0.6 },
  { name: 'Leicht',      thinkMin: 0.50, thinkMax: 0.90, block: 0.20, combo: 0.15, special: 0.12, aggression: 0.40, damage: 0.8 },
  { name: 'Mittel',      thinkMin: 0.30, thinkMax: 0.60, block: 0.35, combo: 0.30, special: 0.25, aggression: 0.60, damage: 1.0 },
  { name: 'Schwer',      thinkMin: 0.15, thinkMax: 0.35, block: 0.50, combo: 0.50, special: 0.35, aggression: 0.75, damage: 1.2 },
  { name: 'Sehr schwer', thinkMin: 0.08, thinkMax: 0.20, block: 0.65, combo: 0.70, special: 0.45, aggression: 0.90, damage: 1.4 },
  { name: 'BOSS',        thinkMin: 0.05, thinkMax: 0.15, block: 0.80, combo: 0.90, special: 0.55, aggression: 1.00, damage: 1.6 },
];

export const byId = (id) => ROSTER.find((f) => f.id === id) || ROSTER[0];
