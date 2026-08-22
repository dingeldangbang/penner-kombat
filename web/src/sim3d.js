/**
 * sim3d.js — Erweiterte Kampflogik für Le Binde vs Mojo Bob 3D.
 * 
 * Basiert auf sim.js, aber mit 100% spielbaren Kombos und Effekten:
 * - Alle 6 Specials pro Charakter aus MoveCatalog
 * - Projektile (Flasche, Löffel, Münze)
 * - Buffs/Debuffs (REIF! +80%, Mops-Kommando +50% Krit, Dingeneldang 100% Krit)
 * - Schmier-Schlüppa -60% Schaden, 45% Grab-Rutsch
 * - Mojo System: 15% Basis-Krit x2.4, Mojo-Punkte, Gamble
 * - Arena-Interaktion (Props kippen)
 * - Fatal Blow 28-44, Med-Kapseln
 * - Kombo-System mit Cancel-Fenster
 */

import { K, STATURE, AI_LEVELS, byId } from './data.js';

export const STEP = 1/60;
const clamp = (v,a,b)=>Math.min(b,Math.max(a,v));
const dist = (a,b)=>Math.hypot(a.x-b.x, a.z-b.z);

export function makeRandom(seed=12345){
  let s = seed>>>0;
  return ()=>{ s = (s*1664525+1013904223)>>>0; return s/4294967296; };
}

class Projectile {
  constructor(owner, x,z, vx,vz, damage, range, effect, life=2.0){
    this.owner = owner;
    this.x = x; this.z = z;
    this.vx = vx; this.vz = vz;
    this.damage = damage;
    this.range = range;
    this.effect = effect;
    this.life = life;
    this.hit = false;
  }
  tick(dt){
    this.x += this.vx*dt;
    this.z += this.vz*dt;
    this.life -= dt;
  }
}

export class Fighter {
  constructor(config, index, rng){
    this.cfg = config;
    this.index = index;
    this.rng = rng;
    const st = STATURE[config.stature] || STATURE.normal;
    this.height = st.height;
    this.radius = st.radius;
    this.mass = st.mass;
    this.hitboxScale = st.hitbox;

    this.maxHP = config.maxHP;
    this.moveSpeed = config.moveSpeed;
    this.light = config.light;
    this.heavy = config.heavy;
    this.range = config.range;

    this.reset(index===0?-4:4);
  }

  reset(x){
    this.x = x; this.z = 0; this.y = 0; this.vy = 0;
    this.facing = this.index===0?1:-1;
    this.hp = this.maxHP;
    this.blocking = false;
    this.attackTimer = 0;
    this.stun = 0;
    this.attacking = null;
    this.combo = 0;
    this.lastHit = -99;
    this.meter = 0;
    this.rollTimer = 0;
    this.rollCd = 0;
    this.invuln = 0;
    this.med = K.medCharges;
    this.medCd = 0;
    this.medTimer = 0;
    this.cooldowns = {};
    this.effects = {};
    this.damageMul = 1;
    this.dead = false;
    this.flash = 0;
    this.greaseCharges = this.cfg.id==='le_binde'?3:0;
    this.greaseActive = true;
    this.fireHits = 0;
    this.greaseBurnTimer = 0;
    this.reifTimer = 0;
    this.reifMul = 1.8;
    this.mopsCritTimer = 0;
    this.mopsCritChance = 0;
    this.mojoPoints = this.cfg.id==='mojo_bob'?0:0;
    this.mojoBuffTimer = 0;
    this.mojoDebuffTimer = 0;
    this.projectiles = [];
    this._moveLen = 0;
    this.armorTimer = 0;
    this.armorActive = false;
  }

  get grounded(){ return this.y <= 0.001; }
  get busy(){ return this.stun>0 || this.medTimer>0; }
  get alive(){ return this.hp>0; }
  cooldown(key){ return this.cooldowns[key]||0; }

