// Service Worker: macht die Browser-Fassung offline spielbar (inkl. 3D Pose Arena).
const CACHE = 'penner-kombat-v4';
const FILES = [
  './', './index.html', './3d.html', './lab.html', './style.css', './manifest.webmanifest',
  './src/lab/lab.css', './src/lab/main.js', './src/lab/res480.js', './src/lab/aiEngine.js',
  './src/lab/schema.js', './src/lab/assetLibrary.js', './src/lab/fighter.js',
  './src/lab/vfx.js', './src/lab/audioPool.js', './src/lab/inputBuffer.js', './src/lab/placeholder.js',
  './src/lab/presets.js', './src/lab/cinematic.js', './src/lab/ragdoll.js', './src/lab/stage.js',
  './assets/fighter-profiles.json',
  './assets/icon.svg',
  './src/main.js', './src/sim.js', './src/data.js',
  './src/render.js', './src/input.js', './src/audio.js', './src/cover.js',
  './src/main3d.js', './src/sim3d.js', './src/pose.js', './src/fighter3d.js',
  './src/arena3d.js', './src/render3d.js', './src/input3d.js',
];

self.addEventListener('install', (e) => {
  e.waitUntil(caches.open(CACHE).then((c) => c.addAll(FILES)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', (e) => {
  e.waitUntil(caches.keys().then((keys) =>
    Promise.all(keys.filter((k) => k !== CACHE).map((k) => caches.delete(k)))).then(() => self.clients.claim()));
});

self.addEventListener('fetch', (e) => {
  if (e.request.method !== 'GET') return;
  e.respondWith(
    caches.match(e.request).then((hit) => hit || fetch(e.request).then((res) => {
      const copy = res.clone();
      caches.open(CACHE).then((c) => c.put(e.request, copy)).catch(() => {});
      return res;
    }).catch(() => caches.match('./index.html')))
  );
});
