/**
 * app.test.mjs — Auslieferungs-Tests der Web-App
 *
 * Prüft die Eigenschaften, die eine „fertige App" von losen HTML-Dateien
 * trennen: keine CDN-Abhängigkeit, vollständige Offline-Liste, gültiges
 * Manifest, eingebundene App-Shell, vorhandene Icons.
 */

import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const WEB = path.join(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (p) => fs.readFileSync(path.join(WEB, p), 'utf8');
const exists = (p) => fs.existsSync(path.join(WEB, p));

const PAGES = ['index.html', '3d.html', 'lab.html', 'offline.html'];

test('App: alle Seiten und die Offline-Karte existieren', () => {
  for (const p of PAGES) assert.ok(exists(p), p + ' fehlt');
});

test('App: keine CDN-Referenzen — die App muss offline laufen', () => {
  const offenders = [];
  const walk = (dir) => {
    for (const entry of fs.readdirSync(path.join(WEB, dir), { withFileTypes: true })) {
      const rel = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        if (['node_modules', 'test', 'vendor'].includes(entry.name)) continue;
        walk(rel);
      } else if (/\.(html|js|css)$/.test(entry.name)) {
        const src = read(rel);
        if (/https?:\/\/(unpkg\.com|cdn\.|cdnjs|jsdelivr)/.test(src)) offenders.push(rel);
      }
    }
  };
  walk('.');
  assert.deepEqual(offenders, [], 'CDN-Referenzen gefunden: ' + offenders.join(', '));
});

test('App: three.js liegt lokal und vollständig im Repo', () => {
  const files = [
    'vendor/three/three.module.min.js',
    'vendor/three/addons/loaders/GLTFLoader.js',
    'vendor/three/addons/utils/BufferGeometryUtils.js',
    'vendor/three/LICENSE',
  ];
  for (const f of files) assert.ok(exists(f), f + ' fehlt');
  assert.ok(fs.statSync(path.join(WEB, 'vendor/three/three.module.min.js')).size > 100000, 'three-Build sieht leer aus');
  // GLTFLoader darf nur auf 'three' und lokale Pfade zeigen
  // (nur echte Import-Anweisungen prüfen, keine URLs aus Kommentaren)
  const gltf = read('vendor/three/addons/loaders/GLTFLoader.js');
  const imports = gltf.split('\n')
    .filter((l) => /^(import\s|}\s*from\s)/.test(l.trim()))
    .map((l) => (l.match(/from\s+'([^']+)'/) || [])[1])
    .filter(Boolean);
  assert.ok(imports.length >= 2, 'Import-Analyse fehlgeschlagen');
  for (const i of imports) {
    assert.ok(i === 'three' || i.startsWith('.'), 'unerwarteter Import in GLTFLoader: ' + i);
  }
});

