/**
 * sw.js — Service Worker der Penner-Kombat-Web-App
 *
 * Strategie:
 *   • App-Shell (HTML, JS, CSS, three.js, Icons) wird bei der Installation
 *     vollständig vorgeladen  ->  die App startet danach komplett offline.
 *   • Navigationsanfragen: Network-first mit Cache-Fallback (frische Version,
 *     wenn online; sonst die gecachte Seite, sonst die Offline-Karte).
 *   • Alles andere: Cache-first mit Nachladen im Hintergrund.
 *   • /api/ wird nie gecacht (KI-Proxy).
 *
 * Beim Aktualisieren von Dateien die Versionsnummer hochzählen.
 */

const VERSION = 'v5';
const CACHE = `penner-kombat-${VERSION}`;
const OFFLINE_URL = './offline.html';

const FILES = [
  './',
  './index.html', './3d.html', './lab.html', './offline.html',
  './style.css', './manifest.webmanifest',
  './assets/icon.svg', './assets/icon-maskable.svg', './assets/fighter-profiles.json',

  // 2D-Spiel
  './src/main.js', './src/sim.js', './src/data.js',
  './src/render.js', './src/input.js', './src/audio.js', './src/cover.js',

  // 3D-Spiel
  './src/main3d.js', './src/sim3d.js', './src/pose.js', './src/fighter3d.js',
  './src/arena3d.js', './src/render3d.js', './src/input3d.js',

  // KI-Werkstatt
  './src/lab/lab.css', './src/lab/main.js', './src/lab/res480.js',
  './src/lab/aiEngine.js', './src/lab/schema.js', './src/lab/assetLibrary.js',
  './src/lab/fighter.js', './src/lab/vfx.js', './src/lab/audioPool.js',
  './src/lab/inputBuffer.js', './src/lab/placeholder.js', './src/lab/presets.js',
  './src/lab/cinematic.js', './src/lab/ragdoll.js', './src/lab/stage.js',

  // three.js liegt lokal im Repo — kein CDN nötig
  './vendor/three/three.module.min.js',
  './vendor/three/addons/loaders/GLTFLoader.js',
  './vendor/three/addons/utils/BufferGeometryUtils.js',
];

self.addEventListener('install', (e) => {
  e.waitUntil((async () => {
    const cache = await caches.open(CACHE);
    // Einzeln laden: eine fehlende Datei darf die Installation nicht killen
    await Promise.all(FILES.map((url) => cache.add(url).catch((err) => {
      console.warn('[sw] konnte nicht cachen:', url, err.message);
    })));
    await self.skipWaiting();
  })());
});

self.addEventListener('activate', (e) => {
  e.waitUntil((async () => {
    const keys = await caches.keys();
    await Promise.all(keys.filter((k) => k !== CACHE).map((k) => caches.delete(k)));
    if (self.registration.navigationPreload) {
      await self.registration.navigationPreload.enable().catch(() => {});
    }
    await self.clients.claim();
  })());
});

self.addEventListener('message', (e) => {
  if (e.data === 'skip-waiting') self.skipWaiting();
  if (e.data === 'version') {
    e.source?.postMessage({ type: 'version', version: VERSION, cache: CACHE });
  }
});

self.addEventListener('fetch', (e) => {
  const req = e.request;
  if (req.method !== 'GET') return;

  const url = new URL(req.url);
  if (url.origin !== self.location.origin) return;   // CDN/Fremdhosts durchreichen
  if (url.pathname.includes('/api/')) return;        // KI-Proxy nie cachen

  // Navigation: erst Netz, dann Cache, dann Offline-Karte
  if (req.mode === 'navigate') {
    e.respondWith((async () => {
      try {
        const preload = await e.preloadResponse;
        if (preload) { cachePut(req, preload.clone()); return preload; }
        const net = await fetch(req);
        cachePut(req, net.clone());
        return net;
      } catch {
        return (await caches.match(req))
          || (await caches.match('./index.html'))
          || (await caches.match(OFFLINE_URL))
          || new Response('Offline', { status: 503, headers: { 'Content-Type': 'text/plain' } });
      }
    })());
    return;
  }

  // Rest: Cache-first, im Hintergrund auffrischen
  e.respondWith((async () => {
    const hit = await caches.match(req);
    if (hit) {
      fetch(req).then((res) => cachePut(req, res)).catch(() => {});
      return hit;
    }
    try {
      const net = await fetch(req);
      cachePut(req, net.clone());
      return net;
    } catch {
      return new Response('', { status: 504 });
    }
  })());
});

function cachePut(req, res) {
  if (!res || !res.ok || res.type === 'opaque') return;
  caches.open(CACHE).then((c) => c.put(req, res)).catch(() => {});
}
