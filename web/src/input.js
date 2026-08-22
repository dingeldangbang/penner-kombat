/**
 * input.js — Tastatur und Touch.
 *
 * Touch spiegelt das Layout aus docs/TOUCH.md: Joystick links,
 * Buttons rechts (□ leicht, △ schwer, ○ Spezial 1, ✕ Sprung, BLOCK halten,
 * S2, X-RAY, MED, ROLLE).
 */

export class InputState {
  constructor() {
    this.keys = new Set();
    this.pressed = new Set();
    this.touch = { dx: 0, dz: 0, buttons: new Set(), tapped: new Set() };
    this.bindKeyboard();
  }

  bindKeyboard() {
    window.addEventListener('keydown', (e) => {
      if (!this.keys.has(e.code)) this.pressed.add(e.code);
      this.keys.add(e.code);
      if (['Space', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(e.code)) e.preventDefault();
    });
    window.addEventListener('keyup', (e) => this.keys.delete(e.code));
    window.addEventListener('blur', () => this.keys.clear());
  }

  /** Spieler 1: WASD + J/K/Shift/Space/U/I/O/H — wie FighterInput.cs. */
  readP1() {
    const k = this.keys, p = this.pressed, t = this.touch;
    const dx = (k.has('KeyD') ? 1 : 0) - (k.has('KeyA') ? 1 : 0) + t.dx;
    const dz = (k.has('KeyW') ? 1 : 0) - (k.has('KeyS') ? 1 : 0) + t.dz;
    return {
      dx, dz,
      block: k.has('ShiftLeft') || k.has('ShiftRight') || t.buttons.has('block'),
      jump: p.has('Space') || t.tapped.has('jump'),
      light: p.has('KeyJ') || t.tapped.has('light'),
      heavy: p.has('KeyK') || t.tapped.has('heavy'),
      special1: p.has('KeyU') || t.tapped.has('s1'),
      special2: p.has('KeyI') || t.tapped.has('s2'),
      roll: p.has('KeyO') || t.tapped.has('roll'),
      fatal: p.has('KeyY') || t.tapped.has('fatal'),
      med: p.has('KeyH') || t.tapped.has('med'),
    };
  }

  /** Spieler 2: Pfeiltasten + Numpad — wie FighterInput.cs. */
  readP2() {
    const k = this.keys, p = this.pressed;
    return {
      dx: (k.has('ArrowRight') ? 1 : 0) - (k.has('ArrowLeft') ? 1 : 0),
      dz: (k.has('ArrowUp') ? 1 : 0) - (k.has('ArrowDown') ? 1 : 0),
      block: k.has('Numpad3'),
      jump: p.has('Numpad0'),
      light: p.has('Numpad1'),
      heavy: p.has('Numpad2'),
      special1: p.has('Numpad4'),
      special2: p.has('Numpad5'),
      roll: p.has('Numpad6'),
      fatal: p.has('NumpadAdd'),
      med: p.has('NumpadEnter'),
    };
  }

  endFrame() {
    this.pressed.clear();
    this.touch.tapped.clear();
  }
}

/** Baut Joystick und Buttons als DOM-Elemente über dem Canvas. */
export function buildTouchControls(root, input, onAny) {
  const layer = document.createElement('div');
  layer.className = 'touch-layer';

  // --- Joystick ---
  const stick = document.createElement('div');
  stick.className = 'joystick';
  const knob = document.createElement('div');
  knob.className = 'knob';
  stick.appendChild(knob);
  layer.appendChild(stick);

  let stickId = null, originX = 0, originY = 0;
  const radius = 55;

  const startStick = (e) => {
    const t = e.changedTouches ? e.changedTouches[0] : e;
    stickId = t.identifier ?? 'mouse';
    const rect = stick.getBoundingClientRect();
    originX = rect.left + rect.width / 2;
    originY = rect.top + rect.height / 2;
    onAny();
    e.preventDefault();
  };

  const moveStick = (e) => {
    if (stickId === null) return;
    const touches = e.changedTouches ? Array.from(e.changedTouches) : [e];
    const t = touches.find((x) => (x.identifier ?? 'mouse') === stickId);
    if (!t) return;
    let dx = t.clientX - originX;
    let dy = t.clientY - originY;
    const len = Math.hypot(dx, dy) || 1;
    const clamped = Math.min(len, radius);
    dx = (dx / len) * clamped;
    dy = (dy / len) * clamped;
    knob.style.transform = `translate(${dx}px, ${dy}px)`;
    const dead = 0.18;
    const nx = dx / radius, ny = -dy / radius;
    input.touch.dx = Math.abs(nx) > dead ? nx : 0;
    input.touch.dz = Math.abs(ny) > dead ? ny : 0;
    e.preventDefault();
  };

  const endStick = () => {
    stickId = null;
    knob.style.transform = 'translate(0,0)';
    input.touch.dx = 0;
    input.touch.dz = 0;
  };

  stick.addEventListener('touchstart', startStick, { passive: false });
  stick.addEventListener('touchmove', moveStick, { passive: false });
  stick.addEventListener('touchend', endStick);
  stick.addEventListener('touchcancel', endStick);
  stick.addEventListener('mousedown', startStick);
  window.addEventListener('mousemove', moveStick);
  window.addEventListener('mouseup', endStick);

  // --- Buttons ---
  const pad = document.createElement('div');
  pad.className = 'buttonpad';
  const defs = [
    { id: 'light', label: '□', title: 'leicht', cls: 'b-light' },
    { id: 'heavy', label: '△', title: 'schwer', cls: 'b-heavy' },
    { id: 's1', label: '○', title: 'Spezial 1', cls: 'b-s1' },
    { id: 'jump', label: '✕', title: 'Sprung', cls: 'b-jump' },
    { id: 'block', label: 'BLOCK', title: 'halten', cls: 'b-block', hold: true },
    { id: 's2', label: 'S2', title: 'Spezial 2', cls: 'b-s2' },
    { id: 'roll', label: 'ROLLE', title: 'ausweichen', cls: 'b-roll' },
    { id: 'med', label: 'MED', title: 'Kapsel', cls: 'b-med' },
    { id: 'fatal', label: 'X-RAY', title: 'Fatal Blow', cls: 'b-fatal' },
  ];

  for (const d of defs) {
    const b = document.createElement('button');
    b.className = `tbtn ${d.cls}`;
    b.textContent = d.label;
    b.title = d.title;

    const down = (e) => {
      onAny();
      if (d.hold) input.touch.buttons.add(d.id);
      else input.touch.tapped.add(d.id);
      b.classList.add('active');
      if (navigator.vibrate) navigator.vibrate(8);
      e.preventDefault();
    };
    const up = (e) => {
      if (d.hold) input.touch.buttons.delete(d.id);
      b.classList.remove('active');
      if (e) e.preventDefault();
    };

    b.addEventListener('touchstart', down, { passive: false });
    b.addEventListener('touchend', up, { passive: false });
    b.addEventListener('touchcancel', up);
    b.addEventListener('mousedown', down);
    b.addEventListener('mouseup', up);
    b.addEventListener('mouseleave', up);
    pad.appendChild(b);
  }

  layer.appendChild(pad);
  root.appendChild(layer);
  return layer;
}
