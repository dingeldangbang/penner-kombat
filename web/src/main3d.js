/**
 * main3d.js — 3D Fight Arena für Le Binde vs Mojo Bob
 * 100% spielbare Kombos und Effekte, Pose-System
 */

import { ROSTER, STATURE, AI_LEVELS, PALETTE, K, byId } from './data.js';
import { Match, STEP } from './sim3d.js';
import { Renderer3D } from './render3d.js';
import { InputState3D, buildTouchControls3D } from './input3d.js';
import * as A from './audio.js';

const $ = (sel)=>document.querySelector(sel);
const isTouch = matchMedia('(hover: none)').matches || 'ontouchstart' in window;

const state = {
  p1: 0, // le_binde index
  p2: 2, // mojo_bob index
  p2IsAI: false, // 2 Spieler Standard
  aiLevel: 2,
  roundsIndex: 1,
  match: null,
  running: false,
  accumulator: 0,
  last: 0,
  time: 0
};
const ROUND_OPTIONS = [1,3,5];

const input = new InputState3D();
let renderer = null;
const beat = new A.BeatLoop();

// Finde Le Binde und Mojo Bob Indices
const leBindeIdx = ROSTER.findIndex(f=>f.id==='le_binde');
const mojoBobIdx = ROSTER.findIndex(f=>f.id==='mojo_bob');
state.p1 = leBindeIdx>=0?leBindeIdx:0;
state.p2 = mojoBobIdx>=0?mojoBobIdx:2;

function showScreen(id){
  for(const el of document.querySelectorAll('.screen')) el.classList.toggle('hidden', el.id!==id);
  $('#hud').classList.toggle('hidden', id!=='screen-fight');
  $('#stage3d').classList.toggle('hidden', id!=='screen-fight');
  const touchLayer = document.querySelector('.touch-layer');
  if(touchLayer) touchLayer.classList.toggle('hidden', id!=='screen-fight');
}

function buildSelect(){
  const list1 = $('#list-p1'), list2 = $('#list-p2');
  if(!list1 || !list2) return;
  list1.innerHTML=''; list2.innerHTML='';
  // Nur Le Binde und Mojo Bob hervorheben, aber alle anzeigen
  ROSTER.forEach((f,i)=>{
    for(const [list,who] of [[list1,1],[list2,2]]){
      const b=document.createElement('button');
      b.className='slot';
      b.style.borderColor=f.color;
      if(f.id==='le_binde' || f.id==='mojo_bob') b.style.boxShadow='0 0 10px '+f.color;
      b.innerHTML=`<span class="slot-name">${f.name}</span><span class="slot-stats">${f.maxHP} HP · ${f.moveSpeed} Tempo</span>`;
      b.addEventListener('click',()=>{
        A.playClick();
        if(who===1) state.p1=i; else state.p2=i;
        refreshSelect();
      });
      list.appendChild(b);
    }
  });
  refreshSelect();
}

function refreshSelect(){
  const f1=ROSTER[state.p1], f2=ROSTER[state.p2];
  const l1=$('#label-p1'), l2=$('#label-p2'), p1=$('#passive-p1'), p2=$('#passive-p2');
  if(l1){ l1.textContent=`SPIELER 1 — ${f1.name}`; l1.style.color=f1.color; }
  if(l2){ l2.textContent=`${state.p2IsAI?'KI':'SPIELER 2'} — ${f2.name}`; l2.style.color=f2.color; }
  if(p1) p1.textContent=f1.passive;
  if(p2) p2.textContent=f2.passive;
  const bm=$('#btn-mode'), ba=$('#btn-ai'), br=$('#btn-rounds');
  if(bm) bm.textContent=state.p2IsAI?'Gegner: Computer':'Gegner: Spieler 2 (Le Binde vs Mojo Bob)';
  if(ba){ ba.textContent=state.p2IsAI?`KI-Stufe: ${AI_LEVELS[state.aiLevel].name}`:'KI-Stufe: —'; ba.disabled=!state.p2IsAI; }
  if(br) br.textContent=ROUND_OPTIONS[state.roundsIndex]===1?'Runden: eine':`Runden: Best of ${ROUND_OPTIONS[state.roundsIndex]}`;

  document.querySelectorAll('#list-p1 .slot').forEach((el,i)=>el.classList.toggle('sel', i===state.p1));
  document.querySelectorAll('#list-p2 .slot').forEach((el,i)=>el.classList.toggle('sel', i===state.p2));

  // Specials anzeigen
  const s1=$('#specials-p1'), s2=$('#specials-p2');
  if(s1){
    s1.innerHTML = f1.specials.map(s=>`<div class="spec"><b>${s.name}</b> ${s.damage>0?`· ${s.damage} DMG`:''} · ${s.cooldown}s<br><small>${s.desc||s.effect}</small></div>`).join('');
  }
  if(s2){
    s2.innerHTML = f2.specials.map(s=>`<div class="spec"><b>${s.name}</b> ${s.damage>0?`· ${s.damage} DMG`:''} · ${s.cooldown}s<br><small>${s.desc||s.effect}</small></div>`).join('');
  }
}

