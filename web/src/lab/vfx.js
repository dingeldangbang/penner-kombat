/**
 * vfx.js — Vorkompilierte Partikel-Bausteine (Asset-Pool)
 *
 * Ein einziges Points-Objekt pro Emission, recycelt über einen Pool. Die KI
 * wählt nur den Namen aus VFX_LIBRARY — die Geometrie/Shader stehen fest.
 */

import * as THREE from 'three';
import { VFX_LIBRARY } from './assetLibrary.js';

function sprite() {
  const c = document.createElement('canvas');
  c.width = c.height = 64;
  const g = c.getContext('2d');
  const grd = g.createRadialGradient(32, 32, 0, 32, 32, 32);
  grd.addColorStop(0, 'rgba(255,255,255,1)');
  grd.addColorStop(0.4, 'rgba(255,255,255,0.55)');
  grd.addColorStop(1, 'rgba(255,255,255,0)');
  g.fillStyle = grd;
  g.fillRect(0, 0, 64, 64);
  const t = new THREE.CanvasTexture(c);
  t.colorSpace = THREE.SRGBColorSpace;
  return t;
}

export class ParticleSystem {
  constructor(scene) {
    this.scene = scene;
    this.tex = sprite();
    this.active = [];
    this.pool = [];
  }

  /**
   * @param {string} assetName Schlüssel aus VFX_LIBRARY
   * @param {THREE.Vector3} position
   * @param {{direction?:THREE.Vector3, scale?:number}} opts
   */
  spawn(assetName, position, opts = {}) {
    const def = VFX_LIBRARY[assetName] || VFX_LIBRARY.fire_blast;
    const dir = (opts.direction || new THREE.Vector3(1, 0, 0)).clone().normalize();
    const count = Math.round(def.count * (opts.scale || 1));

    const emitter = this.pool.pop() || this.#create();
    const pos = new Float32Array(count * 3);
    const col = new Float32Array(count * 3);
    const vel = new Float32Array(count * 3);

    const cA = new THREE.Color(def.color);
    const cB = new THREE.Color(def.color2);
    const cone = def.cone ?? 1.0;

    for (let i = 0; i < count; i++) {
      if (def.inward) {
        // Sog: Partikel starten im Ring und fliegen zum Zentrum
        const a = Math.random() * Math.PI * 2;
        const r = 1.6 + Math.random() * 1.8;
        pos[i * 3] = position.x + Math.cos(a) * r;
        pos[i * 3 + 1] = position.y + (Math.random() - 0.4) * 1.2;
        pos[i * 3 + 2] = position.z + Math.sin(a) * r;
        const inV = new THREE.Vector3(position.x - pos[i * 3], position.y - pos[i * 3 + 1], position.z - pos[i * 3 + 2])
          .normalize().multiplyScalar(def.speed * (0.6 + Math.random() * 0.6));
        vel[i * 3] = inV.x; vel[i * 3 + 1] = inV.y; vel[i * 3 + 2] = inV.z;
        const ci = cA.clone().lerp(cB, Math.random());
        col[i * 3] = ci.r; col[i * 3 + 1] = ci.g; col[i * 3 + 2] = ci.b;
        continue;
      }
      pos[i * 3] = position.x; pos[i * 3 + 1] = position.y; pos[i * 3 + 2] = position.z;
      const spread = def.travels ? cone : 1.0;
      const v = new THREE.Vector3(
        dir.x + (Math.random() - 0.5) * spread,
        dir.y + (Math.random() - 0.5) * spread + (def.travels ? 0.15 : 0.9),
        dir.z + (Math.random() - 0.5) * spread,
      ).normalize().multiplyScalar(def.speed * (0.45 + Math.random() * 0.75));
      vel[i * 3] = v.x; vel[i * 3 + 1] = v.y; vel[i * 3 + 2] = v.z;
      const c = cA.clone().lerp(cB, Math.random());
      col[i * 3] = c.r; col[i * 3 + 1] = c.g; col[i * 3 + 2] = c.b;
    }

    const geo = emitter.points.geometry;
    geo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    geo.setAttribute('color', new THREE.BufferAttribute(col, 3));
    geo.setDrawRange(0, count);
    emitter.points.material.size = def.size * (opts.scale || 1);
    emitter.points.material.blending = def.blend === 'add' ? THREE.AdditiveBlending : THREE.NormalBlending;
    emitter.points.material.opacity = 1;
    emitter.points.visible = true;
    emitter.vel = vel;
    emitter.count = count;
    emitter.life = def.life;
    emitter.age = 0;
    emitter.gravity = def.gravity;
    this.active.push(emitter);
    return emitter;
  }

  #create() {
    const geo = new THREE.BufferGeometry();
    const mat = new THREE.PointsMaterial({
      size: 0.2, map: this.tex, vertexColors: true, transparent: true,
      depthWrite: false, sizeAttenuation: true,
    });
    const points = new THREE.Points(geo, mat);
    points.frustumCulled = false;
    this.scene.add(points);
    return { points, vel: null, count: 0, life: 1, age: 0, gravity: 0 };
  }

  update(dt) {
    for (let i = this.active.length - 1; i >= 0; i--) {
      const e = this.active[i];
      e.age += dt;
      const attr = e.points.geometry.getAttribute('position');
      const arr = attr.array;
      for (let p = 0; p < e.count; p++) {
        e.vel[p * 3 + 1] -= e.gravity * dt;
        arr[p * 3] += e.vel[p * 3] * dt;
        arr[p * 3 + 1] += e.vel[p * 3 + 1] * dt;
        arr[p * 3 + 2] += e.vel[p * 3 + 2] * dt;
        if (arr[p * 3 + 1] < 0.02) { arr[p * 3 + 1] = 0.02; e.vel[p * 3 + 1] *= -0.25; }
      }
      attr.needsUpdate = true;
      e.points.material.opacity = Math.max(0, 1 - e.age / e.life);
      if (e.age >= e.life) {
        e.points.visible = false;
        this.active.splice(i, 1);
        if (this.pool.length < 12) this.pool.push(e);
        else { this.scene.remove(e.points); e.points.geometry.dispose(); e.points.material.dispose(); }
      }
    }
  }

  clear() {
    for (const e of this.active) { e.points.visible = false; this.pool.push(e); }
    this.active.length = 0;
  }
}
