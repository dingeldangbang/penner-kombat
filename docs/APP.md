# 📦 Die fertige App — installieren, hosten, ausliefern

> Betrifft die **Web-App in `web/`**: 2D-Arena, 3D-Pose-Arena und die
> KI-Werkstatt in einem installierbaren Paket. Kein Build-Tool, kein Bundler,
> keine CDN-Abhängigkeit — reines ES-Modul-HTML.

**Version 1.1.0** · 46 Dateien · 1,3 MB entpackt / 324 KB als ZIP · 81 Tests grün

---

## 1 · Sofort starten (lokal)

```bash
# Variante A — nur Dateien ausliefern (KI läuft mit dem lokalen Parser)
cd web && python3 -m http.server 8000        # → http://localhost:8000/index.html

# Variante B — mit KI-Proxy (optionales echtes LLM)
node server/src/ai-proxy.js                  # → http://localhost:8080/lab.html
PORT=8080 OPENAI_API_KEY=sk-… node server/src/ai-proxy.js
```

| Seite | Inhalt |
|---|---|
| `index.html` | Startseite + 2D-Arena mit allen 9 Kämpfern |
| `3d.html` | 3D-Pose-Arena (Le Binde vs Mojo Bob) |
| `lab.html` | **KI-Werkstatt**: `.glb` hochladen, Moves per Chat, X-Ray/Fatality/Arena |
| `offline.html` | Fallback, falls eine Seite noch nicht im Cache liegt |

## 2 · Als App installieren (PWA)

Beim ersten Besuch registriert sich der Service Worker und lädt die komplette
App-Shell (inkl. three.js) in den Cache. Danach läuft **alles offline**.

* **Desktop (Chrome/Edge)**: Knopf „📲 Als App installieren" auf der Startseite
  bzw. „📲 App installieren" in der Werkstatt-Topbar — sonst Adressleiste → Installieren.
* **Android**: Browsermenü → „App installieren" / „Zum Startbildschirm".
* **iOS/Safari**: Teilen → „Zum Home-Bildschirm".

Die App startet im Vollbild-Querformat und bringt drei **Shortcuts** mit
(Werkstatt, 3D-Arena, 2D-Arena) — auf Android per Longpress aufs Icon.

Erscheint eine neue Version, meldet sich unten eine Leiste
(„Neue Version geladen — jetzt neu starten").

## 3 · Paket bauen

```bash
Tools/build_web_app.sh              # schnell
Tools/build_web_app.sh --with-tests # inkl. aller 81 Tests
```

Das Skript **verweigert den Build**, wenn eine Auslieferungsbedingung verletzt ist:

1. keine CDN-Referenzen (die App muss offline laufen),
2. jede Datei aus der Service-Worker-Liste existiert,
3. `manifest.webmanifest` ist gültig und vollständig,
4. jede `.js`-Datei ist syntaktisch fehlerfrei.

Ergebnis:

```
release/penner-kombat-web/            → auf jeden Static-Host kopieren
release/penner-kombat-web-1.1.0.zip   → fertiges Paket
release/BUILD_INFO.txt                → Version, Commit, Datum, Dateiliste
```

## 4 · Hosten

### GitHub Pages (kostenlos, empfohlen)

```bash
cp docs/ci/pages-deploy.yml .github/workflows/pages.yml
git add .github/workflows/pages.yml && git commit -m "Pages-Deploy" && git push
```

Dann **Settings → Pages → Source: GitHub Actions**. Der Workflow läuft erst die
Tests, baut danach mit `Tools/build_web_app.sh` und veröffentlicht
`release/penner-kombat-web`.

### Jeder andere Static-Host

Netlify, Vercel, Cloudflare Pages, S3, nginx, Apache, ein USB-Stick im
lokalen Netz — es reicht, den Ordnerinhalt auszuliefern. Zwei Punkte:

* **HTTPS** (oder `localhost`) ist Pflicht, sonst kein Service Worker → keine
  Installation und kein Offline-Betrieb.
* Korrekte MIME-Typen für `.js` (`text/javascript`) und `.webmanifest`
  (`application/manifest+json`). `server/src/ai-proxy.js` macht das bereits richtig.

### Mit echtem LLM

`server/src/ai-proxy.js` liefert die App aus **und** proxyt `POST /api/ai/config`
an einen OpenAI-kompatiblen Endpunkt — der Key bleibt serverseitig:

```bash
PORT=8080 \
OPENAI_API_KEY=sk-… \
OPENAI_BASE_URL=https://api.openai.com/v1/chat/completions \
LLM_MODEL=gpt-4o-mini \
WEB_ROOT=release/penner-kombat-web \
node server/src/ai-proxy.js
```

In der Werkstatt dann links auf **„Remote LLM"** umstellen. Ohne Key antwortet
der Proxy mit `503` und der Client bleibt still beim lokalen Parser — die App
funktioniert also in jedem Fall.

## 5 · Als Android-App

Capacitor ist im Projekt bereits vorbereitet (`web/package.json`):

```bash
cd web
npm install
npm run cap:init && npm run cap:add && npm run cap:sync && npm run cap:open
```

Details und Signierung: [BUILD_ANDROID.md](BUILD_ANDROID.md) und [HANDY.md](HANDY.md).

## 6 · Was drin steckt

```
web/
├── index.html · 3d.html · lab.html · offline.html
├── manifest.webmanifest · sw.js          PWA: installierbar + offline
├── style.css
├── assets/     Icons (normal + maskable), fighter-profiles.json
├── src/        app.js (App-Shell) · 2D-Spiel · 3D-Spiel
│   └── lab/    KI-Werkstatt: res480, aiEngine, schema, fighter,
│               presets, cinematic, ragdoll, stage, vfx, audioPool …
└── vendor/     three.js 0.160.0 lokal (MIT) — deshalb kein CDN nötig
```

## 7 · Grenzen

* Der Service Worker greift nur über HTTPS/localhost — beim Öffnen per
  `file://` läuft die App, aber ohne Offline-Cache und ohne Installation.
* Hochgeladene `.glb`-Modelle bleiben im Browser (Blob-URL) und werden **nicht**
  mitgespeichert; nach dem Neuladen muss die Datei erneut gewählt werden.
  Move-Konfigurationen überleben dagegen im `localStorage`.
* Auf reinem Static-Hosting gibt es keinen LLM-Proxy — der Chat nutzt dort den
  lokalen Regel-Parser (der kann alles, was in [AI_LAB.md](AI_LAB.md) steht).
