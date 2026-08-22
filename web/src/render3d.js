/**
 * render3d.js — Three.js Renderer für Penner Kombat 3D
 * 
 * - Baut Szene, Kamera, Lichter, Arena, FighterMeshes
 * - Wendet Pose-System an (pose.js)
 * - Zeichnet Effekte: Hit Sparks, Projektile, Floating Text, Screen Shake, Hitstop
 * - Kamera: dynamisch, folgt beiden Kämpfern, leicht von oben, wie CameraController.cs
 */

import * as THREE from 'three';
import { FighterMesh3D } from './fighter3d.js';
import { Arena3D } from './arena3d.js';
import { selectPose, lerpPose, POSES } from './pose.js';
import { PALETTE } from './data.js';

export class Renderer3D {
  constructor(canvas){
    this.canvas = canvas;
    this.scene = new THREE.Scene();
    this.renderer = new THREE.WebGLRenderer({canvas, antialias:true, alpha:false});
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio||1,2));
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;

    this.camera = new THREE.PerspectiveCamera(58, 1, 0.1, 100);
    this.camera.position.set(0,5,9);

    this.arena = new Arena3D(this.scene);

    this.fighters = [];
    this.projectileMeshes = [];
    this.effects = []; // {mesh, t, type}
    this.floaters = []; // {el, x,y,z, t}

    this.shakeTime = 0;
    this.shakeStrength = 0;
    this.hitStop = 0;

    this.raycaster = new THREE.Raycaster();

    this.resize();
    window.addEventListener('resize', ()=>this.resize());

    // Fog und Hintergrund schon in Arena gesetzt
  }

  resize(){
    const w = this.canvas.clientWidth;
    const h = this.canvas.clientHeight;
    this.renderer.setSize(w,h,false);
    this.camera.aspect = w/h;
    this.camera.updateProjectionMatrix();
  }

  initFighters(p1Cfg, p2Cfg, stature1, stature2){
    // Alte entfernen
    this.fighters.forEach(f=>{
      this.scene.remove(f.mesh.group);
      this.scene.remove(f.mesh.shadow);
    });
    this.fighters = [];

    const m1 = new FighterMesh3D(p1Cfg, stature1);
    const m2 = new FighterMesh3D(p2Cfg, stature2);
    this.scene.add(m1.group);
    this.scene.add(m1.shadow);
    this.scene.add(m2.group);
    this.scene.add(m2.shadow);
    this.fighters = [
      {mesh:m1, cfg:p1Cfg, lastPose: POSES.idle},
      {mesh:m2, cfg:p2Cfg, lastPose: POSES.idle}
    ];
  }

  shake(strength,time){
    this.shakeStrength = Math.max(this.shakeStrength, strength);
    this.shakeTime = Math.max(this.shakeTime, time);
  }

  freeze(seconds){ this.hitStop = Math.max(this.hitStop, seconds); }

  burst(world, color, count=12){
    for(let i=0;i<count;i++){
      const geo = new THREE.SphereGeometry(0.04 + Math.random()*0.06, 6,6);
      const mat = new THREE.MeshBasicMaterial({color: new THREE.Color(color), transparent:true, opacity:1});
      const mesh = new THREE.Mesh(geo, mat);
      mesh.position.set(world.x, world.y+1, world.z);
      mesh.userData = {
        vx: (Math.random()-0.5)*6,
        vy: 1 + Math.random()*5,
        vz: (Math.random()-0.5)*6,
        t: 0.5 + Math.random()*0.4
      };
      this.scene.add(mesh);
      this.effects.push({mesh, t: mesh.userData.t, type:'spark'});
    }
  }

  floatText(text, world, color){
    // DOM floating text über Canvas
    const div = document.createElement('div');
    div.textContent = text;
    div.style.position='absolute';
    div.style.color=color;
    div.style.fontWeight='800';
    div.style.fontSize='20px';
    div.style.textShadow='0 2px 4px rgba(0,0,0,0.9)';
    div.style.pointerEvents='none';
    div.style.transform='translate(-50%,-50%)';
    document.body.appendChild(div);
    this.floaters.push({el:div, x:world.x, y:world.y+2.2, z:world.z, t:0.9});
  }

  spawnProjectile(x,z, effect){
    let color = 0xFF6B00;
    if(effect==='bleed' || effect==='bleed_heavy') color = 0x8B0000;
    if(effect==='crit' || effect==='coin') color = 0xFFD700;
    if(effect==='burn') color = 0xFF4500;
    const mesh = new THREE.Mesh(
      new THREE.SphereGeometry(effect==='coin'?0.12:0.08,8,8),
      new THREE.MeshStandardMaterial({color, emissive: color, emissiveIntensity:0.6})
    );
    mesh.position.set(x, 0.9, z);
    this.scene.add(mesh);
    this.projectileMeshes.push({mesh, t:2.0, effect});
  }

  draw(match, dt, time){
    if(this.hitStop>0){
      this.hitStop-=dt;
      // Freeze: nicht updaten, nur rendern
      this.renderer.render(this.scene, this.camera);
      return;
    }

    // Shake
    if(this.shakeTime>0){
      this.shakeTime-=dt;
      const s = this.shakeStrength * (this.shakeTime/0.2);
      this.camera.position.x += (Math.random()-0.5)*s*0.1;
      this.camera.position.y += (Math.random()-0.5)*s*0.1;
    }

    // Arena update
    this.arena.update(dt, time);
    this.arena.tickHazards(dt, [match.p1, match.p2]);

    // Kamera: wie CameraController.Dynamic — Mitte + Distanz nach Spread
    const midX = (match.p1.x + match.p2.x)/2;
    const midZ = (match.p1.z + match.p2.z)/2;
    const spread = Math.hypot(match.p1.x-match.p2.x, match.p1.z-match.p2.z);
    const targetCam = {
      x: midX*0.6,
      y: 4.2 + spread*0.15,
      z: 9 + spread*0.55,
      lookX: midX,
      lookY: 0.9,
      lookZ: midZ*0.4
    };
    this.camera.position.x += (targetCam.x - this.camera.position.x)*0.08;
    this.camera.position.y += (targetCam.y - this.camera.position.y)*0.08;
    this.camera.position.z += (targetCam.z - this.camera.position.z)*0.08;
    this.camera.lookAt(targetCam.lookX, targetCam.lookY, targetCam.lookZ);

    // Fighter Meshes
    const fighters = [match.p1, match.p2];
    fighters.forEach((f,i)=>{
      const fm = this.fighters[i];
      if(!fm) return;
      const pose = selectPose(f, time);
      // Lerp von letzter Pose
      const blended = lerpPose(fm.lastPose, pose, 0.35);
      fm.lastPose = blended;
      fm.mesh.update(f, time, blended);
      // Schatten folgt
      fm.mesh.shadow.position.set(f.x, 0.02, f.z);
    });

    // Projektile rendern
    // Sync mit sim projectiles
    this.projectileMeshes.forEach(pm=>pm.t-=dt);
    this.projectileMeshes = this.projectileMeshes.filter(pm=>{
      if(pm.t<=0){ this.scene.remove(pm.mesh); return false; }
      return true;
    });
    // Neue Projektile aus Sim hinzufügen? Wird über events gemacht

    // Effekte
    for(let i=this.effects.length-1;i>=0;i--){
      const e = this.effects[i];
      e.t-=dt;
      if(e.t<=0){ this.scene.remove(e.mesh); this.effects.splice(i,1); continue; }
      if(e.type==='spark'){
        const d = e.mesh.userData;
        d.vy -= 12*dt;
        e.mesh.position.x += d.vx*dt;
        e.mesh.position.y += d.vy*dt;
        e.mesh.position.z += d.vz*dt;
        e.mesh.material.opacity = Math.max(0, e.t*2);
      }
    }

    // Floater DOM
    for(let i=this.floaters.length-1;i>=0;i--){
      const f = this.floaters[i];
      f.t-=dt;
      f.y += dt*1.4;
      if(f.t<=0){ f.el.remove(); this.floaters.splice(i,1); continue; }
      // Projizieren
      const pos = new THREE.Vector3(f.x, f.y, f.z);
      pos.project(this.camera);
      const x = (pos.x*0.5+0.5)*window.innerWidth;
      const y = (1-(pos.y*0.5+0.5))*window.innerHeight;
      f.el.style.left = x+'px';
      f.el.style.top = y+'px';
      f.el.style.opacity = Math.min(1, f.t*2);
    }

    this.renderer.render(this.scene, this.camera);
  }

  handleEvent(e, match){
    if(e.type==='hit'){
      const heavy = e.kind!=='light';
      const pos = {x: e.target.x, y: e.target.y+1, z: e.target.z};
      const color = e.crit ? PALETTE.gold : e.blocked ? PALETTE.neonBlue : PALETTE.bloodRed;
      this.burst(pos, color, e.blocked?5:heavy?16:9);
      this.floatText(
        e.crit?`KRIT ${Math.round(e.damage)}`:Math.round(e.damage).toString(),
        pos, e.crit?PALETTE.gold:e.blocked?PALETTE.neonBlue:PALETTE.white
      );
      this.shake(heavy?9:4, 0.12);
      if(e.crit || e.kind==='fatal') this.freeze(0.08);
      if(e.combo>=2){
        this.floatText(`${e.combo} TREFFER`, {x:e.attacker.x, y:2.6, z:e.attacker.z}, e.combo>=10?'#FF2A2A':e.combo>=5?PALETTE.gold:PALETTE.white);
      }
      // Spezialeffekte
      if(e.special){
        if(e.special.key==='grosser_schwung' || e.special.key==='pfanne'){
          this.burst(pos, PALETTE.warmOrange, 18);
          this.shake(12,0.18);
        }
        if(e.special.key==='mops'){
          this.arena.tiltAll();
          this.arena.panicNeon(2.5);
          for(let i=0;i<6;i++){
            const rx = (Math.random()-0.5)*10;
            const rz = (Math.random()-0.5)*10;
            this.burst({x:rx, y:0.2, z:rz}, PALETTE.earth, 10);
          }
        }
        if(e.special.key==='dingeneldang'){
          this.burst({x:e.attacker.x, y:1.5, z:e.attacker.z}, PALETTE.gold, 30);
        }
      }
    }
    if(e.type==='projectile_hit'){
      this.burst({x:e.x, y:0.9, z:e.z}, e.effect==='coin'?PALETTE.gold:PALETTE.bloodRed, 8);
    }
    if(e.type==='trash_yard'){
      this.arena.tiltAll();
      this.arena.panicNeon(2.5);
    }
    if(e.type==='ko'){
      this.shake(16,0.35);
      this.freeze(0.15);
      this.burst({x:e.target.x, y:1.2, z:e.target.z}, PALETTE.bloodRed, 22);
    }
  }
}
