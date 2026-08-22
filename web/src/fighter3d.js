/**
 * fighter3d.js — Three.js Fighter Mesh mit Pose-System
 * 
 * Baut einen Kämpfer aus primitiven Meshes (wie FighterFactory.CreatePlaceholder)
 * und wendet Posen aus pose.js an. Keine externen Modelle nötig.
 * Jeder Charakter hat eigene Proportionen via STATURE + eigene Farben.
 */

import * as THREE from 'three';
import { POSES, JOINTS } from './pose.js';

export class FighterMesh3D {
  constructor(config, stature){
    this.cfg = config;
    this.stature = stature;
    this.group = new THREE.Group();
    this.joints = {};
    this.meshes = {};
    this.build();
  }

  build(){
    const color = new THREE.Color(this.cfg.color || '#ffffff');
    const accent = color.clone().lerp(new THREE.Color('#FFD700'), 0.3);
    const skin = color.clone().lerp(new THREE.Color('#ffffff'), 0.2);

    // Root hips
    const hips = new THREE.Group();
    this.group.add(hips);
    this.joints.hips = hips;

    // Spine
    const spine = new THREE.Group();
    spine.position.y = 0.15;
    hips.add(spine);
    this.joints.spine = spine;
    const spineMesh = new THREE.Mesh(
      new THREE.CapsuleGeometry(0.18*this.stature.radius/0.4, 0.35*this.stature.height/1.8, 4, 8),
      new THREE.MeshStandardMaterial({color: color, roughness: 0.7})
    );
    spineMesh.position.y = 0.2;
    spine.add(spineMesh);

    // Chest
    const chest = new THREE.Group();
    chest.position.y = 0.45;
    spine.add(chest);
    this.joints.chest = chest;
    const chestMesh = new THREE.Mesh(
      new THREE.BoxGeometry(0.5*this.stature.radius/0.4 + 0.3, 0.4, 0.25),
      new THREE.MeshStandardMaterial({color: color, roughness: 0.6})
    );
    chest.add(chestMesh);
    this.meshes.torso = chestMesh;

    // Head
    const headGroup = new THREE.Group();
    headGroup.position.y = 0.35;
    chest.add(headGroup);
    this.joints.head = headGroup;
    const headMesh = new THREE.Mesh(
      new THREE.SphereGeometry(0.22, 16, 12),
      new THREE.MeshStandardMaterial({color: skin, roughness: 0.5})
    );
    headGroup.add(headMesh);
    // Nase = Blickrichtung
    const nose = new THREE.Mesh(
      new THREE.BoxGeometry(0.06,0.06,0.12),
      new THREE.MeshStandardMaterial({color: 0xFFD700})
    );
    nose.position.set(0,0,0.22);
    headGroup.add(nose);
    // Augen
    const eyeMat = new THREE.MeshStandardMaterial({color: 0x111111});
    const leftEye = new THREE.Mesh(new THREE.SphereGeometry(0.03,6,6), eyeMat);
    leftEye.position.set(-0.08,0.05,0.18);
    headGroup.add(leftEye);
    const rightEye = new THREE.Mesh(new THREE.SphereGeometry(0.03,6,6), eyeMat);
    rightEye.position.set(0.08,0.05,0.18);
    headGroup.add(rightEye);

    // Arms
    const armGeoUpper = new THREE.CapsuleGeometry(0.07, 0.3, 4, 8);
    const armGeoLower = new THREE.CapsuleGeometry(0.06, 0.28, 4, 8);
    const armMat = new THREE.MeshStandardMaterial({color: accent, roughness: 0.6});

    // Left shoulder
    const lShoulder = new THREE.Group();
    lShoulder.position.set(-0.32, 0.15, 0);
    chest.add(lShoulder);
    this.joints.lShoulder = lShoulder;
    const lUpper = new THREE.Mesh(armGeoUpper, armMat);
    lUpper.position.y = -0.18;
    lShoulder.add(lUpper);
    const lElbow = new THREE.Group();
    lElbow.position.y = -0.38;
    lShoulder.add(lElbow);
    this.joints.lElbow = lElbow;
    const lLower = new THREE.Mesh(armGeoLower, armMat);
    lLower.position.y = -0.16;
    lElbow.add(lLower);
    // Hand
    const handGeo = new THREE.SphereGeometry(0.07,8,8);
    const lHand = new THREE.Mesh(handGeo, new THREE.MeshStandardMaterial({color: skin}));
    lHand.position.y = -0.32;
    lElbow.add(lHand);
    this.meshes.lHand = lHand;

    // Right shoulder
    const rShoulder = new THREE.Group();
    rShoulder.position.set(0.32, 0.15, 0);
    chest.add(rShoulder);
    this.joints.rShoulder = rShoulder;
    const rUpper = new THREE.Mesh(armGeoUpper, armMat);
    rUpper.position.y = -0.18;
    rShoulder.add(rUpper);
    const rElbow = new THREE.Group();
    rElbow.position.y = -0.38;
    rShoulder.add(rElbow);
    this.joints.rElbow = rElbow;
    const rLower = new THREE.Mesh(armGeoLower, armMat);
    rLower.position.y = -0.16;
    rElbow.add(rLower);
    const rHand = new THREE.Mesh(handGeo, new THREE.MeshStandardMaterial({color: skin}));
    rHand.position.y = -0.32;
    rElbow.add(rHand);
    this.meshes.rHand = rHand;

    // Legs
    const legUpperGeo = new THREE.CapsuleGeometry(0.11, 0.4, 4, 8);
    const legLowerGeo = new THREE.CapsuleGeometry(0.09, 0.38, 4, 8);
    const legMat = new THREE.MeshStandardMaterial({color: color.clone().multiplyScalar(0.8), roughness: 0.7});

    const lHip = new THREE.Group();
    lHip.position.set(-0.15, -0.05, 0);
    hips.add(lHip);
    this.joints.lHip = lHip;
    const lThigh = new THREE.Mesh(legUpperGeo, legMat);
    lThigh.position.y = -0.25;
    lHip.add(lThigh);
    const lKnee = new THREE.Group();
    lKnee.position.y = -0.52;
    lHip.add(lKnee);
    this.joints.lKnee = lKnee;
    const lShin = new THREE.Mesh(legLowerGeo, legMat);
    lShin.position.y = -0.22;
    lKnee.add(lShin);
    const lAnkle = new THREE.Group();
    lAnkle.position.y = -0.45;
    lKnee.add(lAnkle);
    this.joints.lAnkle = lAnkle;
    const lFoot = new THREE.Mesh(new THREE.BoxGeometry(0.14,0.08,0.22), new THREE.MeshStandardMaterial({color: 0x222222}));
    lFoot.position.set(0, -0.04, 0.05);
    lAnkle.add(lFoot);

    const rHip = new THREE.Group();
    rHip.position.set(0.15, -0.05, 0);
    hips.add(rHip);
    this.joints.rHip = rHip;
    const rThigh = new THREE.Mesh(legUpperGeo, legMat);
    rThigh.position.y = -0.25;
    rHip.add(rThigh);
    const rKnee = new THREE.Group();
    rKnee.position.y = -0.52;
    rHip.add(rKnee);
    this.joints.rKnee = rKnee;
    const rShin = new THREE.Mesh(legLowerGeo, legMat);
    rShin.position.y = -0.22;
    rKnee.add(rShin);
    const rAnkle = new THREE.Group();
    rAnkle.position.y = -0.45;
    rKnee.add(rAnkle);
    this.joints.rAnkle = rAnkle;
    const rFoot = new THREE.Mesh(new THREE.BoxGeometry(0.14,0.08,0.22), new THREE.MeshStandardMaterial({color: 0x222222}));
    rFoot.position.set(0, -0.04, 0.05);
    rAnkle.add(rFoot);

    // Mojo Bob tail (Riesenschwanz)
    if(this.cfg.id === 'mojo_bob' || this.cfg.id === 'mojo_bob'){
      const tailGroup = new THREE.Group();
      tailGroup.position.set(0, -0.1, -0.15);
      hips.add(tailGroup);
      this.joints.tail = tailGroup;
      const tailMat = new THREE.MeshStandardMaterial({color: 0x8B4513, roughness: 0.8});
      let prev = tailGroup;
      this.tailSegments = [];
      for(let i=0;i<6;i++){
        const seg = new THREE.Group();
        seg.position.z = -0.18;
        const mesh = new THREE.Mesh(new THREE.CapsuleGeometry(0.06 - i*0.008, 0.18, 4, 6), tailMat);
        mesh.position.z = -0.09;
        mesh.rotation.x = Math.PI/2;
        seg.add(mesh);
        prev.add(seg);
        prev = seg;
        this.tailSegments.push(seg);
      }
    } else {
      const tailGroup = new THREE.Group();
      hips.add(tailGroup);
      this.joints.tail = tailGroup;
    }

    // Weapon placeholder (Flasche, Löffel, etc)
    const weaponGroup = new THREE.Group();
    rHand.add(weaponGroup);
    this.weaponGroup = weaponGroup;

    // Scale by stature
    const s = this.stature.height / 1.8;
    this.group.scale.set(s,s,s);

    // Schatten
    this.shadow = new THREE.Mesh(
      new THREE.CircleGeometry(this.stature.radius*0.9, 16),
      new THREE.MeshBasicMaterial({color: 0x000000, transparent: true, opacity: 0.35})
    );
    this.shadow.rotation.x = -Math.PI/2;
    this.shadow.position.y = 0.02;

    // Flash material for hit
    this.baseMaterials = [];
    this.group.traverse(o=>{
      if(o.isMesh && o.material){
        this.baseMaterials.push({mesh:o, color:o.material.color.clone(), emissive: o.material.emissive ? o.material.emissive.clone() : null});
      }
    });
  }

