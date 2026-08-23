'use strict';

/**
 * ai-proxy.js — Statischer Webserver + optionaler LLM-Proxy für die KI-Werkstatt
 * ==============================================================================
 *
 * Zwei Aufgaben:
 *
 *   1. Liefert das Verzeichnis web/ aus (damit lab.html, 3d.html und index.html
 *      ohne Build-Schritt laufen).
 *   2. Nimmt POST /api/ai/config entgegen und reicht es an einen
 *      OpenAI-kompatiblen /chat/completions-Endpunkt weiter. Der API-Key bleibt
 *      dabei serverseitig — er landet nie im Browser-Bundle.
 *
 * Ohne gesetzten Key antwortet der Proxy mit 503; der Client fällt dann
 * automatisch auf den lokalen, regelbasierten Parser zurück.
 *
 * Start:
 *   node server/src/ai-proxy.js
 *   PORT=8080 OPENAI_API_KEY=sk-… node server/src/ai-proxy.js
 *   OPENAI_BASE_URL=https://openrouter.ai/api/v1/chat/completions … (kompatibel)
 */

const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = Number(process.env.PORT || 8080);
const HOST = process.env.HOST || '0.0.0.0';
const WEB_ROOT = path.resolve(process.env.WEB_ROOT || path.join(__dirname, '..', '..', 'web'));
const API_KEY = process.env.OPENAI_API_KEY || process.env.LLM_API_KEY || '';
const BASE_URL = process.env.OPENAI_BASE_URL || 'https://api.openai.com/v1/chat/completions';
const DEFAULT_MODEL = process.env.LLM_MODEL || 'gpt-4o-mini';
const MAX_BODY = 256 * 1024;

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.webmanifest': 'application/manifest+json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.glb': 'model/gltf-binary',
  '.gltf': 'model/gltf+json',
  '.wasm': 'application/wasm',
  '.ico': 'image/x-icon',
};

const log = (...a) => console.log(new Date().toISOString(), ...a);

function json(res, code, obj) {
  const body = JSON.stringify(obj);
  res.writeHead(code, { 'Content-Type': 'application/json; charset=utf-8', 'Content-Length': Buffer.byteLength(body) });
  res.end(body);
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    let size = 0;
    const chunks = [];
    req.on('data', (c) => {
      size += c.length;
      if (size > MAX_BODY) { reject(new Error('Body zu groß')); req.destroy(); return; }
      chunks.push(c);
    });
    req.on('end', () => resolve(Buffer.concat(chunks).toString('utf8')));
    req.on('error', reject);
  });
}

// ---------------------------------------------------------------------------
//  LLM-Proxy
// ---------------------------------------------------------------------------

async function handleAi(req, res) {
  if (req.method !== 'POST') return json(res, 405, { error: 'POST erwartet' });

  if (!API_KEY) {
    return json(res, 503, {
      error: 'no_api_key',
      message: 'Kein OPENAI_API_KEY gesetzt — der Client nutzt den lokalen Parser.',
    });
  }

  let payload;
  try {
    payload = JSON.parse(await readBody(req));
  } catch (e) {
    return json(res, 400, { error: 'bad_json', message: e.message });
  }
  if (!Array.isArray(payload.messages) || !payload.messages.length) {
    return json(res, 400, { error: 'bad_request', message: 'messages[] fehlt' });
  }

  const body = {
    model: payload.model || DEFAULT_MODEL,
    temperature: typeof payload.temperature === 'number' ? payload.temperature : 0.2,
    response_format: { type: 'json_object' },
    messages: payload.messages.slice(-12),
  };

  try {
    const upstream = await fetch(BASE_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${API_KEY}` },
      body: JSON.stringify(body),
      signal: AbortSignal.timeout(30000),
    });
    const text = await upstream.text();
    res.writeHead(upstream.status, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(text);
    log(`ai ${upstream.status} model=${body.model}`);
  } catch (e) {
    log('ai upstream error', e.message);
    json(res, 502, { error: 'upstream_failed', message: e.message });
  }
}

// ---------------------------------------------------------------------------
//  Statisches Ausliefern
// ---------------------------------------------------------------------------

function serveStatic(req, res, urlPath) {
  let rel = decodeURIComponent(urlPath.split('?')[0]);
  // Startseite der App; fällt auf die Werkstatt zurück, wenn nur sie vorhanden ist
  if (rel === '/' || rel === '') {
    rel = fs.existsSync(path.join(WEB_ROOT, 'index.html')) ? '/index.html' : '/lab.html';
  }
  const full = path.resolve(path.join(WEB_ROOT, rel));
  if (!full.startsWith(WEB_ROOT)) { res.writeHead(403); return res.end('Forbidden'); }

  fs.stat(full, (err, st) => {
    if (err || !st.isFile()) { res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' }); return res.end('404 — ' + rel); }
    res.writeHead(200, {
      'Content-Type': MIME[path.extname(full).toLowerCase()] || 'application/octet-stream',
      'Content-Length': st.size,
      'Cache-Control': 'no-cache',
    });
    fs.createReadStream(full).pipe(res);
  });
}

// ---------------------------------------------------------------------------

const server = http.createServer((req, res) => {
  // Preview-/Cross-Origin-freundlich (kein iframe-Blocking)
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');
  if (req.method === 'OPTIONS') { res.writeHead(204); return res.end(); }

  const url = req.url || '/';
  if (url.startsWith('/api/ai/health')) {
    return json(res, 200, { ok: true, remoteEnabled: !!API_KEY, model: DEFAULT_MODEL, webRoot: WEB_ROOT });
  }
  if (url.startsWith('/api/ai/config')) return handleAi(req, res);
  return serveStatic(req, res, url);
});

if (require.main === module) {
  server.listen(PORT, HOST, () => {
    log(`Penner Kombat App läuft auf http://${HOST}:${PORT}/`);
    log(`  Startseite/2D: /index.html · 3D-Arena: /3d.html · KI-Werkstatt: /lab.html`);
    log(`  Ausgeliefert aus: ${WEB_ROOT}`);
    log(`Remote-LLM: ${API_KEY ? 'aktiv (' + DEFAULT_MODEL + ')' : 'aus — lokaler Parser im Browser'}`);
  });
}

module.exports = { server, WEB_ROOT };
