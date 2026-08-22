/**
 * pose.js — Pose-Bibliothek für Penner Kombat 3D.
 * 
 * Jede Pose ist ein Satz von Gelenk-Rotationen (Euler Radians).
 * Das Skelett ist absichtlich simpel gehalten (15 Gelenke), damit
 * es auch ohne Rig funktioniert — genau wie FighterFactory im Unity-Code.
 * 
 * Gelenke: hips (root), spine, chest, head, lShoulder, lElbow, rShoulder, rElbow,
 * lHip, lKnee, rHip, rKnee, lAnkle, rAnkle, tail (für Mojo Bobs Schwanz)
 */

export const JOINTS = ['hips','spine','chest','head','lShoulder','lElbow','rShoulder','rElbow','lHip','lKnee','rHip','rKnee','lAnkle','rAnkle','tail'];

function r(x=0,y=0,z=0){ return {x: x*Math.PI/180, y: y*Math.PI/180, z: z*Math.PI/180}; }

export const POSES = {
  idle: {
    hips: r(0,0,0), spine: r(2,0,0), chest: r(-2,0,0), head: r(-5,0,0),
    lShoulder: r(10,0,-25), lElbow: r(0,0,-70), rShoulder: r(10,0,25), rElbow: r(0,0,70),
    lHip: r(-5,0,5), lKnee: r(5,0,0), rHip: r(-5,0,-5), rKnee: r(5,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  walk: {
    hips: r(3,0,0), spine: r(5,0,0), chest: r(0,5,0), head: r(0,0,0),
    lShoulder: r(-15,0,-20), lElbow: r(0,0,-60), rShoulder: r(20,0,20), rElbow: r(0,0,60),
    lHip: r(-25,0,0), lKnee: r(30,0,0), rHip: r(15,0,0), rKnee: r(10,0,0),
    lAnkle: r(-10,0,0), rAnkle: r(5,0,0), tail: r(10,0,0)
  },
  walk2: { // zweiter Schritt
    hips: r(3,0,0), spine: r(5,0,0), chest: r(0,-5,0), head: r(0,0,0),
    lShoulder: r(20,0,-20), lElbow: r(0,0,-60), rShoulder: r(-15,0,20), rElbow: r(0,0,60),
    lHip: r(15,0,0), lKnee: r(10,0,0), rHip: r(-25,0,0), rKnee: r(30,0,0),
    lAnkle: r(5,0,0), rAnkle: r(-10,0,0), tail: r(-10,0,0)
  },
  light_punch: {
    hips: r(0,15,0), spine: r(10,10,0), chest: r(15,15,0), head: r(0,20,0),
    lShoulder: r(10,0,-20), lElbow: r(0,0,-70),
    rShoulder: r(-80,20,30), rElbow: r(0,0,20),
    lHip: r(-5,0,10), lKnee: r(10,0,0), rHip: r(-10,0,-5), rKnee: r(20,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,15,0)
  },
  heavy_punch: {
    hips: r(0,35,0), spine: r(15,20,0), chest: r(25,30,0), head: r(5,30,0),
    lShoulder: r(-20,0,-40), lElbow: r(0,0,-90),
    rShoulder: r(-110,10,50), rElbow: r(0,0,10),
    lHip: r(-15,0,15), lKnee: r(25,0,0), rHip: r(-20,0,-10), rKnee: r(35,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,25,0)
  },
  block: {
    hips: r(0,0,0), spine: r(5,0,0), chest: r(10,0,0), head: r(-10,0,0),
    lShoulder: r(-70,0,-30), lElbow: r(0,0,-100), rShoulder: r(-70,0,30), rElbow: r(0,0,100),
    lHip: r(-10,0,5), lKnee: r(20,0,0), rHip: r(-10,0,-5), rKnee: r(20,0,0),
    lAnkle: r(-10,0,0), rAnkle: r(-10,0,0), tail: r(0,0,0)
  },
  jump: {
    hips: r(10,0,0), spine: r(-10,0,0), chest: r(-5,0,0), head: r(10,0,0),
    lShoulder: r(-30,0,-40), lElbow: r(0,0,-50), rShoulder: r(-30,0,40), rElbow: r(0,0,50),
    lHip: r(-60,0,10), lKnee: r(80,0,0), rHip: r(-60,0,-10), rKnee: r(80,0,0),
    lAnkle: r(20,0,0), rAnkle: r(20,0,0), tail: r(-20,0,0)
  },
  roll: {
    hips: r(90,0,0), spine: r(20,0,0), chest: r(20,0,0), head: r(30,0,0),
    lShoulder: r(-40,0,-60), lElbow: r(0,0,-90), rShoulder: r(-40,0,60), rElbow: r(0,0,90),
    lHip: r(-90,0,0), lKnee: r(120,0,0), rHip: r(-90,0,0), rKnee: r(120,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  hit_react: {
    hips: r(-15,0,0), spine: r(-20,0,0), chest: r(-25,0,0), head: r(-20,0,0),
    lShoulder: r(20,0,-50), lElbow: r(0,0,-40), rShoulder: r(20,0,50), rElbow: r(0,0,40),
    lHip: r(5,0,0), lKnee: r(5,0,0), rHip: r(5,0,0), rKnee: r(5,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  death: {
    hips: r(90,0,0), spine: r(30,0,0), chest: r(20,0,0), head: r(40,0,0),
    lShoulder: r(10,0,-70), lElbow: r(0,0,-20), rShoulder: r(10,0,70), rElbow: r(0,0,20),
    lHip: r(0,0,0), lKnee: r(0,0,0), rHip: r(0,0,0), rKnee: r(0,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  // Le Binde specials
  flaschenhals: {
    hips: r(0,25,0), spine: r(10,15,0), chest: r(20,25,0), head: r(0,25,0),
    lShoulder: r(-60,0,-20), lElbow: r(0,0,-30),
    rShoulder: r(-90,30,40), rElbow: r(0,0,15),
    lHip: r(-10,0,10), lKnee: r(15,0,0), rHip: r(-5,0,-10), rKnee: r(10,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  grosser_schwung: {
    hips: r(0,50,0), spine: r(20,30,10), chest: r(30,50,10), head: r(10,40,0),
    lShoulder: r(-120,0,-20), lElbow: r(0,0,10),
    rShoulder: r(-120,0,40), rElbow: r(0,0,10),
    lHip: r(-20,0,20), lKnee: r(30,0,0), rHip: r(-25,0,-15), rKnee: r(40,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  reif: {
    hips: r(-10,0,0), spine: r(-15,0,0), chest: r(-20,0,0), head: r(-30,0,0),
    lShoulder: r(-20,0,-60), lElbow: r(0,0,-120), rShoulder: r(-20,0,60), rElbow: r(0,0,120),
    lHip: r(0,0,0), lKnee: r(0,0,0), rHip: r(0,0,0), rKnee: r(0,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  mops: {
    hips: r(5,0,0), spine: r(0,0,0), chest: r(-10,0,0), head: r(-20,-30,0),
    lShoulder: r(-80,0,-10), lElbow: r(0,0,-20),
    rShoulder: r(-80,0,10), rElbow: r(0,0,-20),
    lHip: r(-10,0,5), lKnee: r(15,0,0), rHip: r(-10,0,-5), rKnee: r(15,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  pfanne: {
    hips: r(-5,0,0), spine: r(-10,0,0), chest: r(-20,0,0), head: r(-15,0,0),
    lShoulder: r(-10,0,-20), lElbow: r(0,0,-70),
    rShoulder: r(-150,0,30), rElbow: r(0,0,20),
    lHip: r(-15,0,10), lKnee: r(20,0,0), rHip: r(-10,0,-10), rKnee: r(25,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  // Mojo Bob specials
  schwanz: {
    hips: r(0,30,0), spine: r(10,20,0), chest: r(20,30,0), head: r(0,30,0),
    lShoulder: r(10,0,-20), lElbow: r(0,0,-60),
    rShoulder: r(-30,0,60), rElbow: r(0,0,100),
    lHip: r(-10,0,5), lKnee: r(15,0,0), rHip: r(-15,0,-5), rKnee: r(20,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(-30,20,0)
  },
  schwanz_ex: {
    hips: r(0,40,0), spine: r(15,25,0), chest: r(25,40,0), head: r(5,35,0),
    lShoulder: r(10,0,-20), lElbow: r(0,0,-60),
    rShoulder: r(-40,0,80), rElbow: r(0,0,120),
    lHip: r(-10,0,10), lKnee: r(15,0,0), rHip: r(-15,0,-10), rKnee: r(20,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(-60,30,20)
  },
  beutelchen: {
    hips: r(0,0,0), spine: r(5,0,0), chest: r(0,0,0), head: r(-10,0,0),
    lShoulder: r(-40,0,-30), lElbow: r(0,0,-80),
    rShoulder: r(-40,0,30), rElbow: r(0,0,-80),
    lHip: r(-5,0,5), lKnee: r(10,0,0), rHip: r(-5,0,-5), rKnee: r(10,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  loeffelsturm: {
    hips: r(0,20,0), spine: r(10,10,0), chest: r(15,15,0), head: r(0,20,0),
    lShoulder: r(-90,0,-30), lElbow: r(0,0,-20),
    rShoulder: r(-90,0,30), rElbow: r(0,0,-20),
    lHip: r(-10,0,10), lKnee: r(15,0,0), rHip: r(-10,0,-10), rKnee: r(15,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  fuenfzig: {
    hips: r(0,15,0), spine: r(5,10,0), chest: r(10,15,0), head: r(0,15,0),
    lShoulder: r(-20,0,-20), lElbow: r(0,0,-60),
    rShoulder: r(-70,20,20), rElbow: r(0,0,30),
    lHip: r(-5,0,5), lKnee: r(10,0,0), rHip: r(-5,0,-5), rKnee: r(10,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  },
  dingeneldang: {
    hips: r(-10,0,0), spine: r(-20,0,0), chest: r(-30,0,0), head: r(-40,0,0),
    lShoulder: r(-100,0,-40), lElbow: r(0,0,-30),
    rShoulder: r(-100,0,40), rElbow: r(0,0,-30),
    lHip: r(0,0,0), lKnee: r(0,0,0), rHip: r(0,0,0), rKnee: r(0,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(30,0,0)
  },
  fatal: {
    hips: r(0,30,0), spine: r(15,20,0), chest: r(25,30,0), head: r(10,30,0),
    lShoulder: r(-90,0,-40), lElbow: r(0,0,-10),
    rShoulder: r(-90,0,40), rElbow: r(0,0,-10),
    lHip: r(-20,0,10), lKnee: r(30,0,0), rHip: r(-20,0,-10), rKnee: r(30,0,0),
    lAnkle: r(0,0,0), rAnkle: r(0,0,0), tail: r(0,0,0)
  }
};

export function lerpPose(a, b, t){
  const out = {};
  for(const j of JOINTS){
    const ja = a[j] || {x:0,y:0,z:0};
    const jb = b[j] || {x:0,y:0,z:0};
    out[j] = {
      x: ja.x + (jb.x - ja.x)*t,
      y: ja.y + (jb.y - ja.y)*t,
      z: ja.z + (jb.z - ja.z)*t
    };
  }
  return out;
}

// Pose-Auswahl basierend auf Fighter-Zustand (wie FighterController.AnimTrigger)
export function selectPose(fighter, time){
  if(fighter.dead) return POSES.death;
  if(fighter.rollTimer > 0) return POSES.roll;
  if(fighter.stun > 0.05) return POSES.hit_react;
  if(!fighter.grounded) return POSES.jump;
  if(fighter.blocking) return POSES.block;
  if(fighter.attacking){
    const a = fighter.attacking;
    const key = a.special ? a.special.key : a.kind;
    // Mapping special keys zu Posen
    const map = {
      light: 'light_punch',
      heavy: 'heavy_punch',
      fatal: 'fatal',
      flaschenhals: 'flaschenhals',
      flaschenhals_ex: 'flaschenhals',
      grosser_schwung: 'grosser_schwung',
      reif: 'reif',
      mops: 'mops',
      pfanne: 'pfanne',
      riesenschwanz: 'schwanz',
      riesenschwanz_ex: 'schwanz_ex',
      schwanz: 'schwanz',
      schwanz_ex: 'schwanz_ex',
      beutelchen: 'beutelchen',
      loeffelsturm: 'loeffelsturm',
      fuenfzig: 'fuenfzig',
      dingeneldang: 'dingeneldang'
    };
    return POSES[map[key] || (a.kind==='light'?'light_punch':'heavy_punch')] || POSES.light_punch;
  }
  // Walk cycle
  if(fighter._moveLen && fighter._moveLen > 0.1){
    const cycle = Math.sin(time*8) > 0 ? POSES.walk : POSES.walk2;
    return cycle;
  }
  return POSES.idle;
}
