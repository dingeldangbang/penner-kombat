/**
 * res480.js — Harte Rendering-Sperre auf max. 480p
 *
 * Der WebGL-Backbuffer wird fest auf 854x480 (16:9) genagelt — egal ob 4K-Monitor
 * oder Handy. Die sichtbare Größe macht CSS (100% Breite/Höhe), dadurch bleibt die
 * Füllrate konstant niedrig und die Framerate auch auf schwachen Geräten hoch.
 *
 * setPixelRatio(1) ist Pflicht: sonst multipliziert Three.js die Puffergröße
 * auf Retina-Displays wieder hoch.
 */

export const RESOLUTION_PRESETS = {
  '480p': { width: 854, height: 480 },
  '360p': { width: 640, height: 360 },
  '240p': { width: 426, height: 240 },
  '144p': { width: 256, height: 144 },
};

export const MAX_PIXELS = RESOLUTION_PRESETS['480p'].width * RESOLUTION_PRESETS['480p'].height;

/**
 * Klemmt beliebige Wunschmaße auf höchstens 480p (Seitenverhältnis bleibt erhalten).
 */
export function clampTo480p(width, height) {
  let w = Math.max(64, Math.round(width || 854));
  let h = Math.max(36, Math.round(height || 480));
  const pixels = w * h;
  if (pixels > MAX_PIXELS) {
    const f = Math.sqrt(MAX_PIXELS / pixels);
    w = Math.max(64, Math.floor(w * f));
    h = Math.max(36, Math.floor(h * f));
  }
  return { width: w, height: h };
}

/**
 * Sperrt einen Three.js-WebGLRenderer auf das Preset.
 * @param {import('three').WebGLRenderer} renderer
 * @param {HTMLCanvasElement} canvas
 * @param {import('three').PerspectiveCamera} camera
 * @param {keyof typeof RESOLUTION_PRESETS} preset
 */
export function lockResolution(renderer, canvas, camera, preset = '480p') {
  const p = RESOLUTION_PRESETS[preset] || RESOLUTION_PRESETS['480p'];
  const { width, height } = clampTo480p(p.width, p.height);

  renderer.setPixelRatio(1);            // niemals DPR-Skalierung
  renderer.setSize(width, height, false); // false => CSS-Größe unangetastet lassen

  // CSS streckt den kleinen Puffer über die volle Fläche
  canvas.style.width = '100%';
  canvas.style.height = '100%';
  canvas.style.imageRendering = 'pixelated';

  if (camera) {
    camera.aspect = width / height;
    camera.updateProjectionMatrix();
  }
  return { width, height };
}

/**
 * Installiert die Sperre inkl. Resize-Handler. Gibt einen Controller zurück.
 */
export function installResolutionLock(renderer, canvas, camera, preset = '480p') {
  let current = preset;
  const apply = () => lockResolution(renderer, canvas, camera, current);
  let size = apply();
  const onResize = () => { size = apply(); };
  window.addEventListener('resize', onResize);
  window.addEventListener('orientationchange', onResize);
  return {
    get size() { return size; },
    get preset() { return current; },
    set(p) { if (RESOLUTION_PRESETS[p]) { current = p; size = apply(); } return size; },
    dispose() {
      window.removeEventListener('resize', onResize);
      window.removeEventListener('orientationchange', onResize);
    },
  };
}
