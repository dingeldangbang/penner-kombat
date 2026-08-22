/**
 * arena3d.js — 3D Arena "Zum Blauen Eimer" im Stil von ArenaVisuals.cs
 * Baut Boden, Wände, Lichter, Neon-Schild, Props als Platzhalter.
 */

import * as THREE from 'three';

export class Arena3D {
  constructor(scene){
    this.scene = scene;
    this.props = [];
    this.hazards = [];
    this.group = new THREE.Group();
    scene.add(this.group);
    this.build();
  }

  build(){
    // Boden — nasses Pflaster, Nachtblau
    const floorGeo = new THREE.CircleGeometry(12, 32);
    const floorMat = new THREE.MeshStandardMaterial({
      color: 0x14161f,
      roughness: 0.85,
      metalness: 0.15
    });
    const floor = new THREE.Mesh(floorGeo, floorMat);
    floor.rotation.x = -Math.PI/2;
    floor.receiveShadow = true;
    this.group.add(floor);

    // Pflasterlinien
    const lineMat = new THREE.LineBasicMaterial({color: 0xffffff, transparent:true, opacity:0.06});
    for(let i=-12;i<=12;i+=2){
      const pts = [new THREE.Vector3(i,0.01,-12), new THREE.Vector3(i,0.01,12)];
      const geo = new THREE.BufferGeometry().setFromPoints(pts);
      this.group.add(new THREE.Line(geo, lineMat));
      const pts2 = [new THREE.Vector3(-12,0.01,i), new THREE.Vector3(12,0.01,i)];
      const geo2 = new THREE.BufferGeometry().setFromPoints(pts2);
      this.group.add(new THREE.Line(geo2, lineMat));
    }

    // Wände (unsichtbare Begrenzung, aber visuell als Backstein)
    const wallMat = new THREE.MeshStandardMaterial({color: 0x1e2233, roughness: 0.9});
    const wallGeo = new THREE.BoxGeometry(24, 4, 0.5);
    const walls = [
      {pos:[0,2,12], rot:[0,0,0]},
      {pos:[0,2,-12], rot:[0,0,0]},
      {pos:[12,2,0], rot:[0,Math.PI/2,0]},
      {pos:[-12,2,0], rot:[0,Math.PI/2,0]},
    ];
    walls.forEach(w=>{
      const m = new THREE.Mesh(wallGeo, wallMat);
      m.position.set(...w.pos);
      m.rotation.y = w.rot[1];
      this.group.add(m);
    });

    // Lichter wie ArenaVisuals.cs
    // Key Laterne warm orange
    const keyLight = new THREE.SpotLight(0xFF6B00, 800, 22, Math.PI/4, 0.3, 1);
    keyLight.position.set(-3.5, 6.5, -1);
    keyLight.target.position.set(0,0,0);
    keyLight.castShadow = true;
    this.group.add(keyLight);
    this.group.add(keyLight.target);
    // Visual für Laterne
    const lanternMesh = new THREE.Mesh(
      new THREE.SphereGeometry(0.25,12,8),
      new THREE.MeshStandardMaterial({color: 0xFF6B00, emissive: 0xFF6B00, emissiveIntensity: 2})
    );
    lanternMesh.position.copy(keyLight.position);
    this.group.add(lanternMesh);

    const fillLight = new THREE.SpotLight(0xFFD700, 400, 18, Math.PI/3, 0.4, 1);
    fillLight.position.set(4.5,5.5,-2);
    fillLight.target.position.set(0,0,0);
    this.group.add(fillLight);
    this.group.add(fillLight.target);

    const moonLight = new THREE.DirectionalLight(0x88CCFF, 0.6);
    moonLight.position.set(0,12,8);
    moonLight.castShadow = true;
    this.group.add(moonLight);

    const neonLight = new THREE.PointLight(0x00BFFF, 50, 12);
    neonLight.position.set(0,4.2,5.5);
    this.group.add(neonLight);
    this.neonLight = neonLight;

    // Ambient
    this.scene.background = new THREE.Color(0x0b0d16);
    this.scene.fog = new THREE.FogExp2(0x1A1C2A, 0.018);

    // Neon Schild Text
    const canvas = document.createElement('canvas');
    canvas.width = 512; canvas.height = 128;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#000000';
    ctx.fillRect(0,0,512,128);
    ctx.fillStyle = '#00BFFF';
    ctx.font = 'bold 48px system-ui';
    ctx.textAlign = 'center';
    ctx.fillText('ZUM BLAUEN EIMER',256,80);
    const tex = new THREE.CanvasTexture(canvas);
    const neonMat = new THREE.MeshBasicMaterial({map: tex, transparent:true});
    const neonPlane = new THREE.Mesh(new THREE.PlaneGeometry(6,1.5), neonMat);
    neonPlane.position.set(0,4.2,5.6);
    this.group.add(neonPlane);
    this.neonPlane = neonPlane;

    // Props wie ArenaVisuals.BuildYard()
    this.buildProps();

    // Partikel: Mücken, Staub
    this.buildParticles();
  }

