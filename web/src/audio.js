/**
 * audio.js — synthetisierter Ton per WebAudio.
 * Spiegel von Assets/Scripts/Core/ProceduralAudio.cs: gleiche Klangideen,
 * gleiche Längen. Keine Audiodateien, kein Download.
 */

let ctx = null;

export function initAudio() {
  if (ctx) return ctx;
  const Ctx = window.AudioContext || window.webkitAudioContext;
  if (!Ctx) return null;
  ctx = new Ctx();
  return ctx;
}

export function resumeAudio() {
  if (ctx && ctx.state === 'suspended') ctx.resume();
}

let master = 0.7;
export function setVolume(v) { master = v; }

function env(node, gainValue, attack, decay) {
  const g = ctx.createGain();
  g.gain.setValueAtTime(0, ctx.currentTime);
  g.gain.linearRampToValueAtTime(gainValue * master, ctx.currentTime + attack);
  g.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + attack + decay);
  node.connect(g);
  g.connect(ctx.destination);
  return g;
}

function noiseBuffer(seconds) {
  const len = Math.floor(ctx.sampleRate * seconds);
  const buf = ctx.createBuffer(1, len, ctx.sampleRate);
  const data = buf.getChannelData(0);
  for (let i = 0; i < len; i++) data[i] = Math.random() * 2 - 1;
  return buf;
}

/** Treffer: tiefer Sinus-Abfall plus Rauschtransient. */
export function playHit(heavy = false, crit = false) {
  if (!ctx) return;
  const osc = ctx.createOscillator();
  osc.type = 'sine';
  const base = heavy ? 90 : 150;
  osc.frequency.setValueAtTime(base * (crit ? 1.3 : 1), ctx.currentTime);
  osc.frequency.exponentialRampToValueAtTime(base * 0.4, ctx.currentTime + (heavy ? 0.3 : 0.18));
  env(osc, heavy ? 0.9 : 0.7, 0.005, heavy ? 0.3 : 0.18);
  osc.start();
  osc.stop(ctx.currentTime + (heavy ? 0.32 : 0.2));

  const noise = ctx.createBufferSource();
  noise.buffer = noiseBuffer(0.06);
  const filter = ctx.createBiquadFilter();
  filter.type = 'bandpass';
  filter.frequency.value = crit ? 2600 : 1400;
  noise.connect(filter);
  env(filter, 0.4, 0.001, 0.06);
  noise.start();
}

/** Block: metallisches Klacken. */
export function playBlock() {
  if (!ctx) return;
  [900, 1370].forEach((f, i) => {
    const osc = ctx.createOscillator();
    osc.type = 'square';
    osc.frequency.value = f;
    env(osc, i === 0 ? 0.25 : 0.15, 0.001, 0.12);
    osc.start();
    osc.stop(ctx.currentTime + 0.14);
  });
}

/** Schmerzlaut. */
export function playHurt() {
  if (!ctx) return;
  const osc = ctx.createOscillator();
  osc.type = 'sawtooth';
  osc.frequency.setValueAtTime(220, ctx.currentTime);
  osc.frequency.exponentialRampToValueAtTime(120, ctx.currentTime + 0.22);
  env(osc, 0.25, 0.01, 0.22);
  osc.start();
  osc.stop(ctx.currentTime + 0.24);
}

/** UI-Klick. */
export function playClick() {
  if (!ctx) return;
  const osc = ctx.createOscillator();
  osc.type = 'triangle';
  osc.frequency.value = 640;
  env(osc, 0.3, 0.001, 0.05);
  osc.start();
  osc.stop(ctx.currentTime + 0.06);
}

/** 808-Kick — Herz des Beats. */
export function playKick() {
  if (!ctx) return;
  const osc = ctx.createOscillator();
  osc.type = 'sine';
  osc.frequency.setValueAtTime(145, ctx.currentTime);
  osc.frequency.exponentialRampToValueAtTime(48, ctx.currentTime + 0.12);
  env(osc, 0.8, 0.002, 0.42);
  osc.start();
  osc.stop(ctx.currentTime + 0.45);
}

/** Siegesfanfare, absichtlich schäbig. */
export function playVictory() {
  if (!ctx) return;
  [196, 261.6, 392].forEach((f, i) => {
    const osc = ctx.createOscillator();
    osc.type = 'square';
    osc.frequency.value = f;
    const g = ctx.createGain();
    const t0 = ctx.currentTime + i * 0.22;
    g.gain.setValueAtTime(0, t0);
    g.gain.linearRampToValueAtTime(0.25 * master, t0 + 0.02);
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.3);
    osc.connect(g);
    g.connect(ctx.destination);
    osc.start(t0);
    osc.stop(t0 + 0.32);
  });
}

/**
 * Dunkler 808-Loop im Hintergrund (MusicSync.cs: Combo treibt das Tempo).
 * Bewusst minimal — vier Kicks, Hi-Hat auf den Achteln, Bass-Ton.
 */
export class BeatLoop {
  constructor() {
    this.bpm = 140;
    this.timer = 0;
    this.step = 0;
    this.running = false;
  }

  start() { this.running = true; }
  stop() { this.running = false; }

  /** Combo erhöht das Tempo (wie MusicSync: +0,6 BPM pro Treffer, max 180). */
  onCombo(combo) { this.bpm = Math.min(180, 140 + combo * 0.6); }
  reset() { this.bpm = 140; }

  tick(dt) {
    if (!this.running || !ctx) return;
    this.timer -= dt;
    if (this.timer > 0) return;
    const stepLength = 60 / this.bpm / 2;      // Achtel
    this.timer += stepLength;

    if (this.step % 4 === 0) playKick();
    if (this.step % 2 === 1) this.hat();
    if (this.step % 8 === 6) this.bass();
    this.step = (this.step + 1) % 16;
  }

  hat() {
    const noise = ctx.createBufferSource();
    noise.buffer = noiseBuffer(0.03);
    const filter = ctx.createBiquadFilter();
    filter.type = 'highpass';
    filter.frequency.value = 7000;
    noise.connect(filter);
    env(filter, 0.08, 0.001, 0.03);
    noise.start();
  }

  bass() {
    const osc = ctx.createOscillator();
    osc.type = 'triangle';
    osc.frequency.value = 55;
    env(osc, 0.3, 0.01, 0.25);
    osc.start();
    osc.stop(ctx.currentTime + 0.28);
  }
}
