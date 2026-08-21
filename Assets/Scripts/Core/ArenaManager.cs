using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Verwaltet die interaktiven Objekte einer Arena: umkippbare Props,
    /// explodierende Objekte, den Mops-Napf sowie die Stage-Fatality.
    /// Wird von LeBindes "Mops-Kommando" (KippAlle) und von der Arena-Szene
    /// bei Rundenstart (Reset) angesprochen.
    /// </summary>
    public class ArenaManager : MonoBehaviour
    {
        public static ArenaManager Instance;

        [Header("Arena")]
        public string arenaName = "Hinterhof Zum Blauen Eimer";

        [Header("Props")]
        public List<GameObject> interactiveProps = new List<GameObject>();
        public List<GameObject> explosiveProps = new List<GameObject>();
        public GameObject[] stageHazards;
        public Transform[] spawnPoints;

        [Header("Effects")]
        public GameObject explosionPrefab;
        public GameObject debrisPrefab;
        public GameObject dustCloudPrefab;
        public AudioClip explosionSound;
        public AudioClip propTiltSound;
        public AudioClip mopsBowlSound;

        [Header("Mops")]
        public GameObject mopsBowl;

        [Header("Explosion")]
        public float explosionRadius = 5f;
        public float explosionDamage = 25f;

        private bool isTrashed;
        private bool mopsBowlTilted;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        /// <summary>Le Bindes "Mops-Kommando": alle Props kippen/explodieren.</summary>
        public void TiltAllProps()
        {
            if (isTrashed) return;
            isTrashed = true;

            foreach (var prop in interactiveProps)
            {
                if (prop != null) StartCoroutine(TiltProp(prop));
            }
            foreach (var prop in explosiveProps)
            {
                if (prop != null) StartCoroutine(ExplodePropDelayed(prop, Random.Range(0f, 0.5f)));
            }

            if (mopsBowl != null && !mopsBowlTilted)
            {
                mopsBowlTilted = true;
                mopsBowl.transform.Rotate(Random.Range(30f, 60f), 0, Random.Range(-20f, 20f));
                if (mopsBowlSound != null) AudioSource.PlayClipAtPoint(mopsBowlSound, mopsBowl.transform.position);
            }

            if (propTiltSound != null) AudioSource.PlayClipAtPoint(propTiltSound, transform.position);
        }

        IEnumerator TiltProp(GameObject prop)
        {
            float duration = Random.Range(0.3f, 0.8f);
            float elapsed = 0f;
            Quaternion startRot = prop.transform.rotation;
            Quaternion targetRot = startRot * Quaternion.Euler(Random.Range(20f, 90f), Random.Range(-30f, 30f), 0);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                prop.transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
                yield return null;
            }
            prop.transform.rotation = targetRot;
        }

        IEnumerator ExplodePropDelayed(GameObject prop, float delay)
        {
            yield return new WaitForSeconds(delay);
            ExplodeProp(prop);
        }

        public void ExplodeProp(GameObject prop)
        {
            if (prop == null) return;
            if (explosionPrefab != null) Instantiate(explosionPrefab, prop.transform.position, Quaternion.identity);
            if (explosionSound != null) AudioSource.PlayClipAtPoint(explosionSound, prop.transform.position);

            Collider[] hits = Physics.OverlapSphere(prop.transform.position, explosionRadius);
            foreach (var hit in hits)
            {
                var f = hit.GetComponent<FighterController>();
                if (f != null)
                {
                    Vector3 dir = (f.transform.position - prop.transform.position).normalized;
                    f.TakeDamage(explosionDamage, dir, null);
                }
            }

            if (debrisPrefab != null)
                for (int i = 0; i < 5; i++)
                {
                    Vector3 pos = prop.transform.position + Random.insideUnitSphere * 2f;
                    pos.y = 0.5f;
                    Instantiate(debrisPrefab, pos, Random.rotation);
                }
            if (dustCloudPrefab != null) Instantiate(dustCloudPrefab, prop.transform.position, Quaternion.identity);
            Destroy(prop);
        }

        /// <summary>Setzt die Arena für eine neue Runde zurück (kann in voller Implementierung Props neu spawnen).</summary>
        public void ResetArena()
        {
            isTrashed = false;
            mopsBowlTilted = false;
        }

        public Transform GetSpawnPoint(int index)
            => spawnPoints != null && index < spawnPoints.Length ? spawnPoints[index] : null;

        /// <summary>Stage-Fatality "Abfüllung": Opfer kopfüber in den Bierkasten-Turm.</summary>
        public void StageFatality(FighterController victim)
        {
            StartCoroutine(StageFatalityCoroutine(victim));
        }

        IEnumerator StageFatalityCoroutine(FighterController victim)
        {
            if (victim == null) yield break;
            for (int i = 0; i < 28; i++)
            {
                yield return new WaitForSeconds(0.1f);
                // 28 Flaschen brechen nacheinander (hier nur Audio/Schaden simuliert)
                victim.TakeDamage(3f, Vector3.up, null);
            }
            victim.TakeDamage(100f, Vector3.up, null);
        }
    }
}