function startMatch(training=false){
  A.initAudio();
  A.resumeAudio();
  const canvas = $('#stage3d');
  if(!renderer){
    renderer = new Renderer3D(canvas);
  }

  state.match = new Match({
    p1: ROSTER[state.p1].id,
    p2: ROSTER[state.p2].id,
    p2IsAI: training ? true : state.p2IsAI,
    aiLevel: training ? 0 : state.aiLevel,
    bestOf: training ? 1 : ROUND_OPTIONS[state.roundsIndex],
    roundTime: training ? 999 : K.defaultRoundTime,
  });

  // Fighter Meshes initialisieren
  const st1 = STATURE[ROSTER[state.p1].stature] || STATURE.normal;
  const st2 = STATURE[ROSTER[state.p2].stature] || STATURE.normal;
  renderer.initFighters(ROSTER[state.p1], ROSTER[state.p2], st1, st2);

  state.training = training;
  state.running = true;
  state.last = performance.now();
  state.time = 0;
  beat.reset();
  beat.start();
  showScreen('screen-fight');
  $('#result').classList.add('hidden');
  requestAnimationFrame(loop);
}

function loop(now){
  if(!state.running) return;
  const dt = Math.min(0.1, (now - state.last)/1000);
  state.last = now;
  state.time += dt;

  if(renderer.hitStop<=0){
    state.accumulator += dt;
    let guard=0;
    while(state.accumulator>=STEP && guard++<8){
      const events = state.match.step({p1: input.readP1(), p2: input.readP2()});
      handleEvents(events);
      input.endFrame();
      state.accumulator-=STEP;
    }
  } else {
    renderer.hitStop-=dt;
  }

  if(state.training){
    state.match.p1.hp = state.match.p1.maxHP;
    state.match.p2.hp = state.match.p2.maxHP;
  }

  beat.tick(dt);
  renderer.draw(state.match, dt, state.time);
  drawHud();
  requestAnimationFrame(loop);
}

function handleEvents(events){
  for(const e of events){
    if(e.type==='hit'){
      const heavy = e.kind!=='light';
      if(e.blocked) A.playBlock();
      else { A.playHit(heavy, e.crit); A.playHurt(); }
      renderer.handleEvent(e, state.match);
    }
    if(e.type==='projectile_hit'){
      A.playHit(false,false);
      renderer.handleEvent(e, state.match);
    }
    if(e.type==='trash_yard'){
      renderer.handleEvent(e, state.match);
      A.playHit(true,false);
    }
    if(e.type==='ko'){
      renderer.handleEvent(e, state.match);
    }
    if(e.type==='roundEnd'){
      beat.reset();
      renderer.arena.reset();
    }
    if(e.type==='matchEnd'){
      A.playVictory();
      beat.stop();
      showResult(e.winner);
    }
    if(e.type==='roll'){
      A.playClick();
    }
    if(e.type==='special'){
      A.playClick();
      // Spezialeffekte Sound
      if(e.f && e.f.cfg.specials[e.slot]){
        const sp = e.f.cfg.specials[e.slot];
        if(sp.key==='mops' || sp.key==='dingeneldang') A.playVictory();
      }
    }
  }
}