  move(dx,dz,dt){
    if(this.busy || this.rollTimer>0) return;
    const len = Math.hypot(dx,dz);
    this._moveLen = len;
    if(len<0.01) return;
    let speed = this.moveSpeed * (this.effects.slow?0.6:1);
    if(this.blocking) speed = K.strafeSpeed * K.blockMoveScale;
    if(this.effects.invert){ dx=-dx; dz=-dz; }
    this.x += (dx/len)*speed*dt;
    this.z += (dz/len)*speed*dt;
    const r = K.arenaRadius - this.radius;
    this.x = clamp(this.x,-r,r);
    this.z = clamp(this.z,-r,r);
  }

  jump(){
    if(!this.grounded || this.busy) return;
    this.vy = K.jumpForce;
  }

  roll(dx,dz){
    if(this.rollCd>0 || this.busy || this.rollTimer>0) return false;
    const len = Math.hypot(dx,dz)||1;
    this.rollDir = {x:(dx||this.facing)/len, z:dz/len};
    this.rollTimer = K.rollDuration;
    this.rollCd = K.rollCooldown;
    this.invuln = K.rollInvulnerable;
    return true;
  }

  attack(heavy){
    if(this.attackTimer>0 || this.busy || this.blocking) return false;
    const damage = heavy ? this.heavy : this.light;
    this.attackTimer = heavy ? K.heavyCooldown : K.lightCooldown;
    this.attacking = {
      kind: heavy?'heavy':'light',
      damage,
      activeAt: heavy?0.20:0.12,
      endsAt: heavy?0.45:0.32,
      t:0, hasHit:false,
      range: this.range,
    };
    return true;
  }

  special(slot){
    const sp = this.cfg.specials[slot];
    if(!sp || this.busy || this.cooldown(sp.key)>0 || this.attackTimer>0) return false;
    this.cooldowns[sp.key] = sp.cooldown;
    this.attackTimer = 0.5;

    // Spezielle Logik pro Move
    switch(sp.key){
      case 'flaschenhals':
        this.attacking = {kind:'special', special:sp, damage:sp.damage, activeAt:0.15, endsAt:0.45, t:0, hasHit:false, range:sp.range};
        // Projektil
        this.projectiles.push(new Projectile(this, this.x+this.facing*0.5, this.z, this.facing*9, 0, sp.damage, 0.6, 'bleed', 1.2));
        break;
      case 'flaschenhals_ex':
        this.attacking = {kind:'special', special:sp, damage:sp.damage, activeAt:0.15, endsAt:0.45, t:0, hasHit:false, range:sp.range};
        this.projectiles.push(new Projectile(this, this.x+this.facing*0.5, this.z, this.facing*10, 0, sp.damage, 0.8, 'bleed_heavy', 1.2));
        break;
      case 'grosser_schwung':
        this.armorActive = true;
        this.armorTimer = 0.35; // Armor Frames 8-20 ~0.13-0.33s
        this.attacking = {kind:'special', special:sp, damage:sp.damage, activeAt:0.18, endsAt:0.55, t:0, hasHit:false, range:sp.range, armor:true};
        break;
      case 'reif':
        this.reifTimer = 6.0;
        this.attacking = {kind:'special', special:sp, damage:0, activeAt:0.5, endsAt:0.6, t:0, hasHit:true, range:0};
        break;
      case 'mops':
        this.mopsCritChance = 0.5;
        this.mopsCritTimer = 8.0;
        this.attacking = {kind:'special', special:sp, damage:0, activeAt:0.8, endsAt:1.0, t:0, hasHit:true, range:sp.range};
        break;
      case 'pfanne':
        this.attacking = {kind:'special', special:sp, damage:sp.damage, activeAt:0.12, endsAt:0.4, t:0, hasHit:false, range:sp.range, launch:true};
        if(this.greaseCharges>0) this.greaseCharges--;
        break;
      case 'riesenschwanz':
      case 'schwanz':
        this.attacking = {kind:'special', special:sp, damage:sp.damage, activeAt:0.20, endsAt:0.55, t:0, hasHit:false, range:sp.range, crit:true};
        break;
      case 'riesenschwanz_ex':
      case 'schwanz_ex':
        this.attacking = {kind:'special', special:sp, damage:sp.damage, activeAt:0.20, endsAt:0.85, t:0, hasHit:false, range:sp.range, multi:true, hits:3};
        break;
      case 'beutelchen':
        // Gamble
        {
          const roll = this.rng();
          let eff = 'none';
          let dmg = sp.damage;
          if(roll<0.05){ // Klavier 5%
            dmg = 25; eff='stun_heavy';
          } else if(roll<0.2){ eff='heal'; this.hp = Math.min(this.maxHP, this.hp+10); }
          else if(roll<0.35){ eff='burn'; }
          else if(roll<0.5){ eff='poison'; }
          else if(roll<0.65){ eff='slow'; }
          else if(roll<0.8){ eff='invert'; }
          else { eff='crit_self'; this.mojoPoints = Math.min(7, this.mojoPoints+1); }
          this.attacking = {kind:'special', special:sp, damage:dmg, activeAt:0.25, endsAt:0.5, t:0, hasHit:false, range:sp.range, gamble:eff};
        }
        break;
      case 'loeffelsturm':
        // 17 Löffel
        for(let i=0;i<17;i++){
          const angle = (this.rng()-0.5)*0.6;
          const speed = 8 + this.rng()*4;
          this.projectiles.push(new Projectile(this, this.x+this.facing*0.5, this.z, this.facing*speed*Math.cos(angle), speed*Math.sin(angle)*0.5, 1.2, 0.5, 'crit', 1.5));
        }
        this.attacking = {kind:'special', special:sp, damage:0, activeAt:0.3, endsAt:0.6, t:0, hasHit:true, range:0};
        break;
      case 'fuenfzig':
        this.projectiles.push(new Projectile(this, this.x+this.facing*0.5, this.z, this.facing*11, 0, sp.damage, 0.4, 'coin', 1.8));
        this.attacking = {kind:'special', special:sp, damage:0, activeAt:0.15, endsAt:0.35, t:0, hasHit:true, range:0};
        break;
      case 'dingeneldang':
        if(this.mojoPoints<7 && this.cfg.id==='mojo_bob'){
          // Nicht genug Mojo -> fail, aber trotzdem Cooldown
          return false;
        }
        if(this.cfg.id==='mojo_bob'){
          this.mojoPoints = 0;
          this.mojoBuffTimer = 8.0;
          this.mojoDebuffTimer = 0;
        }
        this.attacking = {kind:'special', special:sp, damage:0, activeAt:0.5, endsAt:0.7, t:0, hasHit:true, range:0};
        break;
      default:
        this.attacking = {kind:'special', special:sp, damage:sp.damage, activeAt:0.15, endsAt:0.45, t:0, hasHit:false, range:sp.range};
        break;
    }
    return true;
  }

