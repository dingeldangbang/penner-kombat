/**
 * placeholder.js — Prozeduraler Ersatzkämpfer
 *
 * Solange kein .glb hochgeladen wurde, steht ein einfacher Penner-Rig aus
 * Primitiven in der Arena, damit die KI-Pipeline sofort testbar ist.
 */

import * as THREE from 'three';

export function buildPlaceholderFighter(color = 0xff6b00) {
  const g = new THREE.Group();
  const mat = (c, rough = 0.75) => new THREE.MeshStandardMaterial({ color: c, roughness: rough, metalness: 0.05 });
  const body = mat(color);
  const skin = mat(0xd7a07a);
  const dark = mat(0x2b2b33);

  const add = (geo, m, x, y, z) => {
    const mesh = new THREE.Mesh(geo, m);
    mesh.position.set(x, y, z);
    mesh.castShadow = true;
    g.add(mesh);
    return mesh;
  };

  add(new THREE.CapsuleGeometry(0.28, 0.55, 4, 10), body, 0, 1.15, 0);       // Rumpf
  add(new THREE.SphereGeometry(0.22, 12, 10), skin, 0, 1.72, 0);              // Kopf
  add(new THREE.CylinderGeometry(0.24, 0.26, 0.12, 10), dark, 0, 1.9, 0);     // Mütze
  const armL = add(new THREE.CapsuleGeometry(0.09, 0.45, 4, 8), skin, -0.4, 1.2, 0);
  const armR = add(new THREE.CapsuleGeometry(0.09, 0.45, 4, 8), skin, 0.4, 1.2, 0);
  armL.rotation.z = 0.25; armR.rotation.z = -0.25;
  add(new THREE.CapsuleGeometry(0.12, 0.5, 4, 8), dark, -0.16, 0.45, 0);
  add(new THREE.CapsuleGeometry(0.12, 0.5, 4, 8), dark, 0.16, 0.45, 0);

  g.userData.isPlaceholder = true;
  g.userData.animate = (t) => {
    g.position.y = Math.sin(t * 2.2) * 0.03;
    armL.rotation.x = Math.sin(t * 2.2) * 0.15;
    armR.rotation.x = -Math.sin(t * 2.2) * 0.15;
  };
  return g;
}

export function buildDummy() {
  const g = new THREE.Group();
  const mat = new THREE.MeshStandardMaterial({ color: 0x9aa0b5, roughness: 0.9 });
  const body = new THREE.Mesh(new THREE.CapsuleGeometry(0.35, 0.9, 4, 12), mat);
  body.position.y = 1.05; body.castShadow = true;
  const head = new THREE.Mesh(new THREE.SphereGeometry(0.24, 12, 10), mat);
  head.position.y = 1.85; head.castShadow = true;
  const base = new THREE.Mesh(new THREE.CylinderGeometry(0.45, 0.55, 0.2, 14),
    new THREE.MeshStandardMaterial({ color: 0x40424f, roughness: 1 }));
  base.position.y = 0.1;
  g.add(body, head, base);
  return g;
}
