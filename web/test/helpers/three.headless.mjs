/**
 * three.headless.mjs — Test-Doppel für den WebGL-Teil von three.js
 *
 * Re-exportiert das echte three.js, ersetzt aber `WebGLRenderer` durch eine
 * Attrappe. Damit lässt sich `src/lab/main.js` komplett in jsdom ausführen —
 * Szenengraph, Materialien und Vektormathematik bleiben echt, nur das Zeichnen
 * entfällt.
 */

export * from 'three';
import * as THREE from 'three';

export class WebGLRenderer {
  constructor(params = {}) {
    this.domElement = params.canvas || { style: {} };
    this.shadowMap = { enabled: false, type: 0 };
    this.outputColorSpace = '';
    this.info = { render: { calls: 0 } };
    this.calls = 0;
    this.size = { width: 0, height: 0 };
    this.pixelRatio = 1;
  }
  setPixelRatio(r) { this.pixelRatio = r; }
  setSize(w, h, updateStyle = true) {
    this.size = { width: w, height: h };
    if (this.domElement) { this.domElement.width = w; this.domElement.height = h; }
    if (updateStyle && this.domElement && this.domElement.style) {
      this.domElement.style.width = w + 'px';
      this.domElement.style.height = h + 'px';
    }
  }
  getSize(target = new THREE.Vector2()) { return target.set(this.size.width, this.size.height); }
  render() { this.calls++; this.info.render.calls++; }
  dispose() {}
}
