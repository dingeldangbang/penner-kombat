/**
 * aiEngine.js — Die KI-Chat-Pipeline (Prompt -> strukturiertes JSON)
 *
 * Zwei Backends, identisches Ausgabeformat:
 *
 *   1) 'local'  — regelbasierter Offline-Parser (Deutsch + Englisch). Braucht
 *                 keinen API-Key, läuft im Browser, deterministisch, testbar.
 *   2) 'remote' — beliebiger OpenAI-kompatibler /chat/completions-Endpunkt.
 *                 Systemprompt erzwingt JSON gegen unser Asset-Manifest.
 *
 * Beide Wege laufen zwingend durch validateConfig() aus schema.js.
 * Es wird nie Code ausgeführt — die KI liefert ausschließlich Daten.
 */

import { manifest, VFX_LIBRARY, AUDIO_LIBRARY, HITBOX_LIBRARY } from './assetLibrary.js';
import { validateConfig, extractJson, normalizeSequence } from './schema.js';
import { PRESET_LIST } from './presets.js';

// ---------------------------------------------------------------------------
//  Systemprompt für echte LLMs
// ---------------------------------------------------------------------------

export function buildSystemPrompt(extra = {}) {
  const m = manifest();
  return [
    'Du bist die Konfigurations-Engine eines 2.5D-Fighting-Games (Penner Kombat, Three.js).',
    'Du antwortest AUSSCHLIESSLICH mit einem einzigen JSON-Objekt. Kein Fließtext, kein Markdown.',
    'Du erzeugst niemals Code, nur Daten. Du erfindest keine Assetnamen — nutze nur diese Bausteine:',
    JSON.stringify(m),
    'Schema:',
    JSON.stringify({
      requestType: 'UPDATE_ABILITIES|UPDATE_VISUALS|UPDATE_STATS|REMOVE_MOVE|BATCH',
      combos: [{ name: 'string', inputSequence: ['LIGHT_PUNCH'], totalDamage: 15, hits: 3, animationClipName: 'punch_combo_1', hitboxShape: 'medium_sphere', sfxAsset: 'whoosh_light' }],
      specialAttacks: [{ name: 'string', inputSequence: ['DOWN', 'FORWARD', 'HEAVY_PUNCH'], vfxAsset: 'fire_particle_stream', sfxAsset: 'fire_roar', hitboxShape: 'wide_cone', damage: 25, startupFrames: 12, activeFrames: 30, recoveryFrames: 18, projectile: true, statusEffect: 'burn', animationClipName: 'cast_forward' }],
      visualOverrides: { tintColor: '#ff0000', emissiveColor: '#220000', emissiveIntensity: 0.5, scale: 1.0, auraVfx: 'shadow_aura' },
      stats: { maxHP: 100, moveSpeed: 5, defense: 1, jumpForce: 9.5 },
      removeMoves: ['Name'],
      notes: 'kurze deutsche Erklärung',
    }),
    'Balance-Grenzen: damage 0-60, totalDamage 0-120, startupFrames 1-60, activeFrames 1-90, maxHP 50-400.',
    'Gib nur die Felder aus, die der Nutzer wirklich ändern will.',
    extra.characterName ? `Aktueller Charakter: ${extra.characterName}.` : '',
    extra.knownMoves && extra.knownMoves.length ? `Bereits vorhandene Moves: ${extra.knownMoves.join(', ')}.` : '',
  ].filter(Boolean).join('\n');
}

// ---------------------------------------------------------------------------
//  Lokaler Parser
// ---------------------------------------------------------------------------

const norm = (s) => String(s).toLowerCase()
  .replace(/ä/g, 'ae').replace(/ö/g, 'oe').replace(/ü/g, 'ue').replace(/ß/g, 'ss');

