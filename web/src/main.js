/**
 * main.js — Menü, Charakterauswahl, Spielschleife und HUD.
 * Spiegelt UI/FrontEnd.cs und UI/HudBuilder.cs aus der Unity-Fassung.
 */

import { ROSTER, AI_LEVELS, PALETTE, K, byId } from './data.js';
import { Match, STEP } from './sim.js';
import { Renderer } from './render.js';
import { InputState, buildTouchControls } from './input.js';
import * as A from './audio.js';

const $ = (sel) => document.querySelector(sel);
const isTouch = matchMedia('(hover: none)').matches || 'ontouchstart' in window;

const state = {
  p1: 0,
  p2: 2,
  p2IsAI: true,
  aiLevel: 2,
  roundsIndex: 1,
  match: null,
  running: false,
  accumulator: 0,
  last: 0,
};
const ROUND_OPTIONS = [1, 3, 5];

const input = new InputState();
const renderer = new Renderer($('#stage'));
const beat = new A.BeatLoop();

// ---------------------------------------------------------------- Menü

function showScreen(id) {
  for (const el of document.querySelectorAll('.screen')) el.classList.toggle('hidden', el.id !== id);
  $('#hud').classList.toggle('hidden', id !== 'screen-fight');
  $('#stage').classList.toggle('hidden', id !== 'screen-fight');
  const touchLayer = document.querySelector('.touch-layer');
  if (touchLayer) touchLayer.classList.toggle('hidden', id !== 'screen-fight');
}

function buildSelect() {
  const list1 = $('#list-p1'), list2 = $('#list-p2');
  list1.innerHTML = ''; list2.innerHTML = '';

  ROSTER.forEach((f, i) => {
    for (const [list, who] of [[list1, 1], [list2, 2]]) {
      const b = document.createElement('button');
      b.className = 'slot';
      b.style.borderColor = f.color;
      b.innerHTML = `<span class="slot-name">${f.name}</span>
                     <span class="slot-stats">${f.maxHP} HP · ${f.moveSpeed} Tempo</span>`;
      b.addEventListener('click', () => {
        A.playClick();
        if (who === 1) state.p1 = i; else state.p2 = i;
        refreshSelect();
      });
      list.appendChild(b);
    }
  });
  refreshSelect();
}

function refreshSelect() {
  const f1 = ROSTER[state.p1], f2 = ROSTER[state.p2];
  $('#label-p1').textContent = `SPIELER 1 — ${f1.name}`;
  $('#label-p1').style.color = f1.color;
  $('#label-p2').textContent = `${state.p2IsAI ? 'KI' : 'SPIELER 2'} — ${f2.name}`;
  $('#label-p2').style.color = f2.color;
  $('#passive-p1').textContent = f1.passive;
  $('#passive-p2').textContent = f2.passive;
  $('#btn-mode').textContent = state.p2IsAI ? 'Gegner: Computer' : 'Gegner: Spieler 2 (Tastatur)';
  $('#btn-ai').textContent = state.p2IsAI ? `KI-Stufe: ${AI_LEVELS[state.aiLevel].name}` : 'KI-Stufe: —';
  $('#btn-ai').disabled = !state.p2IsAI;
  $('#btn-rounds').textContent = ROUND_OPTIONS[state.roundsIndex] === 1
    ? 'Runden: eine' : `Runden: Best of ${ROUND_OPTIONS[state.roundsIndex]}`;

  document.querySelectorAll('#list-p1 .slot').forEach((el, i) => el.classList.toggle('sel', i === state.p1));
  document.querySelectorAll('#list-p2 .slot').forEach((el, i) => el.classList.toggle('sel', i === state.p2));
}

// ---------------------------------------------------------------- Kampf

function startMatch(training = false) {
  A.initAudio();
  A.resumeAudio();
  state.match = new Match({
    p1: ROSTER[state.p1].id,
    p2: ROSTER[state.p2].id,
    p2IsAI: training ? true : state.p2IsAI,
    aiLevel: training ? 0 : state.aiLevel,
    bestOf: training ? 1 : ROUND_OPTIONS[state.roundsIndex],
    roundTime: training ? 999 : K.defaultRoundTime,
  });
  state.training = training;
  state.running = true;
  state.last = performance.now();
  beat.reset();
  beat.start();
  showScreen('screen-fight');
  $('#result').classList.add('hidden');
  requestAnimationFrame(loop);
}

function loop(now) {
  if (!state.running) return;
  const dt = Math.min(0.1, (now - state.last) / 1000);
  state.last = now;

  if (renderer.hitStop <= 0) {
    state.accumulator += dt;
    let guard = 0;
    while (state.accumulator >= STEP && guard++ < 8) {
      const events = state.match.step({ p1: input.readP1(), p2: input.readP2() });
      handleEvents(events);
      input.endFrame();
      state.accumulator -= STEP;
    }
  } else {
    renderer.hitStop -= dt;
  }

  if (state.training) {
    state.match.p1.hp = state.match.p1.maxHP;
    state.match.p2.hp = state.match.p2.maxHP;
  }

  beat.tick(dt);
  renderer.draw(state.match, dt);
  drawHud();
  requestAnimationFrame(loop);
}

