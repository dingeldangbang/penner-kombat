/**
 * input3d.js — Erweiterte Eingabe für 3D Arena mit allen 6 Specials
 * 
 * Mapping für 2 Spieler, 100% spielbar:
 * Le Binde / Mojo Bob:
 *  P1: WASD bewegen, J=Light, K=Heavy, U=Sp1 Flaschenhals/Riesenschwanz, I=Sp2 Mops/Beutelchen,
 *      L=Sp3 Großer Schwung/Löffelsturm, M=Sp4 REIF!/Fünfzig Cent, N=Sp5 Pfanne/Dingeneldang, Komma=Sp6 EX
 *  P2: Pfeile bewegen, Numpad 1=Light, 2=Heavy, 4=Sp1,5=Sp2,6=Roll,7=Sp3,8=Sp4,9=Sp5, + = Sp6, 0=Jump
 *  + Q/E für Spezial-Modifier (Mojo Wette)
 * 
 * Zusätzlich Command-Input: speichert letzte Richtungen für QCF etc.
 */

export class InputState3D {
  constructor(){
    this.keys = new Set();
    this.pressed = new Set();
    this.touch = {dx:0,dz:0,buttons:new Set(),tapped:new Set()};
    this.history = []; // {dir, time}
    this.bindKeyboard();
  }

  bindKeyboard(){
    window.addEventListener('keydown', (e)=>{
      if(!this.keys.has(e.code)) this.pressed.add(e.code);
      this.keys.add(e.code);
      // Speichere Richtungs-History für Command-Input
      const dir = this.codeToNumpad(e.code);
      if(dir){
        this.history.push({dir, time: performance.now()});
        if(this.history.length>12) this.history.shift();
      }
      if(['Space','ArrowUp','ArrowDown','ArrowLeft','ArrowRight'].includes(e.code)) e.preventDefault();
    });
    window.addEventListener('keyup', (e)=>this.keys.delete(e.code));
    window.addEventListener('blur', ()=>this.keys.clear());
  }

  codeToNumpad(code){
    // WASD -> Numpad relativ zum Gegner (vereinfacht: W=8, S=2, A=4, D=6)
    if(code==='KeyW' || code==='ArrowUp') return 8;
    if(code==='KeyS' || code==='ArrowDown') return 2;
    if(code==='KeyA' || code==='ArrowLeft') return 4;
    if(code==='KeyD' || code==='ArrowRight') return 6;
    if(code==='KeyW' && this.keys.has('KeyD')) return 9;
    if(code==='KeyW' && this.keys.has('KeyA')) return 7;
    if(code==='KeyS' && this.keys.has('KeyD')) return 3;
    if(code==='KeyS' && this.keys.has('KeyA')) return 1;
    return null;
  }

  readP1(){
    const k=this.keys, p=this.pressed, t=this.touch;
    const dx = (k.has('KeyD')?1:0)-(k.has('KeyA')?1:0)+t.dx;
    const dz = (k.has('KeyW')?1:0)-(k.has('KeyS')?1:0)+t.dz;
    return {
      dx, dz,
      block: k.has('ShiftLeft')||k.has('ShiftRight')||t.buttons.has('block'),
      jump: p.has('Space')||t.tapped.has('jump'),
      light: p.has('KeyJ')||t.tapped.has('light'),
      heavy: p.has('KeyK')||t.tapped.has('heavy'),
      special1: p.has('KeyU')||t.tapped.has('s1'), // Flaschenhals / Riesenschwanz
      special2: p.has('KeyI')||t.tapped.has('s2'), // Mops / Beutelchen
      special3: p.has('KeyL')||t.tapped.has('s3'), // Großer Schwung / Löffelsturm
      special4: p.has('KeyM')||p.has('KeyO')&&k.has('KeyU')||t.tapped.has('s4'), // REIF! / Fünfzig
      special5: p.has('KeyN')||t.tapped.has('s5'), // Pfanne / Dingeneldang
      special6: p.has('Comma')||p.has('KeyP')||t.tapped.has('s6'), // EX
      roll: p.has('KeyO')||t.tapped.has('roll'),
      fatal: p.has('KeyY')||t.tapped.has('fatal'),
      med: p.has('KeyH')||t.tapped.has('med'),
      history: [...this.history]
    };
  }

  readP2(){
    const k=this.keys, p=this.pressed;
    return {
      dx: (k.has('ArrowRight')?1:0)-(k.has('ArrowLeft')?1:0),
      dz: (k.has('ArrowUp')?1:0)-(k.has('ArrowDown')?1:0),
      block: k.has('Numpad3')||k.has('ControlRight'),
      jump: p.has('Numpad0')||p.has('Enter'),
      light: p.has('Numpad1'),
      heavy: p.has('Numpad2'),
      special1: p.has('Numpad4'), // Sp1
      special2: p.has('Numpad5'), // Sp2
      special3: p.has('Numpad7'), // Sp3
      special4: p.has('Numpad8'), // Sp4
      special5: p.has('Numpad9'), // Sp5
      special6: p.has('NumpadAdd'), // EX / Sp6
      roll: p.has('Numpad6'),
      fatal: p.has('NumpadMultiply'),
      med: p.has('NumpadEnter'),
      history: [...this.history]
    };
  }