/** Themen-Erkennung: Stichwort -> VFX/SFX/Status/Name */
const THEMES = [
  { keys: ['feueratem', 'fire breath', 'flammenwerfer', 'flamethrower'], vfx: 'fire_particle_stream', sfx: 'fire_roar', status: 'burn', name: 'Feueratem', hitbox: 'wide_cone', projectile: false },
  { keys: ['feuerball', 'fireball', 'feuer', 'fire', 'flamme', 'flame', 'brand', 'burn'], vfx: 'fire_blast', sfx: 'fire_roar', status: 'burn', name: 'Feuerball', hitbox: 'medium_sphere', projectile: true },
  { keys: ['eis', 'ice', 'frost', 'gefrier', 'freeze'], vfx: 'ice_shards', sfx: 'ice_crack', status: 'slow', name: 'Eissplitter', hitbox: 'long_box', projectile: true },
  { keys: ['blitz', 'elektro', 'strom', 'lightning', 'electric', 'thunder', 'schock'], vfx: 'electricity', sfx: 'zap', status: 'stun', name: 'Blitzschlag', hitbox: 'long_box', projectile: true },
  { keys: ['schatten', 'shadow', 'dunkel', 'dark', 'aura', 'void'], vfx: 'shadow_aura', sfx: 'energy_charge', status: 'armor', name: 'Schattenaura', hitbox: 'ground_slam', projectile: false },
  { keys: ['gift', 'poison', 'toxic', 'saeure', 'acid'], vfx: 'poison_cloud', sfx: 'grunt_male', status: 'poison', name: 'Giftwolke', hitbox: 'wide_cone', projectile: false },
  { keys: ['blut', 'blood', 'gore'], vfx: 'blood_splatter', sfx: 'impact_flesh', status: 'bleed', name: 'Blutfontäne', hitbox: 'medium_sphere', projectile: false },
  { keys: ['bier', 'beer', 'fusel', 'schnaps', 'pulle', 'flasche'], vfx: 'beer_spray', sfx: 'impact_metal', status: 'poison', name: 'Bierfontäne', hitbox: 'wide_cone', projectile: true },
  { keys: ['muenze', 'coin', 'gold', 'geld', 'cent'], vfx: 'gold_sparks', sfx: 'coin_ding', status: 'none', name: 'Münzwurf', hitbox: 'small_sphere', projectile: true },
  { keys: ['boden', 'ground', 'slam', 'stampf', 'erdbeben', 'quake', 'staub'], vfx: 'dust_burst', sfx: 'impact_flesh', status: 'launch', name: 'Bodenstampfer', hitbox: 'ground_slam', projectile: false },
];

const COLORS = {
  rot: 0xcc2222, red: 0xcc2222, blau: 0x2255cc, blue: 0x2255cc, gruen: 0x22aa44, green: 0x22aa44,
  gelb: 0xdddd22, yellow: 0xdddd22, schwarz: 0x1a1a1a, black: 0x1a1a1a, weiss: 0xeeeeee, white: 0xeeeeee,
  lila: 0x8822cc, violett: 0x8822cc, purple: 0x8822cc, orange: 0xff6b00, pink: 0xff55aa, magenta: 0xff00aa,
  gold: 0xffd700, silber: 0xc0c0c0, silver: 0xc0c0c0, braun: 0x8b4513, brown: 0x8b4513, cyan: 0x00ffff, tuerkis: 0x00e5cc,
};

const NUMWORDS = { ein: 1, eine: 1, eins: 1, one: 1, zwei: 2, two: 2, drei: 3, three: 3, vier: 4, four: 4, fuenf: 5, five: 5, sechs: 6, six: 6, sieben: 7, seven: 7, acht: 8, eight: 8 };

function findTheme(t) {
  for (const th of THEMES) if (th.keys.some((k) => t.includes(norm(k)))) return th;
  return null;
}

function num(t, patterns, dflt = null) {
  for (const re of patterns) {
    const m = t.match(re);
    if (m) {
      const raw = m[1];
      const n = Number(raw);
      if (Number.isFinite(n)) return n;
      if (NUMWORDS[raw]) return NUMWORDS[raw];
    }
  }
  return dflt;
}

/** Extrahiert eine explizit genannte Eingabefolge ("runter vorne schwerer schlag", "236HP"). */
function parseSequence(text) {
  const t = norm(text);
  const numpad = t.match(/\b([1-9]{2,4})\s*(hp|lp|hk|lk|p|k|punch|kick)\b/);
  if (numpad) return normalizeSequence(numpad[1] + numpad[2]);
  const words = t.match(/(runter|unten|down|vor|vorne|forward|zurueck|hinten|back|hoch|oben|up)[,\s+]+((?:(?:runter|unten|down|vor|vorne|forward|zurueck|hinten|back|hoch|oben|up)[,\s+]+)*)(schwerer schlag|leichter schlag|schwerer tritt|leichter tritt|heavy punch|light punch|heavy kick|light kick|hp|lp|hk|lk|schlag|tritt|punch|kick)/);
  if (words) {
    const parts = (words[1] + ' ' + words[2] + ' ' + words[3])
      .replace(/schwerer schlag|heavy punch/g, 'HEAVY_PUNCH')
      .replace(/leichter schlag|light punch/g, 'LIGHT_PUNCH')
      .replace(/schwerer tritt|heavy kick/g, 'HEAVY_KICK')
      .replace(/leichter tritt|light kick/g, 'LIGHT_KICK')
      .split(/[,\s+]+/).filter(Boolean);
    const seq = normalizeSequence(parts);
    if (seq.length >= 2) return seq;
  }
  return null;
}