  fatalBlow(){
    if(this.meter < K.fatalBlowMeterMax || this.busy) return false;
    this.meter = 0;
    const dmg = K.fatalBlowDamageMin + this.rng()*(K.fatalBlowDamageMax-K.fatalBlowDamageMin);
    this.attacking = {kind:'fatal', damage:dmg, activeAt:0.25, endsAt:0.9, t:0, hasHit:false, range:this.range+1.5};
    this.attackTimer = 1.0;
    return true;
  }

  useMed(){
    if(this.med<=0 || this.medCd>0 || this.busy) return false;
    this.med--;
    this.medCd = K.medCooldown;
    this.medTimer = K.medDuration;
    return true;
  }

  takeDamage(amount, fromX, attacker){
    if(this.invuln>0 || this.dead) return 0;
    let dmg = amount;

    // Le Binde Schmier-Schlüppa
    if(this.cfg.id==='le_binde' && this.greaseActive && !this.blocking){
      dmg *= 0.4; // -60%
      // 45% Grab-Rutsch
      if(attacker && this.rng()<0.45 && attacker.attacking && attacker.attacking.special && attacker.attacking.special.effect==='grab'){
        attacker.stun = Math.max(attacker.stun,0.2);
        return 0;
      }
    }

    // Armor
    if(this.armorActive){
      dmg *= 0.3;
    }

    // Block
    let blocked = false;
    if(this.blocking){
      // Topfdeckel bessere Blockwirkung für Uschi, aber auch hier
      let blockRed = K.blockDamageReduction;
      if(this.cfg.id==='uschi') blockRed *= 0.7;
      dmg *= blockRed;
      this.stun = Math.max(this.stun,0.1);
      blocked = true;
    } else {
      this.stun = Math.max(this.stun, K.hitStun);
      this.combo = 0;
    }

    // Sigi Firewall
    if(this.cfg.id==='sigi' && this.effects.firewall){
      dmg *= 0.5;
    }

    this.hp = Math.max(0, this.hp - dmg);
    this.meter = Math.min(K.fatalBlowMeterMax, this.meter + K.fatalBlowChargePerHitTaken);
    this.flash = 0.12;

    // Feuer für Le Binde
    if(attacker && (attacker.effects.burn || (attacker.attacking && attacker.attacking.special && attacker.attacking.special.effect==='burn'))){
      if(this.cfg.id==='le_binde'){
        this.fireHits++;
        if(this.fireHits>=3 && this.greaseActive){
          this.greaseActive = false;
          this.greaseBurnTimer = 8.0;
        }
      }
    }

    const dir = Math.sign(this.x - fromX) || 1;
    this.x += (dir * K.knockback * 0.12)/this.mass;
    if(!this.blocking) this.vy = Math.max(this.vy, K.knockbackUp*0.5);

    // Mojo Punkte für verlorene Situation (Mojo Bob)
    if(this.cfg.id==='mojo_bob' && !blocked && dmg>8){
      if(this.rng()<0.4) this.mojoPoints = Math.min(7, this.mojoPoints+1);
    }

    if(this.hp<=0) this.dead = true;
    return dmg;
  }