test('App: 3D-Seiten mappen three auf das lokale Vendor-Verzeichnis', () => {
  for (const page of ['lab.html', '3d.html']) {
    const html = read(page);
    const map = html.match(/<script type="importmap">([\s\S]*?)<\/script>/);
    assert.ok(map, page + ' hat keine Import-Map');
    const json = JSON.parse(map[1]);
    assert.match(json.imports.three, /^\.\/vendor\/three\//, page + ': three nicht lokal');
    assert.match(json.imports['three/addons/'], /^\.\/vendor\/three\/addons\//, page + ': addons nicht lokal');
  }
});

test('App: Service Worker cached jede gelistete Datei (und sie existiert)', () => {
  const sw = read('sw.js');
  const list = sw.slice(sw.indexOf('const FILES = ['), sw.indexOf('];', sw.indexOf('const FILES = [')));
  const files = [...list.matchAll(/'\.\/([^']*)'/g)].map((m) => m[1]).filter(Boolean);
  assert.ok(files.length >= 30, 'Cache-Liste verdächtig kurz: ' + files.length);
  const missing = files.filter((f) => !exists(f));
  assert.deepEqual(missing, [], 'im SW gelistet, aber nicht vorhanden: ' + missing.join(', '));

  // Umgekehrt: jede Lab-Quelldatei muss auch gecacht sein
  const labFiles = fs.readdirSync(path.join(WEB, 'src/lab')).filter((f) => /\.(js|css)$/.test(f));
  const uncached = labFiles.filter((f) => !files.includes('src/lab/' + f));
  assert.deepEqual(uncached, [], 'nicht im Offline-Cache: ' + uncached.join(', '));
});

test('App: Service Worker versioniert und schont die KI-API', () => {
  const sw = read('sw.js');
  assert.match(sw, /const VERSION = '(v\d+)'/, 'keine Cache-Version');
  assert.ok(sw.includes("url.pathname.includes('/api/')"), '/api/ darf nicht gecacht werden');
  assert.ok(sw.includes('offline.html'), 'kein Offline-Fallback');
  assert.ok(sw.includes('skipWaiting'), 'kein Update-Pfad');
});

test('App: Manifest ist installierbar konfiguriert', () => {
  const m = JSON.parse(read('manifest.webmanifest'));
  for (const key of ['id', 'name', 'short_name', 'start_url', 'scope', 'display', 'icons', 'shortcuts']) {
    assert.ok(m[key], 'Manifest ohne ' + key);
  }
  assert.ok(m.icons.some((i) => /maskable/.test(i.purpose || '')), 'kein maskable Icon');
  for (const i of m.icons) assert.ok(exists(i.src), 'Icon fehlt: ' + i.src);
  assert.equal(m.shortcuts.length, 3);
  for (const s of m.shortcuts) {
    const target = s.url.replace(/^\.\//, '');
    assert.ok(exists(target), 'Shortcut zeigt ins Leere: ' + s.url);
  }
  assert.equal(m.start_url.replace(/^\.\//, ''), 'index.html');
});

test('App: App-Shell ist auf allen Seiten eingebunden', () => {
  for (const page of ['index.html', '3d.html', 'lab.html']) {
    assert.ok(read(page).includes('src/app.js'), page + ' bindet app.js nicht ein');
  }
  const app = read('src/app.js');
  assert.ok(app.includes("navigator.serviceWorker.register('sw.js')"), 'keine SW-Registrierung');
  assert.ok(app.includes('beforeinstallprompt'), 'kein Installations-Prompt');
  assert.ok(app.includes('checkWebGL'), 'kein WebGL-Check');
  assert.match(app, /APP_VERSION = '\d+\.\d+\.\d+'/, 'keine Version');
});

test('App: doppelte SW-Registrierung ausgeschlossen', () => {
  // Früher registrierte index.html den SW selbst — das macht jetzt nur app.js
  const inline = read('index.html').match(/serviceWorker\.register/g) || [];
  assert.equal(inline.length, 0, 'index.html registriert den SW zusätzlich inline');
});

test('App: Werkstatt hat Installationsknopf und Versionsanzeige', () => {
  const lab = read('lab.html');
  assert.ok(lab.includes('id="btn-install"'), 'kein Installationsknopf');
  assert.ok(lab.includes('data-app-version'), 'keine Versionsanzeige');
});

test('App: Build-Skript ist ausführbar und prüft die harten Kriterien', () => {
  const script = path.join(WEB, '..', 'Tools', 'build_web_app.sh');
  assert.ok(fs.existsSync(script), 'Build-Skript fehlt');
  assert.ok(fs.statSync(script).mode & 0o111, 'Build-Skript ist nicht ausführbar');
  const src = fs.readFileSync(script, 'utf8');
  for (const needle of ['CDN-Referenzen', 'Service-Worker-Dateien', 'manifest.webmanifest', 'JavaScript-Syntax']) {
    assert.ok(src.includes(needle), 'Build-Prüfung fehlt: ' + needle);
  }
});
