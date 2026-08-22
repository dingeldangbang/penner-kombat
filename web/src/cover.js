/**
 * cover.js — Hinterhof-Kulisse im Stil des Cover-Artworks.
 *
 * Zeichnet Backstein, Wäscheleine, Laternen, Neonschild „Zum Blauen Eimer",
 * Bierkästen, Graffiti und Müll auf ein Canvas — rein prozedural, damit kein
 * Bild geladen werden muss.
 *
 * Liegt eine echte Coverdatei unter `assets/cover.jpg`, wird stattdessen die
 * verwendet (siehe loadCoverIfPresent).
 */

import { PALETTE } from './data.js';

export function loadCoverIfPresent(imgEl) {
  return new Promise((resolve) => {
    const img = new Image();
    img.onload = () => { imgEl.src = img.src; imgEl.classList.remove('hidden'); resolve(true); };
    img.onerror = () => resolve(false);
    img.src = 'assets/cover.jpg';
  });
}

export class CoverScene {
  constructor(canvas) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.t = 0;
    this.seedRandom = mulberry(1337);
    this.layout = null;
    this.resize();
    window.addEventListener('resize', () => this.resize());
  }

  resize() {
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    this.w = this.canvas.clientWidth || window.innerWidth;
    this.h = this.canvas.clientHeight || window.innerHeight;
    this.canvas.width = Math.floor(this.w * dpr);
    this.canvas.height = Math.floor(this.h * dpr);
    this.ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    this.layout = this.buildLayout();
  }

  buildLayout() {
    const rnd = mulberry(4711);
    const shirts = [];
    for (let i = 0; i < 9; i++) {
      shirts.push({
        x: 0.06 + i * 0.1,
        w: 0.035 + rnd() * 0.025,
        h: 0.05 + rnd() * 0.045,
        hue: [PALETTE.warmOrange, PALETTE.neonBlue, '#c8c8c8', PALETTE.poisonGreen, '#8f6ad0'][i % 5],
        phase: rnd() * Math.PI * 2,
      });
    }
    const crates = [];
    for (let i = 0; i < 6; i++) {
      crates.push({ x: 0.02 + (i % 2) * 0.055, y: 0.62 + Math.floor(i / 2) * 0.1, hue: i % 3 });
    }
    const litter = [];
    for (let i = 0; i < 18; i++) {
      litter.push({ x: rnd(), y: 0.86 + rnd() * 0.12, r: 2 + rnd() * 4, a: rnd() * Math.PI });
    }
    return { shirts, crates, litter };
  }

  draw(dt) {
    const c = this.ctx;
    this.t += dt;
    const { w, h } = this;

    // --- Nachthimmel / Gasse ---
    const sky = c.createLinearGradient(0, 0, 0, h);
    sky.addColorStop(0, '#0a0c14');
    sky.addColorStop(0.45, PALETTE.nightBlue);
    sky.addColorStop(1, '#0d0f18');
    c.fillStyle = sky;
    c.fillRect(0, 0, w, h);

    this.drawBrick(c, w, h);
    this.drawGraffiti(c, w, h);
    this.drawNeon(c, w, h);
    this.drawLine(c, w, h);
    this.drawLanterns(c, w, h);
    this.drawCrates(c, w, h);
    this.drawGasBottle(c, w, h);
    this.drawFloor(c, w, h);
    this.drawVignette(c, w, h);
  }

  drawBrick(c, w, h) {
    const brickH = h * 0.028;
    const brickW = brickH * 2.4;
    c.save();
    c.globalAlpha = 0.5;
    for (let y = 0, row = 0; y < h * 0.9; y += brickH, row++) {
      for (let x = (row % 2) * -brickW / 2; x < w; x += brickW) {
        const shade = 0.10 + this.seeded(x, y) * 0.08;
        c.fillStyle = `rgb(${Math.round(60 * shade * 6)},${Math.round(40 * shade * 6)},${Math.round(42 * shade * 6)})`;
        c.fillRect(x + 1, y + 1, brickW - 2, brickH - 2);
      }
    }
    c.restore();
  }

  drawGraffiti(c, w, h) {
    c.save();
    c.globalAlpha = 0.5;
    c.font = `italic bold ${Math.round(h * 0.13)}px system-ui, sans-serif`;
    c.textAlign = 'center';
    const g = c.createLinearGradient(w * 0.2, 0, w * 0.8, 0);
    g.addColorStop(0, '#ff2fbe');
    g.addColorStop(0.5, PALETTE.gold);
    g.addColorStop(1, PALETTE.poisonGreen);
    c.fillStyle = g;
    c.fillText('DINGELANG', w * 0.5, h * 0.58);
    c.restore();
  }

  drawNeon(c, w, h) {
    const flicker = 0.72 + Math.abs(Math.sin(this.t * 7.3)) * 0.28;
    const boxW = w * 0.42, boxH = h * 0.075;
    const x = w * 0.5 - boxW / 2, y = h * 0.24;

    c.save();
    c.globalAlpha = 0.25 * flicker;
    c.fillStyle = PALETTE.neonBlue;
    c.fillRect(x - 12, y - 12, boxW + 24, boxH + 24);
    c.globalAlpha = 1;

    c.strokeStyle = `rgba(255,110,220,${flicker})`;
    c.lineWidth = 3;
    c.strokeRect(x, y, boxW, boxH);

    c.shadowColor = PALETTE.neonBlue;
    c.shadowBlur = 24 * flicker;
    c.fillStyle = `rgba(180,240,255,${flicker})`;
    c.font = `italic ${Math.round(boxH * 0.62)}px "Brush Script MT", cursive, system-ui`;
    c.textAlign = 'center';
    c.textBaseline = 'middle';
    c.fillText('Zum Blauen Eimer', w * 0.5, y + boxH / 2);
    c.restore();
  }

  drawLine(c, w, h) {
    const y = h * 0.085;
    c.strokeStyle = 'rgba(210,200,180,0.8)';
    c.lineWidth = 2;
    c.beginPath();
    c.moveTo(0, y);
    c.quadraticCurveTo(w * 0.5, y + h * 0.03, w, y - h * 0.01);
    c.stroke();

    for (const s of this.layout.shirts) {
      const sway = Math.sin(this.t * 1.2 + s.phase) * 3;
      const px = s.x * w + sway;
      const py = y + Math.sin((s.x) * Math.PI) * h * 0.03;
      c.fillStyle = s.hue;
      c.globalAlpha = 0.85;
      c.fillRect(px, py, s.w * w, s.h * h);
      c.globalAlpha = 1;
    }
  }

  drawLanterns(c, w, h) {
    for (const lx of [0.13, 0.87]) {
      const x = lx * w, y = h * 0.28;
      const pulse = 0.85 + Math.sin(this.t * 2.1 + lx * 10) * 0.15;

      const glow = c.createRadialGradient(x, y, 4, x, y, h * 0.45);
      glow.addColorStop(0, `rgba(255,150,40,${0.5 * pulse})`);
      glow.addColorStop(1, 'rgba(255,120,0,0)');
      c.fillStyle = glow;
      c.fillRect(x - h * 0.45, y - h * 0.45, h * 0.9, h * 0.9);

      c.fillStyle = '#2a2622';
      c.fillRect(x - 12, y - 14, 24, 28);
      c.fillStyle = `rgba(255,190,90,${pulse})`;
      c.fillRect(x - 8, y - 10, 16, 20);
    }
  }

  drawCrates(c, w, h) {
    for (const cr of this.layout.crates) {
      const x = cr.x * w, y = cr.y * h;
      const cw = w * 0.05, ch = h * 0.085;
      c.fillStyle = ['#7a2b2b', '#2f5c8a', '#7a6a2b'][cr.hue];
      c.fillRect(x, y, cw, ch);
      c.strokeStyle = 'rgba(0,0,0,0.55)';
      c.lineWidth = 2;
      c.strokeRect(x, y, cw, ch);
      c.fillStyle = 'rgba(255,255,255,0.65)';
      c.font = `${Math.round(ch * 0.28)}px system-ui`;
      c.textAlign = 'center';
      c.fillText('BEER', x + cw / 2, y + ch * 0.6);
    }
  }

  drawGasBottle(c, w, h) {
    const x = w * 0.93, y = h * 0.72;
    c.fillStyle = '#6f7f5a';
    c.fillRect(x, y, w * 0.035, h * 0.16);
    c.beginPath();
    c.arc(x + w * 0.0175, y, w * 0.0175, Math.PI, 0);
    c.fill();
    c.fillStyle = '#3a3a3a';
    c.fillRect(x + w * 0.012, y - h * 0.02, w * 0.011, h * 0.02);
  }

  drawFloor(c, w, h) {
    const floor = c.createLinearGradient(0, h * 0.82, 0, h);
    floor.addColorStop(0, '#1c1e26');
    floor.addColorStop(1, '#0c0d12');
    c.fillStyle = floor;
    c.fillRect(0, h * 0.82, w, h * 0.18);

    // Nasse Reflexionen der Laternen
    for (const lx of [0.13, 0.87]) {
      const g = c.createRadialGradient(lx * w, h * 0.95, 2, lx * w, h * 0.95, w * 0.18);
      g.addColorStop(0, 'rgba(255,140,30,0.22)');
      g.addColorStop(1, 'rgba(255,140,30,0)');
      c.fillStyle = g;
      c.fillRect(0, h * 0.8, w, h * 0.2);
    }

    for (const l of this.layout.litter) {
      c.save();
      c.translate(l.x * w, l.y * h);
      c.rotate(l.a);
      c.fillStyle = ['rgba(120,180,90,0.5)', 'rgba(200,200,200,0.35)', 'rgba(160,90,40,0.45)'][Math.floor(l.r) % 3];
      c.fillRect(-l.r, -1.5, l.r * 2.5, 3);
      c.restore();
    }
  }

  drawVignette(c, w, h) {
    const v = c.createRadialGradient(w / 2, h / 2, Math.min(w, h) * 0.25, w / 2, h / 2, Math.max(w, h) * 0.75);
    v.addColorStop(0, 'rgba(0,0,0,0)');
    v.addColorStop(1, 'rgba(0,0,0,0.75)');
    c.fillStyle = v;
    c.fillRect(0, 0, w, h);
  }

  seeded(x, y) {
    const n = Math.sin(x * 12.9898 + y * 78.233) * 43758.5453;
    return n - Math.floor(n);
  }
}

function mulberry(seed) {
  let a = seed;
  return () => {
    a |= 0; a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
