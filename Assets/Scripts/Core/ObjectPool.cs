using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Einfacher GameObject-Pool zur Vermeidung von Garbage-Collection-Peaks
    /// bei häufig instanziierten Effekten (Projektile, Partikel, Blutspritzer).
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance;

        private readonly Dictionary<GameObject, Stack<GameObject>> pools =
            new Dictionary<GameObject, Stack<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> activeToPrefab =
            new Dictionary<GameObject, GameObject>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        /// <summary>Holt ein Objekt (erstellt bei Bedarf ein neues).</summary>
        public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            if (prefab == null) return null;

            if (!pools.TryGetValue(prefab, out var stack)) { stack = new Stack<GameObject>(); pools[prefab] = stack; }

            GameObject go = stack.Count > 0 ? stack.Pop() : CreateNew(prefab);
            if (go == null) return null;

            activeToPrefab[go] = prefab;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.SetActive(true);
            return go;
        }

        /// <summary>Gibt ein Objekt zurück (bzw. zerstört es, wenn es nicht gepoolt wurde).</summary>
        public void Despawn(GameObject go)
        {
            if (go == null) return;
            if (!activeToPrefab.TryGetValue(go, out var prefab))
            {
                Destroy(go);
                return;
            }
            activeToPrefab.Remove(go);
            go.SetActive(false);
            if (!pools.TryGetValue(prefab, out var stack)) { stack = new Stack<GameObject>(); pools[prefab] = stack; }
            stack.Push(go);
        }

        private GameObject CreateNew(GameObject prefab)
        {
            GameObject go = Instantiate(prefab);
            go.name = prefab.name + "(pooled)";
            go.SetActive(false);
            return go;
        }
    }
}
