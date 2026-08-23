/** gltf.stub.mjs — GLTFLoader-Attrappe für die DOM-Tests (lädt nichts). */
export class GLTFLoader {
  load(url, onLoad, onProgress, onError) {
    if (onError) onError(new Error('GLTFLoader-Stub: kein Netzwerk im Test'));
  }
}
