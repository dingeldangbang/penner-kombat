/**
 * render.js — 2,5D-Darstellung auf einem Canvas.
 *
 * Kein WebGL, keine Bibliothek: Weltkoordinaten (x, y, z) werden perspektivisch
 * projiziert, Kämpfer als Kapseln gezeichnet — genau der Zustand, den auch die
 * Unity-Fassung ohne Modelle zeigt.
 */

import { PALETTE, K } from './data.js';

export class Renderer {
  constructor(canvas) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.shakeTime = 0;
    this.shakeStrength = 0;
    this.hitStop = 0;
    this.floaters = [];
    this.sparks = [];
    this.resize();
    window.addEventListener('resize', () => this.resize());
  }

  resize() {
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    this.w = this.canvas.clientWidth;
    this.h = this.canvas.clientHeight;
    this.canvas.width = Math.floor(this.w * dpr);
    this.canvas.height = Math.floor(this.h * dpr);
    this.ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }

  shake(strength, time) {
    this.shakeStrength = Math.max(this.shakeStrength, strength);
    this.shakeTime = Math.max(this.shakeTime, time);
  }

  freeze(seconds) { this.hitStop = Math.max(this.hitStop, seconds); }

  floatText(text, world, color) {
    this.floaters.push({ text, x: world.x, y: world.y + 2.2, z: world.z, color, t: 0.9 });
  }

  burst(world, color, count = 10) {
    for (let i = 0; i < count; i++) {
      const a = Math.random() * Math.PI * 2;
      this.sparks.push({
        x: world.x, y: world.y + 1, z: world.z,
        vx: Math.cos(a) * (1 + Math.random() * 3),
        vy: 1 + Math.random() * 4,
        vz: Math.sin(a) * (1 + Math.random() * 3),
        color, t: 0.5 + Math.random() * 0.3,
      });
    }
  }

  /** Weltpunkt → Bildschirmpunkt. Kamera schaut leicht von oben nach vorn. */
  project(x, y, z, cam) {
    const camHeight = 4.2;
    const camDist = cam.distance;
    const relX = x - cam.x;
    const relZ = z - cam.z + camDist;
    const depth = Math.max(1.2, relZ);
    const scale = (this.h * 0.62) / depth;
    return {
      sx: this.w / 2 + relX * scale,
      sy: this.h * 0.58 - (y - camHeight * 0.0) * scale + (camHeight * scale * 0.18),
      scale,
      depth,
    };
  }

  draw(match, dt) {
    const c = this.ctx;

    if (this.shakeTime > 0) this.shakeTime -= dt;
    if (this.hitStop > 0) this.hitStop -= dt;

    const mid = { x: (match.p1.x + match.p2.x) / 2, z: (match.p1.z + match.p2.z) / 2 };
    const spread = Math.hypot(match.p1.x - match.p2.x, match.p1.z - match.p2.z);
    const cam = { x: mid.x * 0.6, z: mid.z * 0.4, distance: 11 + spread * 0.55 };

    c.save();
    if (this.shakeTime > 0) {
      const s = this.shakeStrength * (this.shakeTime / 0.2);
      c.translate((Math.random() - 0.5) * s, (Math.random() - 0.5) * s);
    }

    this.drawBackground(c);
    this.drawGround(c, cam);

    const order = [match.p1, match.p2].sort((a, b) => b.z - a.z);
    for (const f of order) this.drawFighter(c, f, cam);

    this.drawSparks(c, cam, dt);
    this.drawFloaters(c, cam, dt);
    c.restore();
  }

  drawBackground(c) {
    const g = c.createLinearGradient(0, 0, 0, this.h);
    g.addColorStop(0, '#0b0d16');
    g.addColorStop(0.55, PALETTE.nightBlue);
    g.addColorStop(1, '#101320');
    c.fillStyle = g;
    c.fillRect(0, 0, this.w, this.h);

    // Neonschild „Zum Blauen Eimer"
    const flicker = 0.75 + Math.random() * 0.25;
    c.globalAlpha = 0.55 * flicker;
    c.fillStyle = PALETTE.neonBlue;
    c.font = `bold ${Math.round(this.h * 0.045)}px system-ui, sans-serif`;
    c.textAlign = 'center';
    c.fillText('ZUM BLAUEN EIMER', this.w * 0.5, this.h * 0.16);
    c.globalAlpha = 1;

    // Laternenlicht
    const lamp = c.createRadialGradient(this.w * 0.2, this.h * 0.1, 10, this.w * 0.2, this.h * 0.1, this.h * 0.7);
    lamp.addColorStop(0, 'rgba(255,107,0,0.28)');
    lamp.addColorStop(1, 'rgba(255,107,0,0)');
    c.fillStyle = lamp;
    c.fillRect(0, 0, this.w, this.h);
  }

  drawGround(c, cam) {
    const r = K.arenaRadius;
    const corners = [
      this.project(-r, 0, -r, cam), this.project(r, 0, -r, cam),
      this.project(r, 0, r, cam), this.project(-r, 0, r, cam),
    ];
    c.beginPath();
    c.moveTo(corners[0].sx, corners[0].sy);
    for (let i = 1; i < corners.length; i++) c.lineTo(corners[i].sx, corners[i].sy);
    c.closePath();
    c.fillStyle = '#14161f';
    c.fill();

    // Pflasterlinien
    c.strokeStyle = 'rgba(255,255,255,0.06)';
    c.lineWidth = 1;
    for (let i = -r; i <= r; i += 2) {
      const a = this.project(i, 0, -r, cam), b = this.project(i, 0, r, cam);
      c.beginPath(); c.moveTo(a.sx, a.sy); c.lineTo(b.sx, b.sy); c.stroke();
      const d = this.project(-r, 0, i, cam), e = this.project(r, 0, i, cam);
      c.beginPath(); c.moveTo(d.sx, d.sy); c.lineTo(e.sx, e.sy); c.stroke();
    }
  }

  drawFighter(c, f, cam) {
    const feet = this.project(f.x, f.y, f.z, cam);
    const head = this.project(f.x, f.y + f.height, f.z, cam);
    const px = feet.sx;
    const bodyH = feet.sy - head.sy;
    const bodyW = f.radius * 2 * feet.scale;

    // Schatten
    c.save();
    c.globalAlpha = 0.4;
    c.fillStyle = '#000';
    c.beginPath();
    const ground = this.project(f.x, 0, f.z, cam);
    c.ellipse(ground.sx, ground.sy, bodyW * 0.55, bodyW * 0.22, 0, 0, Math.PI * 2);
    c.fill();
    c.restore();

    const color = f.flash > 0 ? '#ffffff' : f.cfg.color;

    // Körper (Kapsel)
    c.fillStyle = color;
    this.roundedBody(c, px, head.sy, bodyW, bodyH);

    // Kopf
    c.beginPath();
    c.fillStyle = f.flash > 0 ? '#ffffff' : shade(f.cfg.color, 0.25);
    c.arc(px, head.sy + bodyW * 0.1, bodyW * 0.34, 0, Math.PI * 2);
    c.fill();

    // Blickrichtung (Nase)
    c.fillStyle = PALETTE.gold;
    c.fillRect(px + f.facing * bodyW * 0.28 - 2, head.sy + bodyW * 0.05, 4, 4);

    // Block-Schild
    if (f.blocking) {
      c.strokeStyle = PALETTE.neonBlue;
      c.lineWidth = 3;
      c.globalAlpha = 0.8;
      c.beginPath();
      c.arc(px, head.sy + bodyH * 0.5, bodyW * 0.75, -Math.PI / 2, Math.PI / 2, f.facing < 0);
      c.stroke();
      c.globalAlpha = 1;
    }

    // Angriffsbogen
    if (f.attacking) {
      const a = f.attacking;
      const progress = Math.min(1, a.t / a.endsAt);
      c.strokeStyle = a.kind === 'fatal' ? PALETTE.bloodRed
        : a.kind === 'special' ? PALETTE.gold : PALETTE.warmOrange;
      c.lineWidth = a.kind === 'light' ? 3 : 5;
      c.globalAlpha = 1 - progress;
      c.beginPath();
      c.arc(px, head.sy + bodyH * 0.45,
        a.range * feet.scale * 0.5,
        f.facing > 0 ? -0.7 : Math.PI - 0.7,
        f.facing > 0 ? 0.7 : Math.PI + 0.7);
      c.stroke();
      c.globalAlpha = 1;
    }

    // Rolle: Nachzieheffekt
    if (f.rollTimer > 0) {
      c.globalAlpha = 0.3;
      c.fillStyle = color;
      this.roundedBody(c, px - f.rollDir.x * 18, head.sy, bodyW * 0.8, bodyH * 0.8);
      c.globalAlpha = 1;
    }

    // Med-Anzeige über dem Kopf
    if (f.medTimer > 0) {
      c.fillStyle = PALETTE.neonBlue;
      c.font = '14px system-ui';
      c.textAlign = 'center';
      c.fillText('MED', px, head.sy - 12);
    }
  }

  roundedBody(c, x, top, w, h) {
    const r = w / 2;
    c.beginPath();
    c.moveTo(x - r, top + r);
    c.arc(x, top + r, r, Math.PI, 0);
    c.lineTo(x + r, top + h - r);
    c.arc(x, top + h - r, r, 0, Math.PI);
    c.closePath();
    c.fill();
  }

  drawSparks(c, cam, dt) {
    for (let i = this.sparks.length - 1; i >= 0; i--) {
      const s = this.sparks[i];
      s.t -= dt;
      if (s.t <= 0) { this.sparks.splice(i, 1); continue; }
      s.x += s.vx * dt; s.y += s.vy * dt; s.z += s.vz * dt;
      s.vy -= 12 * dt;
      const p = this.project(s.x, Math.max(0, s.y), s.z, cam);
      c.globalAlpha = Math.max(0, s.t * 2);
      c.fillStyle = s.color;
      const size = Math.max(2, p.scale * 0.05);
      c.fillRect(p.sx, p.sy, size, size);
      c.globalAlpha = 1;
    }
  }

  drawFloaters(c, cam, dt) {
    c.textAlign = 'center';
    for (let i = this.floaters.length - 1; i >= 0; i--) {
      const f = this.floaters[i];
      f.t -= dt;
      if (f.t <= 0) { this.floaters.splice(i, 1); continue; }
      f.y += dt * 1.4;
      const p = this.project(f.x, f.y, f.z, cam);
      c.globalAlpha = Math.min(1, f.t * 2);
      c.fillStyle = f.color;
      c.font = `bold ${Math.max(14, Math.round(p.scale * 0.22))}px system-ui, sans-serif`;
      c.fillText(f.text, p.sx, p.sy);
      c.globalAlpha = 1;
    }
  }
}

function shade(hex, amount) {
  const n = parseInt(hex.slice(1), 16);
  const r = Math.min(255, ((n >> 16) & 255) + amount * 255);
  const g = Math.min(255, ((n >> 8) & 255) + amount * 255);
  const b = Math.min(255, (n & 255) + amount * 255);
  return `rgb(${r | 0},${g | 0},${b | 0})`;
}
