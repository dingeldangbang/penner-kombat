/**
 * cinematic.js — Wucht & Kino: Hitstop, Zeitlupe, Kamerafahrten, Screen-Shake
 *
 * Das Geheimnis hinter dem MKX-Gefühl: Bei einem schweren Treffer friert das
 * Spiel ein paar Frames komplett ein (Hitstop), die Kamera rüttelt und zoomt
 * kurz an. Bei X-Ray/Fatality übernimmt eine gescriptete Kamerafahrt auf einer
 * mathematischen Kurve, während die Spielzeit gedehnt wird (Time Dilation).
 *
 * Der Regisseur liefert der Game-Loop das *skalierte* dt zurück:
 *
 *   const dt = director.update(rawDt);   // 0 während Hitstop, gedehnt in Slow-Mo
 */

import * as THREE from 'three';
import { CAMERA_PATHS } from './assetLibrary.js';

const FRAME = 1 / 60;

export class CinematicDirector {
  /**
   * @param {THREE.Camera} camera
   * @param {{position:THREE.Vector3, lookAt:THREE.Vector3}} home Ruhelage der Kamera
   */
  constructor(camera, home) {
    this.camera = camera;
    this.home = {
      position: home?.position?.clone() ?? camera.position.clone(),
      lookAt: home?.lookAt?.clone() ?? new THREE.Vector3(0, 1.1, 0),
    };
    this.timeScale = 1;
    this.hitstopTimer = 0;
    this.shakeTime = 0;
    this.shakeStrength = 0;
    this.zoomPunch = 0;
    this.sequence = null;     // laufende Kamerafahrt
    this.listeners = {};
    this._tmp = new THREE.Vector3();
  }

  on(evt, fn) { (this.listeners[evt] ||= []).push(fn); return this; }
  emit(evt, p) { (this.listeners[evt] || []).forEach((f) => f(p)); }

  /** Bildstopp für n Frames (60 fps-Basis). */
  hitstop(frames = 5) {
    this.hitstopTimer = Math.max(this.hitstopTimer, frames * FRAME);
  }

  shake(strength = 1, seconds = 0.25) {
    this.shakeStrength = Math.max(this.shakeStrength, strength);
    this.shakeTime = Math.max(this.shakeTime, seconds);
  }

  /** Kurzer Zoom-Stoß bei schweren Treffern. */
  punchZoom(amount = 0.35) { this.zoomPunch = Math.max(this.zoomPunch, amount); }

  /** Kompaktes Treffer-Feedback: Hitstop + Shake + Zoom in einem Aufruf. */
  impact({ frames = 3, strength = 0.6, zoom = 0.2 } = {}) {
    this.hitstop(frames);
    this.shake(strength, 0.18 + frames * FRAME);
    this.punchZoom(zoom);
  }

  get isPlaying() { return !!this.sequence; }

  /**
   * Startet eine gescriptete Kamerafahrt (X-Ray, Fatality).
   * @param {{path?:string, victim:THREE.Vector3, attacker:THREE.Vector3,
   *          durationFrames?:number, slowMotionFactor?:number, zoomFrame?:number,
   *          label?:string, kind?:'xray'|'fatality'}} opts
   */
  play(opts) {
    const path = CAMERA_PATHS[opts.path] ? opts.path : 'orbit_victim';
    const duration = (opts.durationFrames ?? 90) * FRAME;
    this.sequence = {
      path,
      def: CAMERA_PATHS[path],
      victim: opts.victim.clone(),
      attacker: opts.attacker.clone(),
      duration,
      t: 0,
      slowMo: opts.slowMotionFactor ?? 0.2,
      zoomAt: (opts.zoomFrame ?? 14) * FRAME,
      zoomFired: false,
      label: opts.label || '',
      kind: opts.kind || 'xray',
    };
    this.timeScale = this.sequence.slowMo;
    this.emit('start', { ...opts, path });
    return this.sequence;
  }

  cancel() {
    if (!this.sequence) return;
    const seq = this.sequence;
    this.sequence = null;
    this.timeScale = 1;
    this.camera.position.copy(this.home.position);
    this.camera.lookAt(this.home.lookAt);
    this.emit('end', { label: seq.label, kind: seq.kind });
  }