  buildProps(){
    const matEarth = new THREE.MeshStandardMaterial({color: 0x8B4513});
    const matRed = new THREE.MeshStandardMaterial({color: 0x8B0000});
    const matGreen = new THREE.MeshStandardMaterial({color: 0x228B22});
    const matWhite = new THREE.MeshStandardMaterial({color: 0xffffff});
    const matBlue = new THREE.MeshStandardMaterial({color: 0x00BFFF});
    const matGray = new THREE.MeshStandardMaterial({color: 0x888888});

    const defs = [
      {name:'Bierkasten-Turm', kind:'BeerCrateTower', pos:[-6.5,0.9,2.5], scale:[1.2,1.8,0.9], mat: matEarth, dmg:14},
      {name:'Gasflasche', kind:'GasBottle', pos:[6.2,0.8,2.2], scale:[0.5,1.6,0.5], mat: matRed, hits:4, expDmg:25, expRad:5},
      {name:'Mülltonne', kind:'TrashCan', pos:[4.5,0.6,-2.5], scale:[0.8,1.2,0.8], mat: matGreen, dmg:7},
      {name:'Wäscheleine', kind:'LaundryLine', pos:[0,3.6,4.5], scale:[9,0.05,0.05], mat: matWhite, stun:0.3},
      {name:'Paula-Napf', kind:'PaulaBowl', pos:[-3.5,0.12,-3.2], scale:[0.5,0.2,0.5], mat: matBlue},
      {name:'Baugerüst', kind:'Scaffold', pos:[-7.5,1.0,-1.5], scale:[0.2,2.0,3.0], mat: matGray},
    ];

    defs.forEach(d=>{
      const geo = new THREE.BoxGeometry(...d.scale);
      const mesh = new THREE.Mesh(geo, d.mat);
      mesh.position.set(...d.pos);
      mesh.castShadow = true;
      mesh.receiveShadow = true;
      if(d.kind==='BeerCrateTower'){
        mesh.rotation.z = (Math.random()-0.5)*0.2;
      }
      mesh.userData = {kind: d.kind, hits:0, hitsToBreak: d.hits||1, tilted:false, destroyed:false, startPos: new THREE.Vector3(...d.pos), startRot: mesh.rotation.clone(), dmg: d.dmg||0, expDmg: d.expDmg||0, expRad: d.expRad||0, stun: d.stun||0};
      this.group.add(mesh);
      this.props.push(mesh);
    });
  }

  buildParticles(){
    // Einfache Partikel als Points
    const count = 200;
    const geo = new THREE.BufferGeometry();
    const positions = new Float32Array(count*3);
    for(let i=0;i<count;i++){
      positions[i*3] = (Math.random()-0.5)*20;
      positions[i*3+1] = Math.random()*6;
      positions[i*3+2] = (Math.random()-0.5)*20;
    }
    geo.setAttribute('position', new THREE.BufferAttribute(positions,3));
    const mat = new THREE.PointsMaterial({color: 0xFFD700, size:0.04, transparent:true, opacity:0.4});
    const points = new THREE.Points(geo, mat);
    this.group.add(points);
    this.particles = points;
  }

  tiltAll(){
    this.props.forEach(p=>{
      if(p.userData.destroyed) return;
      p.userData.tilted = true;
      // Kippen
      p.rotation.x += (Math.random()-0.5)*1.2;
      p.rotation.z += (Math.random()-0.5)*1.2;
      p.position.y -= 0.3;
    });
  }

  panicNeon(seconds=2){
    if(!this.neonLight) return;
    let t=0;
    const interval = setInterval(()=>{
      t+=0.05;
      this.neonLight.intensity = Math.random()<0.5 ? 5 : 80;
      this.neonPlane.material.opacity = Math.random()<0.5 ? 0.2 : 1;
      if(t>seconds){
        clearInterval(interval);
        this.neonLight.intensity = 50;
        this.neonPlane.material.opacity = 1;
      }
    },50);
  }

  reset(){
    this.props.forEach(p=>{
      if(p.userData.startPos){
        p.position.copy(p.userData.startPos);
        p.rotation.set(p.userData.startRot.x, p.userData.startRot.y, p.userData.startRot.z);
        p.userData.tilted = false;
        p.userData.destroyed = false;
        p.userData.hits = 0;
        p.visible = true;
      }
    });
    // Hazards entfernen
    this.hazards.forEach(h=>this.group.remove(h.mesh));
    this.hazards = [];
  }

  update(dt, time){
    // Neon flackern
    if(this.neonLight){
      this.neonLight.intensity = 45 + Math.sin(time*6)*8 + Math.sin(time*13)*4;
      if(Math.random()<0.01){
        this.neonLight.intensity = 2;
        setTimeout(()=>{ if(this.neonLight) this.neonLight.intensity = 50; }, 60);
      }
    }
    // Partikel leicht bewegen
    if(this.particles){
      this.particles.rotation.y += dt*0.05;
    }
  }

  spawnHazard(pos, kind){
    const color = kind==='Glass' ? 0x228B22 : 0xFF6B00;
    const mesh = new THREE.Mesh(
      new THREE.CircleGeometry(kind==='Glass'?1.4:2.0,16),
      new THREE.MeshBasicMaterial({color: color, transparent:true, opacity:0.35})
    );
    mesh.rotation.x = -Math.PI/2;
    mesh.position.set(pos.x,0.05,pos.z);
    this.group.add(mesh);
    this.hazards.push({mesh, t: kind==='Glass'?12:6, kind, pos, dmg: kind==='Glass'?2:4, radius: kind==='Glass'?1.4:2.0});
  }

  tickHazards(dt, fighters){
    for(let i=this.hazards.length-1;i>=0;i--){
      const h = this.hazards[i];
      h.t -= dt;
      if(h.t<=0){
        this.group.remove(h.mesh);
        this.hazards.splice(i,1);
        continue;
      }
      fighters.forEach(f=>{
        const dx = f.x - h.pos.x;
        const dz = f.z - h.pos.z;
        if(Math.hypot(dx,dz) < h.radius){
          // Schaden über Zeit wird in sim gehandelt, hier nur Visual
        }
      });
    }
  }
}