  tick(dt){
    if(this.attackTimer>0) this.attackTimer-=dt;
    if(this.stun>0) this.stun-=dt;
    if(this.invuln>0) this.invuln-=dt;
    if(this.rollCd>0) this.rollCd-=dt;
    if(this.medCd>0) this.medCd-=dt;
    if(this.flash>0) this.flash-=dt;
    if(this.armorTimer>0){ this.armorTimer-=dt; if(this.armorTimer<=0) this.armorActive=false; }
    if(this.reifTimer>0){ this.reifTimer-=dt; if(this.reifTimer<=0) this.reifTimer=0; }
    if(this.mopsCritTimer>0){ this.mopsCritTimer-=dt; if(this.mopsCritTimer<=0){ this.mopsCritTimer=0; this.mopsCritChance=0; } }
    if(this.mojoBuffTimer>0){ this.mojoBuffTimer-=dt; if(this.mojoBuffTimer<=0){ this.mojoBuffTimer=0; this.mojoDebuffTimer=15.0; } }
    if(this.mojoDebuffTimer>0){ this.mojoDebuffTimer-=dt; if(this.mojoDebuffTimer<=0) this.mojoDebuffTimer=0; }
    if(this.greaseBurnTimer>0){ this.greaseBurnTimer-=dt; if(this.greaseBurnTimer<=0){ this.greaseActive=true; this.fireHits=0; } }

    for(const key of Object.keys(this.cooldowns)){
      if(this.cooldowns[key]>0) this.cooldowns[key]-=dt;
    }

    if(this.medTimer>0){
      this.medTimer-=dt;
      if(this.medTimer<=0) this.hp = Math.min(this.maxHP, this.hp+K.medHeal);
    }

    if(this.rollTimer>0){
      const speed = K.rollDistance / K.rollDuration;
      this.x += this.rollDir.x*speed*dt;
      this.z += this.rollDir.z*speed*dt;
      this.rollTimer-=dt;
    }

    if(!this.grounded || this.vy>0){
      this.vy -= K.gravity*dt;
      this.y = Math.max(0, this.y + this.vy*dt);
      if(this.y===0) this.vy=0;
    }

    for(const [name,e] of Object.entries(this.effects)){
      e.t-=dt;
      if(e.dps) this.hp = Math.max(0, this.hp - e.dps*dt);
      if(e.t<=0) delete this.effects[name];
    }
    if(this.hp<=0) this.dead=true;

    // Projektile
    for(let i=this.projectiles.length-1;i>=0;i--){
      const p = this.projectiles[i];
      p.tick(dt);
      if(p.life<=0 || p.hit){
        this.projectiles.splice(i,1);
      }
    }

    const r = K.arenaRadius - this.radius;
    this.x = clamp(this.x,-r,r);
    this.z = clamp(this.z,-r,r);
  }
}