  /** Ruhelage nachziehen (z. B. wenn die Kamera im Idle leicht schwingt). */
  setHome(position, lookAt) {
    this.home.position.copy(position);
    if (lookAt) this.home.lookAt.copy(lookAt);
  }

  /**
   * @param {number} rawDt echte, ungeskalierte Sekunden seit dem letzten Frame
   * @returns {number} dt für die Spiellogik (0 bei Hitstop, gedehnt in Zeitlupe)
   */
  update(rawDt) {
    // 1 — Hitstop friert die Spiellogik komplett ein, Kamera rüttelt weiter
    if (this.hitstopTimer > 0) {
      this.hitstopTimer -= rawDt;
      this.#applyShake(rawDt);
      return 0;
    }

    // 2 — Kamerafahrt
    if (this.sequence) {
      const seq = this.sequence;
      seq.t += rawDt;
      const k = Math.min(1, seq.t / seq.duration);
      this.#applyPath(seq, k);

      if (!seq.zoomFired && seq.t >= seq.zoomAt) {
        seq.zoomFired = true;
        this.punchZoom(0.5);
        this.emit('zoom', { label: seq.label, kind: seq.kind });
      }
      if (k >= 1) this.cancel();
      this.#applyShake(rawDt);
      return rawDt * this.timeScale;
    }

    // 3 — Normalbetrieb
    this.#applyShake(rawDt);
    return rawDt * this.timeScale;
  }

  #applyPath(seq, k) {
    const ease = k < 0.5 ? 2 * k * k : 1 - Math.pow(-2 * k + 2, 2) / 2;
    const d = seq.def;
    const v = seq.victim;
    const dirToAttacker = this._tmp.copy(seq.attacker).sub(v).setY(0);
    const baseAngle = Math.atan2(dirToAttacker.z, dirToAttacker.x);
    let pos;

    if (d.type === 'orbit') {
      const a = baseAngle + ease * Math.PI * 2 * d.turns;
      pos = new THREE.Vector3(v.x + Math.cos(a) * d.radius, v.y + d.height, v.z + Math.sin(a) * d.radius);
    } else if (d.type === 'push') {
      const dist = d.from + (d.to - d.from) * ease;
      pos = new THREE.Vector3(v.x + Math.cos(baseAngle) * dist, v.y + d.height, v.z + Math.sin(baseAngle) * dist);
    } else if (d.type === 'rise') {
      const a = baseAngle + ease * 0.9;
      const h = d.from + (d.to - d.from) * ease;
      pos = new THREE.Vector3(v.x + Math.cos(a) * d.radius, v.y + h, v.z + Math.sin(a) * d.radius);
    } else { // slide
      const a = baseAngle - d.arc + ease * d.arc * 2;
      pos = new THREE.Vector3(v.x + Math.cos(a) * d.radius, v.y + d.height, v.z + Math.sin(a) * d.radius);
    }

    this.camera.position.copy(pos);
    this.camera.lookAt(v.x, v.y + 1.0, v.z);
  }

  #applyShake(rawDt) {
    if (this.zoomPunch > 0) {
      const fovBase = this.camera.userData.baseFov ?? (this.camera.userData.baseFov = this.camera.fov);
      this.camera.fov = fovBase - this.zoomPunch * 8;
      this.camera.updateProjectionMatrix();
      this.zoomPunch = Math.max(0, this.zoomPunch - rawDt * 2.2);
      if (this.zoomPunch === 0) { this.camera.fov = fovBase; this.camera.updateProjectionMatrix(); }
    }
    if (this.shakeTime <= 0) { this.shakeStrength = 0; return; }
    this.shakeTime -= rawDt;
    const s = this.shakeStrength * Math.max(0, this.shakeTime) * 0.9;
    this.camera.position.x += (Math.random() - 0.5) * s;
    this.camera.position.y += (Math.random() - 0.5) * s;
    if (this.shakeTime <= 0) this.shakeStrength = 0;
  }
}
