/**
 * ragdoll.js — Finisher-Physik: Skelett aus, Ragdoll an
 *
 * Beim Fatality wird die normale Darstellung des Opfers abgeschaltet und durch
 * lose Körperteile ersetzt, die mit einer kleinen Verlet-Integration durch die
 * Arena fliegen (Schwerkraft, Boden-Dämpfung, Drehimpuls). Kein Rapier nötig —
 * genau die Menge Physik, die eine 480p-Szene braucht.
 */

import * as THREE from 'three';
import { FINISHER_TYPES } from './assetLibrary.js';

const PART_SHAPES = {
  head:  { geo: () => new THREE.SphereGeometry(0.24, 10, 8), at: [0, 1.85, 0] },
  torso: { geo: () => new THREE.CapsuleGeometry(0.32, 0.7, 4, 10), at: [0, 1.15, 0] },
  armL:  { geo: () => new THREE.CapsuleGeometry(0.1, 0.5, 4, 8), at: [-0.42, 1.25, 0] },
  armR:  { geo: () => new THREE.CapsuleGeometry(0.1, 0.5, 4, 8), at: [0.42, 1.25, 0] },
  legL:  { geo: () => new THREE.CapsuleGeometry(0.13, 0.55, 4, 8), at: [-0.17, 0.5, 0] },
  legR:  { geo: () => new THREE.CapsuleGeometry(0.13, 0.55, 4, 8), at: [0.17, 0.5, 0] },
};

export class RagdollSystem {
  /**
   * @param {THREE.Scene} scene
   * @param {import('./vfx.js').ParticleSystem} particles
   */
  constructor(scene, particles) {
    this.scene = scene;
    this.particles = particles;
    this.parts = [];
    this.active = false;
    this.hiddenTargets = [];
  }

  /**
   * Zerlegt das Opfer entsprechend des Finisher-Typs.
   * @param {THREE.Object3D} victim
   * @param {string} finisherType Schlüssel aus FINISHER_TYPES
   * @param {THREE.Vector3} impactDir Einschlagsrichtung
   */
  explode(victim, finisherType = 'EXPLOSION', impactDir = new THREE.Vector3(1, 0, 0)) {
    const def = FINISHER_TYPES[finisherType] || FINISHER_TYPES.EXPLOSION;
    this.clear();
    this.active = true;

    // Skelett-Darstellung aus
    victim.visible = false;
    this.hiddenTargets = [victim];

    const origin = victim.position.clone();
    const dir = impactDir.clone().setY(0).normalize();
    const mat = new THREE.MeshStandardMaterial({ color: 0x8a1020, roughness: 0.55, metalness: 0.1 });

    for (const key of def.detach) {
      const shape = PART_SHAPES[key];
      if (!shape) continue;
      const mesh = new THREE.Mesh(shape.geo(), mat);
      mesh.position.set(origin.x + shape.at[0], origin.y + shape.at[1], origin.z + shape.at[2]);
      mesh.castShadow = true;
      this.scene.add(mesh);

      const power = def.force * (0.6 + Math.random() * 0.8);
      this.parts.push({
        mesh,
        vel: new THREE.Vector3(
          dir.x * power * 0.5 + (Math.random() - 0.5) * power * 0.5,
          power * (0.5 + Math.random() * 0.5),
          dir.z * power * 0.5 + (Math.random() - 0.5) * power * 0.5,
        ),
        spin: new THREE.Vector3((Math.random() - 0.5) * 9, (Math.random() - 0.5) * 9, (Math.random() - 0.5) * 9),
        life: 0,
      });
    }

    if (this.particles) {
      this.particles.spawn(def.gore, origin.clone().setY(1.2), { scale: 1.2 });
      this.particles.spawn('blood_splatter', origin.clone().setY(1.4), { scale: 0.9 });
    }
    return { parts: this.parts.length, gore: def.gore, label: def.label };
  }

  update(dt) {
    if (!this.active) return;
    for (const p of this.parts) {
      p.life += dt;
      p.vel.y -= 22 * dt;
      p.mesh.position.addScaledVector(p.vel, dt);
      p.mesh.rotation.x += p.spin.x * dt;
      p.mesh.rotation.y += p.spin.y * dt;
      p.mesh.rotation.z += p.spin.z * dt;

      const floor = 0.18;
      if (p.mesh.position.y < floor) {
        p.mesh.position.y = floor;
        p.vel.y *= -0.32;               // Boden-Dämpfung
        p.vel.x *= 0.72; p.vel.z *= 0.72;
        p.spin.multiplyScalar(0.6);
        if (Math.abs(p.vel.y) < 0.6 && this.particles && p.life < 3) {
          this.particles.spawn('blood_splatter', p.mesh.position.clone(), { scale: 0.25 });
        }
      }
      // Arena-Grenze
      const d = Math.hypot(p.mesh.position.x, p.mesh.position.z);
      if (d > 10.5) {
        p.mesh.position.multiplyScalar(10.5 / d);
        p.vel.x *= -0.4; p.vel.z *= -0.4;
      }
    }
  }

  /** Räumt Teile ab und macht das Opfer wieder sichtbar. */
  clear() {
    for (const p of this.parts) {
      this.scene.remove(p.mesh);
      p.mesh.geometry.dispose();
    }
    if (this.parts[0]) this.parts[0].mesh.material.dispose();
    this.parts = [];
    for (const t of this.hiddenTargets) t.visible = true;
    this.hiddenTargets = [];
    this.active = false;
  }
}