let simClock = 0;
function performanceNow(){ return simClock; }

export class Match {
  constructor(opts={}){
    this.rng = makeRandom(opts.seed ?? Date.now()%100000);
    this.p1 = new Fighter(byId(opts.p1||'le_binde'),0,this.rng);
    this.p2 = new Fighter(byId(opts.p2||'mojo_bob'),1,this.rng);
    this.bestOf = opts.bestOf ?? K.defaultBestOfRounds;
    this.roundTime = opts.roundTime ?? K.defaultRoundTime;
    this.aiLevel = opts.aiLevel ?? 2;
    this.p2IsAI = opts.p2IsAI!==false;

    if(this.p2IsAI){
      const level = AI_LEVELS[clamp(this.aiLevel,0,AI_LEVELS.length-1)];
      this.p2.light *= level.damage;
      this.p2.heavy *= level.damage;
      this.ai = {level, think:0};
    }

    this.wins = [0,0];
    this.round = 1;
    this.timer = this.roundTime;
    this.state = 'fight';
    this.stateTimer = 0;
    this.winner = null;
    this.events = [];
    simClock = 0;
  }

  emit(type,data={}){ this.events.push({type,...data}); }

  step(input, dt=STEP){
    simClock+=dt;
    this.events.length=0;

    if(this.state==='fight'){
      this.applyInput(this.p1, input.p1);
      if(this.p2IsAI) this.thinkAI(dt);
      else this.applyInput(this.p2, input.p2||{});

      this.p1.tick(dt);
      this.p2.tick(dt);
      this.faceEachOther();
      this.resolveAttack(this.p1,this.p2);
      this.resolveAttack(this.p2,this.p1);
      this.resolveProjectiles(this.p1,this.p2);
      this.resolveProjectiles(this.p2,this.p1);
      this.separate();

      this.timer-=dt;
      if(this.timer<=0 || this.p1.dead || this.p2.dead) this.endRound();
    } else {
      this.stateTimer-=dt;
      if(this.stateTimer<=0 && this.state==='roundEnd') this.startRound();
    }
    return this.events;
  }

  applyInput(f,i={}){
    if(!f.alive) return;
    f.blocking = !!i.block && f.grounded && !f.busy;
    f.move(i.dx||0,i.dz||0,STEP);
    if(i.jump) f.jump();
    if(i.roll && f.roll(i.dx||0,i.dz||0)) this.emit('roll',{f});
    if(i.light && f.attack(false)) this.emit('swing',{f,heavy:false});
    if(i.heavy && f.attack(true)) this.emit('swing',{f,heavy:true});
    if(i.special1 && f.special(0)) this.emit('special',{f,slot:0});
    if(i.special2 && f.special(1)) this.emit('special',{f,slot:1});
    if(i.special3 && f.special(2)) this.emit('special',{f,slot:2});
    if(i.special4 && f.special(3)) this.emit('special',{f,slot:3});
    if(i.special5 && f.special(4)) this.emit('special',{f,slot:4});
    if(i.special6 && f.special(5)) this.emit('special',{f,slot:5});
    if(i.fatal && f.fatalBlow()) this.emit('fatal',{f});
    if(i.med && f.useMed()) this.emit('med',{f});
  }

  thinkAI(dt){
    const ai = this.ai;
    const me = this.p2, foe = this.p1;
    if(!me.alive) return;
    ai.think-=dt;
    const d = dist(me,foe);
    const towards = {x: foe.x-me.x, z: foe.z-me.z};
    if(d>me.range*0.9) me.move(towards.x,towards.z,dt);

    if(ai.think>0) return;
    ai.think = ai.level.thinkMin + this.rng()*(ai.level.thinkMax-ai.level.thinkMin);

    if(d<=me.range+0.6){
      const r = this.rng();
      if(r < ai.level.special){
        const slot = Math.floor(this.rng()*me.cfg.specials.length);
        if(me.special(slot)) this.emit('special',{f:me,slot});
      } else if(r < ai.level.special + ai.level.aggression*0.6){
        const heavy = this.rng()<ai.level.combo;
        if(me.attack(heavy)) this.emit('swing',{f:me,heavy});
      } else if(this.rng()<ai.level.block) me.blocking=true;
      else me.blocking=false;
    } else {
      me.blocking=false;
      if(me.hp<me.maxHP*0.35 && this.rng()<0.5 && me.useMed()) this.emit('med',{f:me});
    }
  }

