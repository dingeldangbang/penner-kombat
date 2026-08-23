# vendor/ — mitgelieferte Fremdbibliotheken

Damit die App **ohne CDN und offline** läuft, liegt three.js hier lokal im Repo.

| Paket | Version | Dateien | Lizenz |
|---|---|---|---|
| [three.js](https://threejs.org) | 0.160.0 | `three/three.module.min.js`, `three/addons/loaders/GLTFLoader.js`, `three/addons/utils/BufferGeometryUtils.js` | MIT (siehe `three/LICENSE`) |

Aktualisieren:

```bash
cd web
npm install three@<version>
cp node_modules/three/build/three.module.min.js vendor/three/three.module.min.js
cp node_modules/three/examples/jsm/loaders/GLTFLoader.js vendor/three/addons/loaders/
cp node_modules/three/examples/jsm/utils/BufferGeometryUtils.js vendor/three/addons/utils/
cp node_modules/three/LICENSE vendor/three/LICENSE
```

Danach die Version in `web/sw.js` (`CACHE`) hochzählen, damit Clients die neuen
Dateien ziehen.