function drawHud(){
  const m=state.match;
  const set=(sel,value)=>{ const el=$(sel); if(el) el.style.width=`${Math.max(0,value)*100}%`; };
  set('#hp1-fill', m.p1.hp/m.p1.maxHP);
  set('#hp2-fill', m.p2.hp/m.p2.maxHP);
  set('#fb1-fill', m.p1.meter/K.fatalBlowMeterMax);
  set('#fb2-fill', m.p2.meter/K.fatalBlowMeterMax);

  $('#hp1-fill').style.background = healthColor(m.p1.hp/m.p1.maxHP);
  $('#hp2-fill').style.background = healthColor(m.p2.hp/m.p2.maxHP);

  $('#name1').textContent=`${m.p1.cfg.name} ${'●'.repeat(m.wins[0])} ${m.p1.mojoPoints?`Mojo:${m.p1.mojoPoints}`:''} ${m.p1.greaseActive?`Fett:${m.p1.greaseCharges}`:''}`;
  $('#name2').textContent=`${'●'.repeat(m.wins[1])} ${m.p2.cfg.name} ${m.p2.mojoPoints?`Mojo:${m.p2.mojoPoints}`:''}`;
  $('#timer').textContent=Math.ceil(Math.max(0,m.timer));
  $('#round').textContent=state.training?'TRAINING':`Runde ${m.round} / ${m.bestOf}`;
  $('#med1').textContent='💊'.repeat(m.p1.med);
  $('#med2').textContent='💊'.repeat(m.p2.med);

  // Buffs anzeigen
  const buff1=$('#buff1'), buff2=$('#buff2');
  if(buff1){
    let txt=[];
    if(m.p1.reifTimer>0) txt.push(`REIF! ${m.p1.reifTimer.toFixed(1)}s`);
    if(m.p1.mopsCritTimer>0) txt.push(`MOPS +50% Krit ${m.p1.mopsCritTimer.toFixed(1)}s`);
    if(m.p1.mojoBuffTimer>0) txt.push(`DINGELDANG 100% Krit ${m.p1.mojoBuffTimer.toFixed(1)}s`);
    if(m.p1.mojoDebuffTimer>0) txt.push(`Reue -30% ${m.p1.mojoDebuffTimer.toFixed(1)}s`);
    if(!m.p1.greaseActive) txt.push(`FETT WEG ${m.p1.greaseBurnTimer.toFixed(1)}s`);
    if(m.p1.effects.bleed) txt.push(`Blutung`);
    if(m.p1.effects.burn) txt.push(`Brennt`);
    buff1.textContent=txt.join(' | ');
  }
  if(buff2){
    let txt=[];
    if(m.p2.reifTimer>0) txt.push(`REIF! ${m.p2.reifTimer.toFixed(1)}s`);
    if(m.p2.mopsCritTimer>0) txt.push(`MOPS +50% Krit ${m.p2.mopsCritTimer.toFixed(1)}s`);
    if(m.p2.mojoBuffTimer>0) txt.push(`DINGELDANG 100% Krit ${m.p2.mojoBuffTimer.toFixed(1)}s`);
    if(m.p2.mojoDebuffTimer>0) txt.push(`Reue -30% ${m.p2.mojoDebuffTimer.toFixed(1)}s`);
    if(m.p2.effects.bleed) txt.push(`Blutung`);
    if(m.p2.effects.burn) txt.push(`Brennt`);
    if(m.p2.effects.slow) txt.push(`Slow`);
    if(m.p2.effects.invert) txt.push(`Invertiert`);
    buff2.textContent=txt.join(' | ');
  }
}

function healthColor(t){
  if(t<0.35) return `linear-gradient(90deg, ${PALETTE.bloodRed}, ${PALETTE.warmOrange})`;
  return `linear-gradient(90deg, ${PALETTE.warmOrange}, ${PALETTE.gold})`;
}

function showResult(winner){
  const box=$('#result');
  box.classList.remove('hidden');
  $('#result-text').textContent=winner?`${winner.cfg.name} gewinnt!`:'Unentschieden';
  $('#result-text').style.color=winner?winner.cfg.color:PALETTE.white;
}

function wire(){
  const btnVersus=$('#btn-versus');
  if(btnVersus) btnVersus.addEventListener('click',()=>{ A.initAudio(); A.playClick(); showScreen('screen-select'); });
  const btnTraining=$('#btn-training');
  if(btnTraining) btnTraining.addEventListener('click',()=>{ A.playClick(); startMatch(true); });
  const btnHelp=$('#btn-help');
  if(btnHelp) btnHelp.addEventListener('click',()=>{ A.playClick(); showScreen('screen-help'); });
  for(const el of document.querySelectorAll('[data-back]')){
    el.addEventListener('click',()=>{ A.playClick(); showScreen('screen-main'); });
  }
  const btnMode=$('#btn-mode');
  if(btnMode) btnMode.addEventListener('click',()=>{ A.playClick(); state.p2IsAI=!state.p2IsAI; refreshSelect(); });
  const btnAi=$('#btn-ai');
  if(btnAi) btnAi.addEventListener('click',()=>{ A.playClick(); state.aiLevel=(state.aiLevel+1)%AI_LEVELS.length; refreshSelect(); });
  const btnRounds=$('#btn-rounds');
  if(btnRounds) btnRounds.addEventListener('click',()=>{ A.playClick(); state.roundsIndex=(state.roundsIndex+1)%ROUND_OPTIONS.length; refreshSelect(); });
  const btnFight=$('#btn-fight');
  if(btnFight) btnFight.addEventListener('click',()=>{ A.playClick(); startMatch(false); });
  const btnRematch=$('#btn-rematch');
  if(btnRematch) btnRematch.addEventListener('click',()=>{ A.playClick(); startMatch(state.training); });
  const btnMenu=$('#btn-menu');
  if(btnMenu) btnMenu.addEventListener('click',()=>{
    A.playClick();
    state.running=false;
    beat.stop();
    showScreen('screen-main');
  });
  const btnPause=$('#btn-pause');
  if(btnPause) btnPause.addEventListener('click',()=>{
    state.running=false;
    beat.stop();
    showScreen('screen-main');
  });
  window.addEventListener('keydown',(e)=>{
    if(e.code==='Escape' || e.code==='F1'){
      state.running=false;
      beat.stop();
      showScreen('screen-main');
    }
  });
  if(isTouch){
    buildTouchControls3D(document.body, input, ()=>A.resumeAudio());
    document.body.classList.add('touch');
  }
}

buildSelect();
wire();
showScreen('screen-main');
