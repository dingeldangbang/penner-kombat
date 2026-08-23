/**
 * stage.js — Interaktive Arena-Objekte (Stage Interactions)
 *
 * Brennende Fässer, Kisten, Gasflaschen, Mauervorsprünge: Die KI legt per JSON
 * fest, welche Objekte in der Arena stehen und welche Rolle sie haben.
 *
 *   THROWABLE  — greifbar und als Geschoss auf den Gegner werfbar
 *   ESCAPE_PAD — Absprungpunkt für einen Wandsprung
 *   HAZARD     — schadet, wer zu nah steht
 */

import * as THREE from 'three';
import { STAGE_OBJECTS } from './assetLibrary.js';

export class StageProps {
  constructor(scene, particles) {
    this.scene = scene;
    this.particles = particles;
    this.props = [];      // {key, def, role, mesh, used, damage, interactionInput}
    this.flying = [];     // {mesh, vel, target, damage, def, t}
    this.listeners = {};
  }

  on(evt, fn) { (this.listeners[evt] ||= []).push(fn); return this; }
  emit(evt, p) { (this.listeners[evt] || []).forEach((f) => f(p)); }

  /** Baut die Arena-Objekte aus dem validierten JSON-Block neu auf. */
  build(interactions = []) {
    this.clear();
    for (const item of interactions) {
      const def = STAGE_OBJECTS[item.object];
      if (!def) continue;
      const mesh = new THREE.Mesh(
        new THREE.BoxGeometry(...def.size),
        new THREE.MeshStandardMaterial({
          color: def.color,
          roughness: 0.8,
          emissive: item.role === 'HAZARD' ? new THREE.Color(def.color) : new THREE.Color(0x000000),
          emissiveIntensity: item.role === 'HAZARD' ? 0.6 : 0,
        }),
      );
      mesh.position.set(item.position[0], def.size[1] / 2, item.position[1]);
      mesh.castShadow = true;
      this.scene.add(mesh);
      this.props.push({
        key: item.object, def, role: item.role, mesh, used: false,
        damage: item.damage, interactionInput: item.interactionInput,
        home: mesh.position.clone(),
      });
    }
    return this.props.length;
  }

  /** Alle Interaktions-Eingaben, damit der Kämpfer sie in den Puffer aufnimmt. */
  inputBindings() {
    return this.props.map((p) => ({ sequence: p.interactionInput, key: p.key, role: p.role }));
  }

  /** Nächstes nutzbares Objekt in Reichweite. */
  nearest(position, maxDistance = 3.2, role = null) {
    let best = null;
    for (const p of this.props) {
      if (p.used || (role && p.role !== role)) continue;
      const d = p.mesh.position.distanceTo(position);
      if (d <= maxDistance && (!best || d < best.d)) best = { prop: p, d };
    }
    return best ? best.prop : null;
  }

  /**
   * Wirft ein Objekt auf den Gegner (THROWABLE) bzw. löst Wandsprung/Hazard aus.
   * @returns {object|null} Beschreibung der Interaktion
   */
  interact(fighterPos, targetPos, role = 'THROWABLE') {
    const prop = this.nearest(fighterPos, 3.2, role);
    if (!prop) return null;

    if (prop.role === 'THROWABLE') {
      prop.used = true;
      const dir = targetPos.clone().sub(prop.mesh.position).setY(0).normalize();
      this.flying.push({
        mesh: prop.mesh,
        vel: dir.multiplyScalar(13).setY(3.5),
        target: targetPos,
        damage: prop.damage,
        def: prop.def,
        prop,
        t: 0,
      });
      this.emit('throw', { object: prop.key, damage: prop.damage });
      return { type: 'THROW', object: prop.key, damage: prop.damage };
    }

    if (prop.role === 'ESCAPE_PAD') {
      this.emit('escape', { object: prop.key });
      if (this.particles) this.particles.spawn('dust_burst', prop.mesh.position.clone(), { scale: 0.6 });
      return { type: 'ESCAPE', object: prop.key, damage: 0 };
    }

    this.emit('hazard', { object: prop.key, damage: prop.damage });
    if (this.particles) this.particles.spawn(prop.def.vfx, prop.mesh.position.clone(), { scale: 0.8 });
    return { type: 'HAZARD', object: prop.key, damage: prop.damage };
  }

  update(dt, targetObj) {
    for (let i = this.flying.length - 1; i >= 0; i--) {
      const f = this.flying[i];
      f.t += dt;
      f.vel.y -= 18 * dt;
      f.mesh.position.addScaledVector(f.vel, dt);
      f.mesh.rotation.x += 7 * dt;
      f.mesh.rotation.z += 5 * dt;

      const hitTarget = targetObj && f.mesh.position.distanceTo(targetObj.position) < 1.1;
      if (hitTarget || f.mesh.position.y <= 0.2 || f.t > 3) {
        if (this.particles) this.particles.spawn(f.def.vfx, f.mesh.position.clone().setY(1.0), { scale: 0.9 });
        if (hitTarget) this.emit('hit', { object: f.prop.key, damage: f.damage, sfx: f.def.sfx });
        this.scene.remove(f.mesh);
        f.mesh.geometry.dispose();
        f.mesh.material.dispose();
        this.flying.splice(i, 1);
      }
    }
  }

  clear() {
    for (const p of this.props) {
      this.scene.remove(p.mesh);
      p.mesh.geometry.dispose();
      p.mesh.material.dispose();
    }
    for (const f of this.flying) this.scene.remove(f.mesh);
    this.props = [];
    this.flying = [];
  }
}