  resolveAttack(attacker,target){
    const a = attacker.attacking;
    if(!a) return;
    a.t+=STEP;

    if(!a.hasHit && a.t>=a.activeAt){
      const d = dist(attacker,target);
      const reach = a.range * attacker.hitboxScale + target.radius;
      const inFront = Math.sign(target.x - attacker.x)===attacker.facing || Math.abs(target.x-attacker.x)<0.6;
      const heightOk = Math.abs(attacker.y-target.y)<1.6;

      if(d<=reach && inFront && heightOk){
        a.hasHit = true;
        if(a.multi){
          // Multi-hit handled separately
          a.hasHit = false;
          a.hitsDone = (a.hitsDone||0)+1;
          if(a.hitsDone>= (a.hits||3)) a.hasHit=true;
        }
        let dmg = a.damage * attacker.damageMul;

        // REIF! buff
        if(attacker.reifTimer>0){
          dmg *= attacker.reifMul;
          attacker.reifTimer=0;
        }
        // Mops Krit
        let crit=false;
        if(attacker.mopsCritTimer>0 && this.rng()<attacker.mopsCritChance){
          dmg*=1.5; crit=true;
        }
        // Mojo Bob Krit
        if(attacker.cfg.id==='mojo_bob'){
          let critChance = 0.15;
          if(attacker.mojoBuffTimer>0) critChance=1.0;
          else if(attacker.mopsCritTimer>0) critChance+=attacker.mopsCritChance;
          if(this.rng()<critChance){ dmg*=2.4; crit=true; }
          if(attacker.mojoDebuffTimer>0) dmg*=0.7;
        }

        const blocked = target.blocking && !(a.special && (a.special.effect==='pierce' || a.special.effect==='armor_knock'));
        const dealt = target.takeDamage(blocked?dmg:dmg, attacker.x, attacker);

        attacker.combo = simClock - attacker.lastHit < K.comboWindow ? attacker.combo+1 : 1;
        attacker.lastHit = simClock;
        attacker.meter = Math.min(K.fatalBlowMeterMax, attacker.meter + K.fatalBlowChargePerHit);

        // Effekte
        const eff = a.special && a.special.effect;
        if(eff==='burn' || eff==='bleed' || eff==='bleed_heavy') target.effects.bleed={t:5,dps:1.2};
        if(eff==='poison') target.effects.poison={t:5,dps:1.5};
        if(eff==='dot') target.effects.dot={t:6,dps:1};
        if(eff==='slow') target.effects.slow={t:3};
        if(eff==='stun' || eff==='summon_stun' || eff==='stun_heavy') target.stun=Math.max(target.stun, eff==='stun_heavy'?1.2:0.6);
        if(eff==='armor_knock'){ target.vy = Math.max(target.vy, K.knockbackUp); target.x += (target.x-attacker.x>0?1:-1)*1.2; }
        if(eff==='launch'){ target.vy = K.jumpForce*0.9; target.stun=Math.max(target.stun,0.5); }
        if(eff==='invert') target.effects.invert={t:4};
        if(a.launch){ target.vy = K.jumpForce; target.stun=Math.max(target.stun,0.4); }
        if(a.special && a.special.key==='mops'){
          this.emit('trash_yard',{});
        }

        // Gamble Effekte
        if(a.gamble){
          if(a.gamble==='burn') target.effects.burn={t:4,dps:2};
          if(a.gamble==='poison') target.effects.poison={t:5,dps:1.5};
          if(a.gamble==='slow') target.effects.slow={t:3};
          if(a.gamble==='invert') target.effects.invert={t:4};
          if(a.gamble==='stun_heavy') target.stun=Math.max(target.stun,1.5);
          if(a.gamble==='heal') attacker.hp=Math.min(attacker.maxHP, attacker.hp+15);
        }

        this.emit('hit',{attacker,target,damage:dealt,blocked:target.blocking,crit,kind:a.kind,combo:attacker.combo,special:a.special});
        if(target.dead) this.emit('ko',{target});
      }
    }

    if(a.t>=a.endsAt) attacker.attacking=null;
  }