function handleEvents(events) {
  for (const e of events) {
    if (e.type === 'hit') {
      const heavy = e.kind !== 'light';
      if (e.blocked) A.playBlock();
      else { A.playHit(heavy, e.crit); A.playHurt(); }

      const pos = { x: e.target.x, y: e.target.y + 1, z: e.target.z };
      renderer.burst(pos, e.crit ? PALETTE.gold : e.blocked ? PALETTE.neonBlue : PALETTE.bloodRed,
                     e.blocked ? 5 : heavy ? 16 : 9);
      renderer.floatText(
        e.crit ? `KRIT ${Math.round(e.damage)}` : Math.round(e.damage).toString(),
        pos, e.crit ? PALETTE.gold : e.blocked ? PALETTE.neonBlue : PALETTE.white);
      renderer.shake(heavy ? 9 : 4, 0.12);
      if (e.crit || e.kind === 'fatal') renderer.freeze(0.08);
      if (e.combo >= 2) {
        beat.onCombo(e.combo);
        renderer.floatText(`${e.combo} TREFFER`, { x: e.attacker.x, y: 2.6, z: e.attacker.z },
                           e.combo >= 10 ? '#FF2A2A' : e.combo >= 5 ? PALETTE.gold : PALETTE.white);
      }
    }
    if (e.type === 'ko') {
      renderer.shake(16, 0.35);
      renderer.freeze(0.15);
    }
    if (e.type === 'roundEnd') {
      beat.reset();
    }
    if (e.type === 'matchEnd') {
      A.playVictory();
      beat.stop();
      showResult(e.winner);
    }
  }
}

function drawHud() {
  const m = state.match;
  const set = (sel, value) => { const el = $(sel); if (el) el.style.width = `${Math.max(0, value) * 100}%`; };

  set('#hp1-fill', m.p1.hp / m.p1.maxHP);
  set('#hp2-fill', m.p2.hp / m.p2.maxHP);
  set('#fb1-fill', m.p1.meter / K.fatalBlowMeterMax);
  set('#fb2-fill', m.p2.meter / K.fatalBlowMeterMax);

  $('#hp1-fill').style.background = healthColor(m.p1.hp / m.p1.maxHP);
  $('#hp2-fill').style.background = healthColor(m.p2.hp / m.p2.maxHP);

  $('#name1').textContent = `${m.p1.cfg.name}  ${'●'.repeat(m.wins[0])}`;
  $('#name2').textContent = `${'●'.repeat(m.wins[1])}  ${m.p2.cfg.name}`;
  $('#timer').textContent = Math.ceil(Math.max(0, m.timer));
  $('#round').textContent = state.training ? 'TRAINING' : `Runde ${m.round} / ${m.bestOf}`;
  $('#med1').textContent = '💊'.repeat(m.p1.med);
  $('#med2').textContent = '💊'.repeat(m.p2.med);
}

function healthColor(t) {
  if (t < 0.35) return `linear-gradient(90deg, ${PALETTE.bloodRed}, ${PALETTE.warmOrange})`;
  return `linear-gradient(90deg, ${PALETTE.warmOrange}, ${PALETTE.gold})`;
}

function showResult(winner) {
  const box = $('#result');
  box.classList.remove('hidden');
  $('#result-text').textContent = winner ? `${winner.cfg.name} gewinnt!` : 'Unentschieden';
  $('#result-text').style.color = winner ? winner.cfg.color : PALETTE.white;
}

// ---------------------------------------------------------------- Start

function wire() {
  $('#btn-versus').addEventListener('click', () => { A.initAudio(); A.playClick(); showScreen('screen-select'); });
  $('#btn-training').addEventListener('click', () => { A.playClick(); startMatch(true); });
  $('#btn-help').addEventListener('click', () => { A.playClick(); showScreen('screen-help'); });
  for (const el of document.querySelectorAll('[data-back]')) {
    el.addEventListener('click', () => { A.playClick(); showScreen('screen-main'); });
  }

  $('#btn-mode').addEventListener('click', () => { A.playClick(); state.p2IsAI = !state.p2IsAI; refreshSelect(); });
  $('#btn-ai').addEventListener('click', () => {
    A.playClick(); state.aiLevel = (state.aiLevel + 1) % AI_LEVELS.length; refreshSelect();
  });
  $('#btn-rounds').addEventListener('click', () => {
    A.playClick(); state.roundsIndex = (state.roundsIndex + 1) % ROUND_OPTIONS.length; refreshSelect();
  });
  $('#btn-fight').addEventListener('click', () => { A.playClick(); startMatch(false); });

  $('#btn-rematch').addEventListener('click', () => { A.playClick(); startMatch(state.training); });
  $('#btn-menu').addEventListener('click', () => {
    A.playClick();
    state.running = false;
    beat.stop();
    showScreen('screen-main');
  });
  $('#btn-pause').addEventListener('click', () => {
    state.running = false;
    beat.stop();
    showScreen('screen-main');
  });

  window.addEventListener('keydown', (e) => {
    if (e.code === 'Escape' || e.code === 'F1') {
      state.running = false;
      beat.stop();
      showScreen('screen-main');
    }
  });

  if (isTouch) {
    buildTouchControls(document.body, input, () => A.resumeAudio());
    document.body.classList.add('touch');
  }
}

buildSelect();
wire();
showScreen('screen-main');
