using System.Collections.Generic;
using UnityEngine;
#if PK_GLTFAST
using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
#endif

namespace PennerKombat
{
    /// <summary>
    /// Lädt GLB-/glTF-Modelle zur Laufzeit und hängt sie anstelle des
    /// Kapsel-Platzhalters an einen Kämpfer. Das Modell wird automatisch auf
    /// Kämpfergröße skaliert, auf den Boden gesetzt und nach vorn gedreht;
    /// Feinwerte kommen aus <see cref="GlbLibrary"/>.
    ///
    /// Braucht das Paket <c>com.unity.cloud.gltfast</c> und das Define
    /// <c>PK_GLTFAST</c> (setzt <c>PkProjectSetup</c> automatisch).
    /// Ohne Paket passiert schlicht nichts — die Kapsel bleibt stehen.
    ///
    /// Siehe docs/MODELLE.md
    /// </summary>
    public class GlbModelLoader : MonoBehaviour
    {
        public static GlbModelLoader Instance { get; private set; }

        /// <summary>true, wenn glTFast verfügbar ist.</summary>
        public static bool Available
        {
#if PK_GLTFAST
            get => true;
#else
            get => false;
#endif
        }

        public static GlbModelLoader Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~GlbModelLoader");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<GlbModelLoader>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        /// <summary>
        /// Weist einem Kämpfer sein zugeordnetes Modell zu (falls vorhanden).
        /// Läuft asynchron; der Kampf startet sofort, das Modell schnappt
        /// eine Handvoll Frames später ein.
        /// </summary>
        public static void ApplyTo(FighterController fighter)
        {
            if (fighter == null) return;
            string path = GlbLibrary.GetAssigned(fighter.fighterId);
            if (string.IsNullOrEmpty(path)) return;
            if (!Available)
            {
                Debug.LogWarning("[Penner Kombat] Modell zugewiesen, aber glTFast fehlt. "
                    + "Menü: Tools → Penner Kombat → glTFast (GLB-Import) installieren.");
                return;
            }
            Ensure().Load(fighter, path);
        }

#if PK_GLTFAST
        readonly Dictionary<string, GltfImport> cache = new Dictionary<string, GltfImport>();

        async void Load(FighterController fighter, string path)
        {
            try
            {
                if (!cache.TryGetValue(path, out var gltf) || gltf == null)
                {
                    gltf = new GltfImport();
                    byte[] data = File.ReadAllBytes(path);
                    bool ok = await gltf.Load(data, new Uri(path));
                    if (!ok)
                    {
                        Debug.LogError($"[Penner Kombat] GLB konnte nicht geladen werden: {path}");
                        return;
                    }
                    cache[path] = gltf;
                }

                if (fighter == null) return;   // Kämpfer wurde zwischenzeitlich zerstört

                var holder = new GameObject("Model_GLB");
                holder.transform.SetParent(fighter.transform, false);

                bool instantiated = await gltf.InstantiateMainSceneAsync(holder.transform);
                if (!instantiated || fighter == null)
                {
                    if (holder != null) Destroy(holder);
                    return;
                }

                Normalize(fighter, holder.transform);
                HidePlaceholder(fighter);
                BindAnimator(fighter, holder);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Penner Kombat] Fehler beim GLB-Import ({path}): {e.Message}");
            }
        }
#else
        void Load(FighterController fighter, string path) { }
#endif

        /// <summary>
        /// Skaliert das Modell auf Kämpfergröße, stellt es auf den Boden und
        /// dreht es in Blickrichtung (+Z). Manuelle Werte aus GlbLibrary haben Vorrang.
        /// </summary>
        public static void Normalize(FighterController fighter, Transform model)
        {
            if (fighter == null || model == null) return;
            string id = fighter.fighterId;

            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            model.localScale = Vector3.one;

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            float manualScale = GlbLibrary.GetScale(id);
            float scale = manualScale > 0.0001f
                ? manualScale
                : (bounds.size.y > 0.0001f ? GlbLibrary.TargetHeight / bounds.size.y : 1f);
            model.localScale = Vector3.one * scale;

            // Neu messen, danach Füße auf y = 0 und Mitte auf x/z = 0 schieben
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            Vector3 offset = fighter.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            model.position += offset + Vector3.up * GlbLibrary.GetYOffset(id);
            model.localRotation = Quaternion.Euler(0f, GlbLibrary.GetYaw(id), 0f);
        }

        /// <summary>Kapsel-Platzhalter ausblenden, sobald ein Modell steht.</summary>
        public static void HidePlaceholder(FighterController fighter)
        {
            var placeholder = fighter.transform.Find("Visual");
            if (placeholder != null) placeholder.gameObject.SetActive(false);
        }

        /// <summary>
        /// Verbindet einen im Modell mitgelieferten Animator mit der
        /// Animations-Brücke. Fehlt einer, bleibt alles beim Alten
        /// (<see cref="AnimationsController3D"/> setzt nur vorhandene Parameter).
        /// </summary>
        public static void BindAnimator(FighterController fighter, GameObject model)
        {
            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null) return;
            if (fighter.GetComponent<Animator>() == null && animator.runtimeAnimatorController != null)
                Debug.Log($"[Penner Kombat] Animator aus GLB gefunden für {fighter.displayName} "
                        + "— Controller in den Charakter übernehmen, wenn Clips passen.");
        }

        /// <summary>Alle aktuell laufenden Kämpfer neu bestücken (nach Menüwechsel).</summary>
        public static void RefreshAll()
        {
            foreach (var fighter in FindObjectsOfType<FighterController>())
            {
                var old = fighter.transform.Find("Model_GLB");
                if (old != null) Destroy(old.gameObject);
                var placeholder = fighter.transform.Find("Visual");
                if (placeholder != null) placeholder.gameObject.SetActive(true);
                ApplyTo(fighter);
            }
        }
    }
}
