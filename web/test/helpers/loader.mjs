/**
 * loader.mjs — ESM-Resolve-Hook für die DOM-Tests
 *
 * Biegt `three` auf das Test-Doppel und `three/addons/loaders/GLTFLoader.js`
 * auf einen Stub um, damit `src/lab/main.js` ohne WebGL und ohne CDN läuft.
 *
 * Verwendung:  node --import ./test/helpers/register.mjs --test test/ui.dom.test.mjs
 */

import { pathToFileURL } from 'node:url';
import path from 'node:path';

const here = path.dirname(new URL(import.meta.url).pathname);
const THREE_DOUBLE = pathToFileURL(path.join(here, 'three.headless.mjs')).href;
const GLTF_STUB = pathToFileURL(path.join(here, 'gltf.stub.mjs')).href;

export async function resolve(specifier, context, next) {
  if (specifier === 'three' && context.parentURL && context.parentURL.includes('/src/lab/')) {
    return { url: THREE_DOUBLE, shortCircuit: true };
  }
  if (specifier.startsWith('three/addons/loaders/GLTFLoader')) {
    return { url: GLTF_STUB, shortCircuit: true };
  }
  return next(specifier, context);
}
