/**
 * audioPool.js — Audio-Bibliothek für die KI-Werkstatt
 *
 * Alle Sounds werden zur Laufzeit synthetisiert (WebAudio), damit die KI
 * Audio-Assets per Namen referenzieren kann, ohne dass Dateien geladen werden.
 * Gleiche Philosophie wie src/audio.js im Hauptspiel.
 */

import { AUDIO_LIBRARY } from './assetLibrary.js';

let ctx = null;
let masterGain = 0.6;

export function initAudio() {
  if (ctx) return ctx;
  const Ctx = window.AudioContext || window.webkitAudioContext;
  if (!Ctx) return null;
  ctx = new Ctx();
  return ctx;
}

export function resumeAudio() { if (ctx && ctx.state === 'suspended') ctx.resume(); }
export function setVolume(v) { masterGain = Math.max(0, Math.min(1, v)); }

function noiseBuffer(seconds) {
  const len = Math.max(1, Math.floor(ctx.sampleRate * seconds));
  const buf = ctx.createBuffer(1, len, ctx.sampleRate);
  const d = buf.getChannelData(0);
  for (let i = 0; i < len; i++) d[i] = Math.random() * 2 - 1;
  return buf;
}

/** Spielt ein Asset aus AUDIO_LIBRARY. Unbekannte Namen werden ignoriert. */
export function play(assetName, pitch = 1) {
  const def = AUDIO_LIBRARY[assetName];
  if (!def || !ctx) return false;
  const t0 = ctx.currentTime;
  const g = ctx.createGain();
  g.gain.setValueAtTime(0, t0);
  g.gain.linearRampToValueAtTime(def.gain * masterGain, t0 + 0.008);
  g.gain.exponentialRampToValueAtTime(0.0001, t0 + def.dur);
  g.connect(ctx.destination);

  if (def.type === 'noise') {
    const src = ctx.createBufferSource();
    src.buffer = noiseBuffer(def.dur);
    const f = ctx.createBiquadFilter();
    f.type = 'bandpass';
    f.frequency.setValueAtTime(def.freq * pitch, t0);
    f.frequency.linearRampToValueAtTime(Math.max(60, (def.freq + def.sweep) * pitch), t0 + def.dur);
    f.Q.value = 1.2;
    src.connect(f); f.connect(g);
    src.start(t0); src.stop(t0 + def.dur);
  } else {
    const osc = ctx.createOscillator();
    osc.type = def.type === 'thud' ? 'sine' : 'triangle';
    osc.frequency.setValueAtTime(def.freq * pitch, t0);
    osc.frequency.exponentialRampToValueAtTime(Math.max(30, (def.freq + def.sweep) * pitch), t0 + def.dur);
    osc.connect(g);
    osc.start(t0); osc.stop(t0 + def.dur);
  }
  return true;
}