  resolveProjectiles(attacker,target){
    for(const p of attacker.projectiles){
      if(p.hit) continue;
      const d = Math.hypot(p.x-target.x, p.z-target.z);
      if(d < target.radius + p.range && Math.abs(attacker.y-target.y)<1.6){
        p.hit=true;
        let dmg = p.damage;
        // Krit für Löffel
        let crit=false;
        if(p.effect==='crit' && attacker.cfg.id==='mojo_bob' && this.rng()<0.15){
          dmg*=2.4; crit=true;
        }
        const dealt = target.takeDamage(dmg, p.x, attacker);
        attacker.combo = simClock - attacker.lastHit < K.comboWindow ? attacker.combo+1 : 1;
        attacker.lastHit = simClock;
        attacker.meter = Math.min(K.fatalBlowMeterMax, attacker.meter + K.fatalBlowChargePerHit);

        if(p.effect==='bleed' || p.effect==='bleed_heavy') target.effects.bleed={t:5,dps:p.effect==='bleed_heavy'?2:1};
        if(p.effect==='burn') target.effects.burn={t:4,dps:2};
        if(p.effect==='poison') target.effects.poison={t:5,dps:1.5};
        if(p.effect==='slow') target.effects.slow={t:3};

        this.emit('hit',{attacker,target,damage:dealt,blocked:false,crit,kind:'projectile',combo:attacker.combo,special:{key:p.effect}});
        this.emit('projectile_hit',{x:p.x,z:p.z,effect:p.effect});
        if(target.dead) this.emit('ko',{target});
      }
    }
  }

  faceEachOther(){
    this.p1.facing = this.p2.x>=this.p1.x?1:-1;
    this.p2.facing = this.p1.x>=this.p2.x?1:-1;
  }

  separate(){
    const d = dist(this.p1,this.p2);
    const min = this.p1.radius+this.p2.radius;
    if(d>=min||d===0) return;
    const push = (min-d)/2;
    const nx = (this.p1.x-this.p2.x)/d;
    const nz = (this.p1.z-this.p2.z)/d;
    const total = this.p1.mass+this.p2.mass;
    this.p1.x+= nx*push*(this.p2.mass/total)*2;
    this.p1.z+= nz*push*(this.p2.mass/total)*2;
    this.p2.x-= nx*push*(this.p1.mass/total)*2;
    this.p2.z-= nz*push*(this.p1.mass/total)*2;
  }

  endRound(){
    let winnerIndex=null;
    if(this.p1.hp>this.p2.hp) winnerIndex=0;
    else if(this.p2.hp>this.p1.hp) winnerIndex=1;

    if(winnerIndex!==null) this.wins[winnerIndex]++;
    this.emit('roundEnd',{winnerIndex,wins:[...this.wins]});

    const needed = Math.ceil(this.bestOf/2);
    if(this.wins[0]>=needed || this.wins[1]>=needed){
      this.state='matchEnd';
      this.winner = this.wins[0]>this.wins[1]?this.p1:this.p2;
      this.emit('matchEnd',{winner:this.winner});
    } else if(this.round>=this.bestOf){
      this.state='matchEnd';
      this.winner = this.wins[0]===this.wins[1]?null:(this.wins[0]>this.wins[1]?this.p1:this.p2);
      this.emit('matchEnd',{winner:this.winner});
    } else {
      this.state='roundEnd';
      this.stateTimer=K.roundEndDelay;
    }
  }

  startRound(){
    this.round++;
    this.p1.reset(-4);
    this.p2.reset(4);
    this.timer=this.roundTime;
    this.state='fight';
    this.emit('roundStart',{round:this.round});
  }
}