function comboButton(t) {
  const heavy = /(schwer\w*|heavy|hart\w*|wuchtig|stark\w*)/.test(t);
  const kick = /(tritt|kick|bein)/.test(t);
  if (kick) return heavy ? 'HEAVY_KICK' : 'LIGHT_KICK';
  if (heavy || /\bhp\b/.test(t)) return 'HEAVY_PUNCH';
  return 'LIGHT_PUNCH';
}

const CLIP_FOR = {
  LIGHT_PUNCH: 'punch_combo_1', HEAVY_PUNCH: 'heavy_swing',
  LIGHT_KICK: 'kick_combo_1', HEAVY_KICK: 'kick_combo_1',
};

/**
 * Regelbasierte "KI": Freitext -> Rohkonfiguration (noch unvalidiert).
 * @param {string} text
 * @param {{knownMoves?:string[]}} ctx
 */
export function parseLocal(text, ctx = {}) {
  const t = norm(text);
  const out = { requestType: 'BATCH' };
  const notes = [];

  // ---- Fertige Kampfprofile ("lade Cyber-Scorpion", "mach ihn zum Bio-Mech") ----
  const preset = matchPreset(t);
  if (preset) {
    return { ...structuredClone(preset.config), notes: `Profil „${preset.name}" (${preset.role}) geladen.` };
  }

  // ---- Entfernen ----
  if (/(entferne|loesche|weg mit|remove|delete)\b/.test(t)) {
    const known = (ctx.knownMoves || []).filter((n) => t.includes(norm(n)));
    if (known.length) {
      out.removeMoves = known;
      out.requestType = 'REMOVE_MOVE';
      notes.push(`${known.join(', ')} entfernt.`);
      return finish(out, notes);
    }
    if (/(alle|all)\b/.test(t) && (ctx.knownMoves || []).length) {
      out.removeMoves = ctx.knownMoves.slice();
      out.requestType = 'REMOVE_MOVE';
      notes.push('Alle Moves entfernt.');
      return finish(out, notes);
    }
  }

  // ---- Stats ----
  const stats = {};
  const hp = num(t, [/(\d{2,3})\s*(?:hp|lebenspunkte|leben|health)/, /(?:hp|lebenspunkte|leben|health|hitpoints)\b[^\d]{0,14}(\d{2,3})/]);
  if (hp != null) stats.maxHP = hp;
  else if (/(mehr leben|zaeher|tanky|robuster|more health)/.test(t)) stats.maxHP = 150;
  else if (/(weniger leben|glass cannon|zerbrechlich|squishy)/.test(t)) stats.maxHP = 70;

  if (/(schneller|flinker|faster|speed up|rushdown)/.test(t)) stats.moveSpeed = 7;
  if (/(langsamer|traeger|slower|schwerfaellig)/.test(t)) stats.moveSpeed = 3.2;
  const spd = num(t, [/(?:tempo|speed|geschwindigkeit)\s*(?:auf|to|=|:)?\s*(\d{1,2}(?:\.\d)?)/]);
  if (spd != null) stats.moveSpeed = spd;
  if (/(hoeher springen|higher jump|mehr sprungkraft)/.test(t)) stats.jumpForce = 13;
  if (/(mehr ruestung|panzer|defensiver|more defense|tank)/.test(t)) stats.defense = 1.5;
  if (Object.keys(stats).length) {
    out.stats = stats;
    notes.push('Werte angepasst: ' + Object.entries(stats).map(([k, v]) => `${k}=${v}`).join(', ') + '.');
  }

  // ---- Visuals ----
  const visual = {};
  const hex = text.match(/#([0-9a-f]{6}|[0-9a-f]{3})\b/i);
  if (hex) visual.tintColor = '#' + hex[1];
  else {
    for (const [word, val] of Object.entries(COLORS)) {
      if (new RegExp(`\\b${word}(e|es|er|en|em)?\\b`).test(t) && /(faerb|farbe|tint|color|colour|mach ihn|mache ihn|anzug|haut|skin|leucht|glow)/.test(t)) {
        visual.tintColor = val; break;
      }
    }
  }
  if (/(leucht|glow|glueh|emissive|neon)/.test(t)) {
    visual.emissiveIntensity = 0.9;
    if (visual.tintColor != null) visual.emissiveColor = visual.tintColor;
  }
  if (/(groesser|bigger|riese|giant|huene)/.test(t)) visual.scale = 1.35;
  if (/(kleiner|smaller|zwerg|winzig|tiny)/.test(t)) visual.scale = 0.7;
  const sc = num(t, [/(?:skalier\w*|scale|groesse)\s*(?:auf|to|=|:)?\s*(\d(?:\.\d+)?)/]);
  if (sc != null) visual.scale = sc;
  if (/(wireframe|drahtgitter)/.test(t)) visual.wireframe = true;
  if (/aura/.test(t)) {
    const th = findTheme(t);
    if (th) visual.auraVfx = th.vfx;
  }
  if (Object.keys(visual).length) {
    out.visualOverrides = visual;
    notes.push('Optik aktualisiert.');
  }

  // ---- Combos ----
  const hits = num(t, [
    /(\d|ein|zwei|drei|vier|fuenf|sechs|sieben|acht)\s*[- ]?\s*(?:treffer|hits?|schlaege|schlag|tritte|kicks?|punches)/,
    /(?:kombo|kombi\w*|combo)\s*(?:mit|aus|of|with)?\s*(\d|zwei|drei|vier|fuenf|sechs)/,
    /(\d|zwei|drei|vier|fuenf|sechs)\s*[- ]?\s*\w*\s*(?:kombo|kombi\w*|combo)/,
  ]);
  const wantsCombo = /(kombo|kombi\w*|combo|schlagfolge|kette|chain)/.test(t);
  if (wantsCombo) {
    const n = Math.max(2, Math.min(6, hits || 3));
    const btn = comboButton(t);
    const explicit = parseSequence(text);
    const seq = explicit && explicit.length >= 2 ? explicit : Array.from({ length: n }, () => btn);
    const dmg = num(t, [/(\d{1,3})\s*(?:schaden|damage|dmg)/, /(?:schaden|damage|dmg)\s*(?:von|of|=|:)?\s*(\d{1,3})/]) ?? n * 5;
    const label = btn.includes('KICK') ? 'Tritt' : 'Schlag';
    out.combos = [{
      name: nameFrom(text) || `${n}er-${label}-Kombo`,
      inputSequence: seq,
      totalDamage: dmg,
      hits: seq.length,
      animationClipName: CLIP_FOR[btn],
      hitboxShape: btn.includes('HEAVY') ? 'long_box' : 'medium_sphere',
      sfxAsset: btn.includes('HEAVY') ? 'whoosh_heavy' : 'whoosh_light',
      cancelWindowFrames: 14,
    }];
    notes.push(`Kombo mit ${seq.length} Treffern gebunden.`);
  }

  // ---- Specials ----
  const theme = findTheme(t);
  const wantsSpecial = /(special|spezial|attacke|attack|angriff|move|faehigkeit|ability|zauber|projektil|projectile|schuss|strahl|beam)/.test(t) || (theme && !wantsCombo);
  if (theme && wantsSpecial) {
    const dmg = num(t, [/(\d{1,3})\s*(?:schaden|damage|dmg)/, /(?:schaden|damage|dmg)\s*(?:von|of|=|:)?\s*(\d{1,3})/]) ?? 25;
    const startup = num(t, [/(\d{1,2})\s*(?:startup|anlauf)/]) ?? (dmg > 30 ? 18 : 12);
    const active = num(t, [/(\d{1,2})\s*(?:aktive|active)/]) ?? 30;
    const seq = parseSequence(text) || ['DOWN', 'FORWARD', 'HEAVY_PUNCH'];
    const projectile = /(projektil|projectile|schuss|wurf|wirf|feuerball|fireball|strahl|beam|schiess)/.test(t) ? true : theme.projectile;
    out.specialAttacks = [{
      name: nameFrom(text) || theme.name,
      inputSequence: seq,
      vfxAsset: theme.vfx,
      sfxAsset: theme.sfx,
      hitboxShape: theme.hitbox,
      damage: dmg,
      startupFrames: startup,
      activeFrames: active,
      recoveryFrames: 18,
      meterCost: /(ex|meter|leiste|super)/.test(t) ? 50 : 0,
      projectile,
      statusEffect: theme.status,
      animationClipName: /(boden|ground|slam|stampf)/.test(t) ? 'ground_slam' : 'cast_forward',
    }];
    notes.push(`Special "${out.specialAttacks[0].name}" mit ${theme.vfx} gebunden (${seq.join(' → ')}).`);
  }

  return finish(out, notes);
}

/** Erkennt eines der vier High-Impact-Profile aus presets.js. */
export function matchPreset(normalizedText) {
  const t = normalizedText;
  for (const p of PRESET_LIST) {
    const words = norm(p.name).split(/[^a-z0-9]+/).filter((w) => w.length > 3);
    const idWords = p.id.split('_').filter((w) => w.length > 3);
    const hit = words.every((w) => t.includes(w))
      || idWords.every((w) => t.includes(w))
      || words.some((w) => w.length > 5 && t.includes(w));
    if (hit) return p;
  }
  return null;
}

function nameFrom(text) {
  const m = text.match(/["„»']([^"“«']{2,40})["“»']/);
  const m2 = text.match(/\b(?:nenn(?:e)? (?:ihn|es|sie|den move)?|name[ns]?(?:ihn)?|call it|named?)\s+([\wÄÖÜäöüß' -]{2,40})/i);
  return (m && m[1].trim()) || (m2 && m2[1].trim()) || null;
}

function finish(out, notes) {
  const touched = ['combos', 'specialAttacks', 'visualOverrides', 'stats', 'removeMoves'].filter((k) => out[k]);
  if (touched.length === 1) {
    out.requestType = out.combos || out.specialAttacks ? 'UPDATE_ABILITIES'
      : out.visualOverrides ? 'UPDATE_VISUALS'
      : out.stats ? 'UPDATE_STATS' : 'REMOVE_MOVE';
  }
  out.notes = notes.join(' ') || 'Nichts erkannt — bitte konkreter beschreiben (z. B. „Gib ihm einen Feueratem mit 25 Schaden“).';
  return out;
}

// ---------------------------------------------------------------------------
//  Öffentliche Engine
// ---------------------------------------------------------------------------

export class AIEngine {
  /**
   * @param {{mode?:'local'|'remote', endpoint?:string, apiKey?:string, model?:string}} opts
   */
  constructor(opts = {}) {
    this.mode = opts.mode || 'local';
    this.endpoint = opts.endpoint || '/api/ai/config';
    this.apiKey = opts.apiKey || '';
    this.model = opts.model || 'gpt-4o-mini';
    this.history = [];
  }

  configure(opts = {}) { Object.assign(this, opts); return this; }

  /**
   * Prompt -> validierte Konfiguration.
   * @returns {Promise<{ok:boolean, config:object|null, warnings:string[], errors:string[], source:string, raw:any}>}
   */
  async request(prompt, ctx = {}) {
    let raw, source = this.mode;
    if (this.mode === 'remote') {
      try {
        raw = await this.#remote(prompt, ctx);
      } catch (e) {
        const fallback = parseLocal(prompt, ctx);
        const res = validateConfig(fallback);
        res.warnings.unshift(`Remote-KI nicht erreichbar (${e.message}) — lokaler Parser übernimmt.`);
        return { ...res, source: 'local-fallback', raw: fallback };
      }
    } else {
      raw = parseLocal(prompt, ctx);
    }
    const res = validateConfig(raw);
    this.history.push({ prompt, config: res.config, at: Date.now() });
    return { ...res, source, raw };
  }

  async #remote(prompt, ctx) {
    const body = {
      model: this.model,
      temperature: 0.2,
      response_format: { type: 'json_object' },
      messages: [
        { role: 'system', content: buildSystemPrompt(ctx) },
        { role: 'user', content: prompt },
      ],
    };
    const headers = { 'Content-Type': 'application/json' };
    if (this.apiKey) headers.Authorization = `Bearer ${this.apiKey}`;
    const ctrl = new AbortController();
    const timer = setTimeout(() => ctrl.abort(), 25000);
    let r;
    try {
      r = await fetch(this.endpoint, { method: 'POST', headers, body: JSON.stringify(body), signal: ctrl.signal });
    } finally {
      clearTimeout(timer);
    }
    if (!r.ok) throw new Error('HTTP ' + r.status);
    const json = await r.json();
    const content = json?.choices?.[0]?.message?.content ?? json?.content ?? json;
    return typeof content === 'string' ? extractJson(content) : content;
  }
}

export const LIBRARY_INFO = { VFX_LIBRARY, AUDIO_LIBRARY, HITBOX_LIBRARY };
