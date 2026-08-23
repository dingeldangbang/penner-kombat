/**
 * inputBuffer.js — Eingabepuffer + Sequenzerkennung
 *
 * Speichert die letzten Eingabe-Tokens mit Zeitstempel. Ein Move gilt als
 * getroffen, wenn seine Sequenz das Ende des Puffers bildet und innerhalb des
 * Zeitfensters liegt (Standard 0.6 s pro Token, wie in klassischen Fightern).
 */

export const DEFAULT_WINDOW_MS = 700;

export class InputBuffer {
  constructor(size = 16, windowMs = DEFAULT_WINDOW_MS) {
    this.size = size;
    this.windowMs = windowMs;
    this.entries = []; // {token, t}
  }

  push(token, now = Date.now()) {
    if (!token) return;
    this.entries.push({ token, t: now });
    if (this.entries.length > this.size) this.entries.shift();
  }

  clear() { this.entries.length = 0; }

  /** Nur die Tokens, ältestes zuerst. */
  tokens() { return this.entries.map((e) => e.token); }

  /** Tokens innerhalb des Zeitfensters (Sequenzlänge * windowMs). */
  recent(len, now = Date.now()) {
    const limit = this.windowMs * Math.max(1, len);
    return this.entries.filter((e) => now - e.t <= limit).map((e) => e.token);
  }

  /**
   * Prüft, ob die Sequenz gerade abgeschlossen wurde.
   * @param {string[]} sequence
   */
  matches(sequence, now = Date.now()) {
    if (!Array.isArray(sequence) || !sequence.length) return false;
    const buf = this.recent(sequence.length, now);
    if (buf.length < sequence.length) return false;
    const tail = buf.slice(-sequence.length);
    for (let i = 0; i < sequence.length; i++) if (tail[i] !== sequence[i]) return false;
    return true;
  }

  /** Konsumiert den Puffer nach erfolgreichem Move, damit er nicht doppelt feuert. */
  consume() { this.entries.length = 0; }
}

/** Reine Funktion — von Tests genutzt. */
export function isSequenceMatch(buffer, sequence) {
  if (!Array.isArray(buffer) || !Array.isArray(sequence) || !sequence.length) return false;
  if (buffer.length < sequence.length) return false;
  return JSON.stringify(buffer.slice(-sequence.length)) === JSON.stringify(sequence);
}

/**
 * Wählt aus mehreren Moves den mit der längsten passenden Sequenz
 * (damit "DOWN,FORWARD,HP" nicht von "HP" geschluckt wird).
 */
export function bestMatch(bufferTokens, catalog) {
  let best = null;
  for (const [name, data] of Object.entries(catalog || {})) {
    const seq = data.sequence || data.inputSequence;
    if (!isSequenceMatch(bufferTokens, seq)) continue;
    if (!best || seq.length > best.sequence.length) best = { name, data, sequence: seq };
  }
  return best;
}