  endFrame(){
    this.pressed.clear();
    this.touch.tapped.clear();
  }

  // Prüft ob eine Numpad-Sequenz in der History enthalten ist (wie MoveData.Matches)
  checkSequence(seq){
    if(!seq || seq.length===0) return true;
    if(this.history.length < seq.length) return false;
    const recent = this.history.slice(-seq.length).map(h=>h.dir);
    for(let i=0;i<seq.length;i++) if(recent[i]!==seq[i]) return false;
    return true;
  }
}

export function buildTouchControls3D(root, input, onAny){
  const layer = document.createElement('div');
  layer.className = 'touch-layer';

  // Joystick
  const stick = document.createElement('div');
  stick.className = 'joystick';
  const knob = document.createElement('div');
  knob.className = 'knob';
  stick.appendChild(knob);
  layer.appendChild(stick);

  let stickId=null, originX=0, originY=0;
  const radius=55;
  const startStick=(e)=>{
    const t=e.changedTouches?e.changedTouches[0]:e;
    stickId=t.identifier??'mouse';
    const rect=stick.getBoundingClientRect();
    originX=rect.left+rect.width/2;
    originY=rect.top+rect.height/2;
    onAny(); e.preventDefault();
  };
  const moveStick=(e)=>{
    if(stickId===null) return;
    const touches=e.changedTouches?Array.from(e.changedTouches):[e];
    const t=touches.find(x=>(x.identifier??'mouse')===stickId);
    if(!t) return;
    let dx=t.clientX-originX, dy=t.clientY-originY;
    const len=Math.hypot(dx,dy)||1;
    const clamped=Math.min(len,radius);
    dx=(dx/len)*clamped; dy=(dy/len)*clamped;
    knob.style.transform=`translate(${dx}px, ${dy}px)`;
    const dead=0.18;
    const nx=dx/radius, ny=-dy/radius;
    input.touch.dx=Math.abs(nx)>dead?nx:0;
    input.touch.dz=Math.abs(ny)>dead?ny:0;
    e.preventDefault();
  };
  const endStick=()=>{
    stickId=null;
    knob.style.transform='translate(0,0)';
    input.touch.dx=0; input.touch.dz=0;
  };
  stick.addEventListener('touchstart',startStick,{passive:false});
  stick.addEventListener('touchmove',moveStick,{passive:false});
  stick.addEventListener('touchend',endStick);
  stick.addEventListener('touchcancel',endStick);
  stick.addEventListener('mousedown',startStick);
  window.addEventListener('mousemove',moveStick);
  window.addEventListener('mouseup',endStick);

  // Buttons — 6 Specials + Light/Heavy etc
  const pad=document.createElement('div');
  pad.className='buttonpad';
  pad.style.gridTemplateColumns='repeat(4, 56px)';
  const defs=[
    {id:'light', label:'□', title:'leicht', cls:'b-light'},
    {id:'heavy', label:'△', title:'schwer', cls:'b-heavy'},
    {id:'s1', label:'S1', title:'Flaschenhals / Schwanz', cls:'b-s1'},
    {id:'s2', label:'S2', title:'Mops / Beutelchen', cls:'b-s2'},
    {id:'s3', label:'S3', title:'Schwung / Löffel', cls:'b-s3'},
    {id:'s4', label:'S4', title:'REIF! / 50Cent', cls:'b-s4'},
    {id:'s5', label:'S5', title:'Pfanne / Dingeldang', cls:'b-s5'},
    {id:'s6', label:'EX', title:'EX', cls:'b-ex'},
    {id:'block', label:'BLOCK', title:'halten', cls:'b-block', hold:true},
    {id:'jump', label:'✕', title:'Sprung', cls:'b-jump'},
    {id:'roll', label:'ROLLE', title:'ausweichen', cls:'b-roll'},
    {id:'fatal', label:'X-RAY', title:'Fatal Blow', cls:'b-fatal'},
  ];
  for(const d of defs){
    const b=document.createElement('button');
    b.className=`tbtn ${d.cls}`;
    b.textContent=d.label;
    b.title=d.title;
    const down=(e)=>{
      onAny();
      if(d.hold) input.touch.buttons.add(d.id);
      else input.touch.tapped.add(d.id);
      b.classList.add('active');
      if(navigator.vibrate) navigator.vibrate(8);
      e.preventDefault();
    };
    const up=(e)=>{
      if(d.hold) input.touch.buttons.delete(d.id);
      b.classList.remove('active');
      if(e) e.preventDefault();
    };
    b.addEventListener('touchstart',down,{passive:false});
    b.addEventListener('touchend',up,{passive:false});
    b.addEventListener('touchcancel',up);
    b.addEventListener('mousedown',down);
    b.addEventListener('mouseup',up);
    b.addEventListener('mouseleave',up);
    pad.appendChild(b);
  }
  layer.appendChild(pad);
  root.appendChild(layer);
  return layer;
}