  applyPose(pose, blend=1){
    for(const name of JOINTS){
      const joint = this.joints[name];
      if(!joint) continue;
      const rot = pose[name];
      if(!rot) continue;
      if(blend >= 0.99){
        joint.rotation.set(rot.x, rot.y, rot.z);
      } else {
        joint.rotation.x += (rot.x - joint.rotation.x)*blend;
        joint.rotation.y += (rot.y - joint.rotation.y)*blend;
        joint.rotation.z += (rot.z - joint.rotation.z)*blend;
      }
    }
  }

  update(fighter, time, pose){
    // Position
    this.group.position.set(fighter.x, fighter.y + this.stature.height*0.5, fighter.z);
    // Facing: look towards facing direction (1 = +X, -1 = -X)
    const targetYaw = fighter.facing > 0 ? 0 : Math.PI;
    // Smooth rotation
    let currentYaw = this.group.rotation.y;
    let diff = targetYaw - currentYaw;
    while(diff > Math.PI) diff -= Math.PI*2;
    while(diff < -Math.PI) diff += Math.PI*2;
    this.group.rotation.y += diff * 0.25;

    // Apply pose
    this.applyPose(pose, 0.35);

    // Tail wag for Mojo Bob
    if(this.tailSegments){
      const t = time*3;
      this.tailSegments.forEach((seg,i)=>{
        seg.rotation.x = Math.sin(t + i*0.5)*0.3;
        seg.rotation.y = Math.cos(t*0.7 + i)*0.2;
      });
      if(fighter.attacking && fighter.attacking.special && fighter.attacking.special.key.includes('schwanz')){
        this.tailSegments.forEach((seg,i)=>{
          seg.rotation.x += Math.sin(time*15 + i)*0.8;
          seg.rotation.y += Math.cos(time*12 + i)*0.6;
        });
      }
    }

    // Hit flash
    if(fighter.flash > 0){
      const f = fighter.flash / 0.12;
      this.group.traverse(o=>{
        if(o.isMesh && o.material && o.material.emissive){
          o.material.emissive.setRGB(f,f,f);
        }
        if(o.isMesh && o.material && o.material.color){
          if(f>0.5) o.material.color.set(0xffffff);
        }
      });
    } else {
      // restore
      this.baseMaterials.forEach(entry=>{
        if(entry.mesh.material){
          entry.mesh.material.color.copy(entry.color);
          if(entry.mesh.material.emissive && entry.emissive){
            entry.mesh.material.emissive.copy(entry.emissive);
          } else if(entry.mesh.material.emissive){
            entry.mesh.material.emissive.set(0,0,0);
          }
        }
      });
    }

    // Weapon visibility based on special
    this.weaponGroup.clear();
    if(fighter.attacking && fighter.attacking.special){
      const key = fighter.attacking.special.key;
      if(key === 'flaschenhals' || key === 'flaschenhals_ex'){
        const bottle = new THREE.Mesh(
          new THREE.CylinderGeometry(0.05,0.05,0.35,8),
          new THREE.MeshStandardMaterial({color: 0x228B22, transparent:true, opacity:0.9})
        );
        bottle.rotation.z = Math.PI/2;
        bottle.position.set(0,0,0.15);
        this.weaponGroup.add(bottle);
      } else if(key === 'pfanne'){
        const pan = new THREE.Mesh(
          new THREE.CylinderGeometry(0.18,0.18,0.02,16),
          new THREE.MeshStandardMaterial({color: 0x333333, metalness:0.8, roughness:0.2})
        );
        pan.position.set(0,0,0.12);
        this.weaponGroup.add(pan);
      } else if(key === 'loeffelsturm' || key === 'fuenfzig'){
        const spoon = new THREE.Mesh(
          new THREE.CapsuleGeometry(0.02,0.2,4,6),
          new THREE.MeshStandardMaterial({color: 0xCCCCCC, metalness:0.9})
        );
        spoon.position.set(0,0,0.1);
        this.weaponGroup.add(spoon);
      }
    }

    // Med indicator
    if(fighter.medTimer > 0){
      if(!this.medMesh){
        this.medMesh = new THREE.Mesh(
          new THREE.SphereGeometry(0.08,8,8),
          new THREE.MeshBasicMaterial({color: 0x00BFFF, transparent:true, opacity:0.8})
        );
        this.joints.head.add(this.medMesh);
        this.medMesh.position.set(0,0.5,0);
      }
      this.medMesh.visible = true;
      this.medMesh.scale.setScalar(1 + Math.sin(time*10)*0.2);
    } else {
      if(this.medMesh) this.medMesh.visible = false;
    }

    // Block shield
    if(fighter.blocking){
      if(!this.blockMesh){
        this.blockMesh = new THREE.Mesh(
          new THREE.SphereGeometry(0.55,16,16,0,Math.PI*2,0,Math.PI/2),
          new THREE.MeshBasicMaterial({color: 0x00BFFF, transparent:true, opacity:0.25, side:THREE.DoubleSide})
        );
        this.blockMesh.rotation.y = Math.PI;
        this.group.add(this.blockMesh);
        this.blockMesh.position.set(0,0.9,0);
      }
      this.blockMesh.visible = true;
      this.blockMesh.position.x = fighter.facing*0.4;
    } else {
      if(this.blockMesh) this.blockMesh.visible = false;
    }
  }
}
