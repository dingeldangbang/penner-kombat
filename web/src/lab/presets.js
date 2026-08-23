/**
 * presets.js — Vier fertige High-Impact-Kampfprofile
 *
 * Jedes Profil ist genau der JSON-Block, den die KI-Engine auch selbst
 * ausgeben würde: `requestType: "BATCH"` mit Optik, Werten, Specials und Combos.
 * Man kann sie direkt in
 *
 *     fighter.injectAiConfiguration(PRESETS.cyber_scorpion.config)
 *
 * kippen (Alias von `applyAiConfiguration`) — sie laufen dabei durch dieselbe
 * Validierung wie jede KI-Antwort. Die Materialien wechseln sofort, und der
 * Eingabepuffer hört ab dem nächsten Frame auf genau diese Tastenfolgen.
 */

export const PRESETS = {
  // -------------------------------------------------------------------------
  cyber_scorpion: {
    id: 'cyber_scorpion',
    name: 'Cyber-Scorpion',
    role: 'Der Nano-Ninja',
    emoji: '🦂',
    accent: '#00e5cc',
    summary: 'Chrom-Metallic mit Elektro-Aura. Zieht den Gegner per Magnet-Grab quer über den Screen und drückt dann die 4-Treffer-Kette „Overload“ durch.',
    config: {
      requestType: 'BATCH',
      profileName: 'Cyber-Scorpion',
      archetype: 'Nano-Ninja · Rushdown/Zoner-Hybrid',
      visualOverrides: {
        tintColor: '#c8d2dc',        // Chrom
        emissiveColor: '#00bfff',
        emissiveIntensity: 0.85,
        metalness: 1.0,
        roughness: 0.12,
        scale: 1.0,
        auraVfx: 'electricity',
      },
      stats: { maxHP: 95, moveSpeed: 7.5, defense: 0.9, jumpForce: 11 },
      specialAttacks: [
        {
          name: 'Magnet Grab',
          inputSequence: ['BACK', 'FORWARD', 'GRAB'],
          vfxAsset: 'magnet_pull',
          sfxAsset: 'magnet_hum',
          hitboxShape: 'screen_long_box',   // langer Kasten quer über den Screen
          damage: 12,
          startupFrames: 14,
          activeFrames: 26,
          recoveryFrames: 22,
          meterCost: 0,
          projectile: false,
          statusEffect: 'pull',
          pullStrength: 9,
          spawnAt: 'target',
          animationClipName: 'cast_forward',
        },
        {
          name: 'Nano Bolt',
          inputSequence: ['DOWN', 'FORWARD', 'LIGHT_PUNCH'],
          vfxAsset: 'electricity',
          sfxAsset: 'zap',
          hitboxShape: 'long_box',
          damage: 14,
          startupFrames: 8,
          activeFrames: 16,
          recoveryFrames: 14,
          projectile: true,
          statusEffect: 'stun',
          animationClipName: 'cast_forward',
        },
      ],
      combos: [
        {
          name: 'Overload',
          inputSequence: ['LIGHT_PUNCH', 'LIGHT_PUNCH', 'LIGHT_PUNCH', 'LIGHT_PUNCH'],
          totalDamage: 24,
          hits: 4,
          startupFrames: 4,             // sehr schnelle Kette
          hitStunFrames: 8,
          recoveryFrames: 8,
          cancelWindowFrames: 18,
          animationClipName: 'punch_combo_1',
          hitboxShape: 'small_sphere',
          sfxAsset: 'whoosh_light',
          vfxAsset: 'electricity',
          statusEffect: 'stun',
        },
      ],
      cinematicMoves: [
        {
          name: 'Circuit Breaker X-Ray',
          inputSequence: ['LIGHT_PUNCH', 'BLOCK'],
          triggerCondition: 'METERS_FULL',
          cinematicZoomFrame: 12,
          slowMotionFactor: 0.14,
          boneTarget: 'FOREARM',
          cameraPath: 'orbit_victim',
          damage: 34,
          hitstopFrames: 9,
          shakeStrength: 1.3,
          vfxAsset: 'electricity',
          sfxAsset: 'bone_crack',
          durationFrames: 96,
          animationClipName: 'xray_strike',
        },
      ],
      fatalities: [
        {
          name: 'Nano Disassembly',
          distance: 'CLOSE',
          inputSequence: ['FORWARD', 'DOWN', 'FORWARD', 'LIGHT_PUNCH'],
          finisherType: 'DISMEMBERMENT',
          vfxExplosionAsset: 'electricity',
          sfxAsset: 'gore_squelch',
          slowMotionFactor: 0.22,
          cameraPath: 'side_slide',
          ragdoll: true,
          durationFrames: 150,
        },
      ],
      impactProfile: { lightHitstopFrames: 2, heavyHitstopFrames: 6, shakeStrength: 0.9, zoomPunch: true },
      notes: 'Chrom + Elektro-Aura, Magnet-Grab zieht heran, Overload als schnelle 4-Treffer-Kette, X-Ray auf den Unterarm, Nano-Zerlegung als Fatality.',
    },
  },

  // -------------------------------------------------------------------------
  toxic_ghoulem: {
    id: 'toxic_ghoulem',
    name: 'Toxic Blood-Ghoulem',
    role: 'Der schwere Prügler',
    emoji: '🧟',
    accent: '#7fff2a',
    summary: 'Tiefes Karmesin-Violett, nasse Blut-Optik. „Acid Vomit“ bricht mit breitem Kegel durch den Block, „Brutal Carnage“ startet langsam, hält den Gegner dafür ewig fest.',
    config: {
      requestType: 'BATCH',
      profileName: 'Toxic Blood-Ghoulem',
      archetype: 'Heavy Bruiser · Guard-Breaker',
      visualOverrides: {
        tintColor: '#6a0f2a',          // Karmesin ins Violette
        emissiveColor: '#33ff66',
        emissiveIntensity: 0.35,
        metalness: 0.25,
        roughness: 0.08,               // nasse, glänzende Oberfläche
        scale: 1.2,
        auraVfx: 'poison_cloud',
      },
      stats: { maxHP: 185, moveSpeed: 3.4, defense: 1.6, jumpForce: 7 },
      specialAttacks: [
        {
          name: 'Acid Vomit',
          inputSequence: ['DOWN', 'BACK', 'HEAVY_PUNCH'],
          vfxAsset: 'acid_spray',
          sfxAsset: 'acid_sizzle',
          hitboxShape: 'wide_cone',      // breiter Kegel
          damage: 26,
          startupFrames: 20,
          activeFrames: 34,
          recoveryFrames: 26,
          projectile: false,
          statusEffect: 'guard_break',   // zersetzt normales Blocken
          guardBreak: true,
          spawnAt: 'self',
          animationClipName: 'cast_forward',
        },
        {
          name: 'Blood Geyser',
          inputSequence: ['DOWN', 'DOWN', 'HEAVY_KICK'],
          vfxAsset: 'blood_splatter',
          sfxAsset: 'impact_flesh',
          hitboxShape: 'ground_slam',
          damage: 22,
          startupFrames: 18,
          activeFrames: 22,
          recoveryFrames: 24,
          statusEffect: 'bleed',
          spawnAt: 'ground_target',
          animationClipName: 'ground_slam',
        },
      ],
      combos: [
        {
          name: 'Brutal Carnage',
          inputSequence: ['HEAVY_PUNCH', 'HEAVY_PUNCH', 'HEAVY_KICK'],
          totalDamage: 42,
          hits: 3,
          startupFrames: 16,            // träger Anlauf …
          hitStunFrames: 34,            // … dafür brutaler Hitstun
          recoveryFrames: 28,
          cancelWindowFrames: 26,
          animationClipName: 'heavy_swing',
          hitboxShape: 'long_box',
          sfxAsset: 'whoosh_heavy',
          vfxAsset: 'blood_splatter',
          statusEffect: 'corrode',
        },
      ],
      cinematicMoves: [
        {
          name: 'Ribcage Rupture X-Ray',
          inputSequence: ['HEAVY_PUNCH', 'BLOCK'],
          triggerCondition: 'METERS_FULL',
          cinematicZoomFrame: 18,
          slowMotionFactor: 0.12,
          boneTarget: 'RIBCAGE',
          cameraPath: 'push_in_face',
          damage: 40,
          hitstopFrames: 12,
          shakeStrength: 1.8,
          vfxAsset: 'bone_shards',
          sfxAsset: 'bone_crack',
          durationFrames: 110,
        },
      ],
      fatalities: [
        {
          name: 'Acid Meltdown',
          distance: 'MEDIUM',
          inputSequence: ['DOWN', 'DOWN', 'FORWARD', 'HEAVY_KICK'],
          finisherType: 'MELTDOWN',
          vfxExplosionAsset: 'acid_corrosive',
          sfxAsset: 'acid_sizzle',
          slowMotionFactor: 0.3,
          cameraPath: 'low_angle_rise',
          ragdoll: true,
          durationFrames: 180,
        },
      ],
      stageInteractions: [
        { object: 'burning_barrel', role: 'THROWABLE', position: [-4.5, -1.5], interactionInput: ['FORWARD', 'GRAB'] },
        { object: 'gas_bottle', role: 'THROWABLE', position: [4.6, 1.2], interactionInput: ['FORWARD', 'GRAB'] },
      ],
      impactProfile: { lightHitstopFrames: 3, heavyHitstopFrames: 9, shakeStrength: 1.4, zoomPunch: true },
      notes: 'Guard-Break-Kegel, nasse Blutoptik, Rippenbruch-X-Ray, Säure-Auflösung als Fatality, brennendes Fass und Gasflasche in der Arena.',
    },
  },

  // -------------------------------------------------------------------------
  voodoo_priest: {
    id: 'voodoo_priest',
    name: 'Voodoo Shadow-Priest',
    role: 'Der zonende Teleporter',
    emoji: '🕯️',
    accent: '#aa33ff',
    summary: 'Pechschwarzer Körper unter Void-Aura. „Abyssal Rift“ öffnet AoE-Kugeln direkt unter dem Gegner, „Soul Reap“ ist der leichte Kick-Anfang aus der Distanz.',
    config: {
      requestType: 'BATCH',
      profileName: 'Voodoo Shadow-Priest',
      archetype: 'Zoning Teleporter · Setplay',
      visualOverrides: {
        tintColor: '#0a0a10',          // pechschwarz
        emissiveColor: '#8000ff',
        emissiveIntensity: 0.6,
        metalness: 0.0,
        roughness: 0.95,
        scale: 1.05,
        auraVfx: 'shadow_aura',
      },
      stats: { maxHP: 88, moveSpeed: 5.6, defense: 0.85, jumpForce: 10 },
      specialAttacks: [
        {
          name: 'Abyssal Rift',
          inputSequence: ['DOWN', 'BACK', 'SPECIAL'],
          vfxAsset: 'void_rift',
          sfxAsset: 'void_whisper',
          hitboxShape: 'aoe_sphere',
          damage: 21,
          startupFrames: 22,
          activeFrames: 40,
          recoveryFrames: 16,
          projectile: false,
          statusEffect: 'knockdown',
          spawnAt: 'ground_target',      // direkt unter den Koordinaten des Gegners
          animationClipName: 'cast_forward',
        },
        {
          name: 'Shadow Step',
          inputSequence: ['BACK', 'BACK', 'BLOCK'],
          vfxAsset: 'shadow_aura',
          sfxAsset: 'void_whisper',
          hitboxShape: 'small_sphere',
          damage: 0,
          startupFrames: 6,
          activeFrames: 10,
          recoveryFrames: 10,
          statusEffect: 'none',
          spawnAt: 'self',
          animationClipName: 'cast_forward',
        },
      ],
      combos: [
        {
          name: 'Soul Reap',
          inputSequence: ['LIGHT_KICK', 'LIGHT_KICK'],
          totalDamage: 11,
          hits: 2,
          startupFrames: 7,
          hitStunFrames: 20,             // langer Hitstun als Combo-Starter
          recoveryFrames: 9,
          cancelWindowFrames: 30,        // großzügiges Fenster in den Rift
          animationClipName: 'kick_combo_1',
          hitboxShape: 'long_box',       // greift aus der Distanz
          sfxAsset: 'whoosh_light',
          vfxAsset: 'shadow_aura',
          statusEffect: 'slow',
        },
      ],
      cinematicMoves: [
        {
          name: 'Soul Extraction X-Ray',
          inputSequence: ['LIGHT_KICK', 'BLOCK'],
          triggerCondition: 'METERS_FULL',
          cinematicZoomFrame: 10,
          slowMotionFactor: 0.1,
          boneTarget: 'SPINE_T3',
          cameraPath: 'low_angle_rise',
          damage: 31,
          hitstopFrames: 7,
          shakeStrength: 1.0,
          vfxAsset: 'soul_release',
          sfxAsset: 'void_whisper',
          durationFrames: 120,
        },
      ],
      fatalities: [
        {
          name: 'Soul Harvest',
          distance: 'FAR',
          inputSequence: ['BACK', 'BACK', 'DOWN', 'HEAVY_PUNCH'],
          finisherType: 'SOUL_RIP',
          vfxExplosionAsset: 'soul_release',
          sfxAsset: 'void_whisper',
          slowMotionFactor: 0.18,
          cameraPath: 'orbit_victim',
          ragdoll: true,
          durationFrames: 165,
        },
      ],
      impactProfile: { lightHitstopFrames: 2, heavyHitstopFrames: 4, shakeStrength: 0.7, zoomPunch: true },
      notes: 'AoE unter dem Gegner, Distanz-Starter, Seelen-X-Ray und Seelenernte als Finisher.',
    },
  },

  // -------------------------------------------------------------------------
  bio_mech: {
    id: 'bio_mech',
    name: 'Radioactive Bio-Mech',
    role: 'Der unaufhaltsame Juggernaut',
    emoji: '☢️',
    accent: '#66ff00',
    summary: 'Neongrün strahlende Panzerung. „Meltdown Slam“ ist die Hochrisiko-Schockwelle mit langer Recovery, „Heavy Core Smash“ knallt den Gegner in die Wand.',
    config: {
      requestType: 'BATCH',
      profileName: 'Radioactive Bio-Mech',
      archetype: 'Juggernaut · High Risk / High Reward',
      visualOverrides: {
        tintColor: '#66ff00',
        emissiveColor: '#aaff33',
        emissiveIntensity: 1.4,
        metalness: 0.75,
        roughness: 0.35,
        scale: 1.3,
        auraVfx: 'radiation_burst',
      },
      stats: { maxHP: 220, moveSpeed: 2.8, defense: 1.9, jumpForce: 6.5 },
      specialAttacks: [
        {
          name: 'Meltdown Slam',
          inputSequence: ['DOWN', 'DOWN', 'HEAVY_PUNCH'],
          vfxAsset: 'radiation_burst',
          sfxAsset: 'quake_boom',
          hitboxShape: 'shockwave_ring',
          damage: 38,
          startupFrames: 26,             // Hochrisiko: langer Anlauf …
          activeFrames: 20,
          recoveryFrames: 46,            // … und lange Strafe bei Whiff
          meterCost: 50,
          projectile: false,
          statusEffect: 'knockdown',
          spawnAt: 'self',
          animationClipName: 'ground_slam',
        },
        {
          name: 'Fallout Cloud',
          inputSequence: ['DOWN', 'FORWARD', 'HEAVY_KICK'],
          vfxAsset: 'poison_cloud',
          sfxAsset: 'geiger_click',
          hitboxShape: 'aoe_sphere',
          damage: 16,
          startupFrames: 16,
          activeFrames: 48,
          recoveryFrames: 20,
          statusEffect: 'poison',
          spawnAt: 'ground_target',
          animationClipName: 'cast_forward',
        },
      ],
      combos: [
        {
          name: 'Heavy Core Smash',
          inputSequence: ['HEAVY_PUNCH', 'HEAVY_PUNCH'],
          totalDamage: 34,
          hits: 2,
          startupFrames: 12,
          hitStunFrames: 26,
          recoveryFrames: 20,
          cancelWindowFrames: 20,
          wallBounce: true,              // Wandbounce für Folge-Combos
          animationClipName: 'heavy_swing',
          hitboxShape: 'long_box',
          sfxAsset: 'impact_metal',
          vfxAsset: 'radiation_burst',
          statusEffect: 'wallbounce',
        },
      ],
      cinematicMoves: [
        {
          name: 'Reactor Crush X-Ray',
          inputSequence: ['HEAVY_KICK', 'BLOCK'],
          triggerCondition: 'METERS_FULL',
          cinematicZoomFrame: 20,
          slowMotionFactor: 0.1,
          boneTarget: 'PELVIS',
          cameraPath: 'push_in_face',
          damage: 46,
          hitstopFrames: 14,
          shakeStrength: 2.2,
          vfxAsset: 'radiation_burst',
          sfxAsset: 'quake_boom',
          durationFrames: 130,
        },
      ],
      fatalities: [
        {
          name: 'Core Detonation',
          distance: 'CLOSE',
          inputSequence: ['DOWN', 'DOWN', 'DOWN', 'HEAVY_PUNCH'],
          finisherType: 'EXPLOSION',
          vfxExplosionAsset: 'gore_explosion',
          sfxAsset: 'barrel_burst',
          slowMotionFactor: 0.35,
          cameraPath: 'side_slide',
          ragdoll: true,
          durationFrames: 200,
        },
      ],
      stageInteractions: [
        { object: 'shopping_cart', role: 'THROWABLE', position: [-5.0, 1.8], interactionInput: ['FORWARD', 'GRAB'] },
        { object: 'wall_ledge', role: 'ESCAPE_PAD', position: [5.5, -2.2], interactionInput: ['BACK', 'UP'] },
        { object: 'neon_sign', role: 'HAZARD', position: [0.5, -5.0], interactionInput: ['FORWARD', 'HEAVY_PUNCH'] },
      ],
      impactProfile: { lightHitstopFrames: 4, heavyHitstopFrames: 12, shakeStrength: 2.0, zoomPunch: true },
      notes: 'Panzer-Werte, Schockwelle mit 46 Frames Recovery, Wandbounce-Kombo, Reaktor-X-Ray und Kern-Detonation.',
    },
  },

  // -------------------------------------------------------------------------
  nicro_viper: {
    id: 'nicro_viper',
    name: 'Nicro-Viper',
    role: 'Der Hardcore-Finisher',
    emoji: '🐍',
    accent: '#9dff33',
    summary: 'Das Hardcore-Profil aus der Doku: Spine-Shatter-X-Ray auf Meter, Acid Meltdown als Fatality, dazu eine komplett bespielbare Arena mit Fass, Gasflasche und Mauervorsprung.',
    config: {
      requestType: 'UPDATE_CHARACTER_SPECS',
      profileName: 'Nicro-Viper',
      archetype: 'Hardcore-Finisher · X-Ray & Fatality',
      visualOverrides: {
        tintColor: '#22ff55',
        emissiveColor: '#9dff33',
        emissiveIntensity: 0.7,
        metalness: 0.4,
        roughness: 0.2,            // MKX-Schweiß-Effekt
        scale: 1.05,
        auraVfx: 'acid_spray',
      },
      stats: { maxHP: 120, moveSpeed: 5.8, defense: 1.1, jumpForce: 10 },
      combos: [
        {
          name: 'Volt Strike',
          inputSequence: ['LIGHT_PUNCH', 'HEAVY_PUNCH'],
          totalDamage: 18,
          hits: 2,
          startupFrames: 5,
          hitStunFrames: 16,
          recoveryFrames: 12,
          cancelWindowFrames: 16,
          animationClipName: 'punch_combo_2',
          hitboxShape: 'medium_sphere',
          sfxAsset: 'whoosh_heavy',
          vfxAsset: 'electricity',
        },
      ],
      specialAttacks: [
        {
          name: 'Acid Spit',
          inputSequence: ['DOWN', 'FORWARD', 'HEAVY_KICK'],
          vfxAsset: 'acid_spray',
          sfxAsset: 'acid_sizzle',
          hitboxShape: 'wide_cone',
          damage: 30,
          startupFrames: 12,
          activeFrames: 6,
          recoveryFrames: 18,
          projectile: true,
          statusEffect: 'corrode',
          spawnAt: 'self',
          animationClipName: 'cast_forward',
        },
      ],
      cinematicMoves: [
        {
          name: 'Spine Shatter X-Ray',
          inputSequence: ['LIGHT_PUNCH', 'BLOCK'],
          triggerCondition: 'METERS_FULL',
          cinematicZoomFrame: 14,
          slowMotionFactor: 0.15,
          boneTarget: 'SPINE_T3',
          cameraPath: 'orbit_victim',
          damage: 38,
          hitstopFrames: 11,
          shakeStrength: 1.6,
          vfxAsset: 'bone_shards',
          sfxAsset: 'bone_crack',
          durationFrames: 120,
          animationClipName: 'xray_strike',
        },
      ],
      fatalities: [
        {
          name: 'Acid Meltdown',
          distance: 'MEDIUM',
          inputSequence: ['DOWN', 'DOWN', 'FORWARD', 'HEAVY_KICK'],
          finisherType: 'DISMEMBERMENT',
          vfxExplosionAsset: 'acid_corrosive',
          sfxAsset: 'gore_squelch',
          slowMotionFactor: 0.2,
          cameraPath: 'low_angle_rise',
          ragdoll: true,
          durationFrames: 190,
          animationClipName: 'fatality_finish',
        },
      ],
      stageInteractions: [
        { object: 'burning_barrel', role: 'THROWABLE', position: [-4.2, -2.0], interactionInput: ['FORWARD', 'GRAB'] },
        { object: 'gas_bottle', role: 'THROWABLE', position: [4.4, 2.0], interactionInput: ['FORWARD', 'GRAB'] },
        { object: 'wall_ledge', role: 'ESCAPE_PAD', position: [-6.0, 3.0], interactionInput: ['BACK', 'UP'] },
        { object: 'wooden_crate', role: 'THROWABLE', position: [2.0, -4.5], interactionInput: ['FORWARD', 'GRAB'] },
      ],
      impactProfile: { lightHitstopFrames: 3, heavyHitstopFrames: 8, shakeStrength: 1.5, zoomPunch: true },
      notes: 'Elektro-Combo, Säurespucken, Spine-Shatter-X-Ray und Acid Meltdown — inklusive interaktiver Arena.',
    },
  },
};

export const PRESET_LIST = Object.values(PRESETS);

/** Nur die reinen JSON-Blöcke (z. B. zum Export in eine Datei). */
export function presetConfigs() {
  return Object.fromEntries(PRESET_LIST.map((p) => [p.id, p.config]));
}
