using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Frame-genaue Inszenierung von Le Bindes Mops-Kommando (Spec §3.2).
    /// Die Sequenz läuft über 180 Frames (≈3 s bei 60 fps) und ist in
    /// unskalierter Zeit getaktet, damit sie auch in der Zeitlupe stimmt.
    ///
    /// 0–20    Tasche auf, Paula schaut heraus (Zeitlupe 0,1×, Zoom)
    /// 20–40   Paula springt heraus
    /// 40–60   Paula trottet zum Gegner (Pfotenabdrücke)
    /// 60–80   Auge-in-Auge (Kamera-Zoom, Herzschlag)
    /// 80–100  Häufchen (Dampf)
    /// 100–120 Debuff „FASSUNGSLOSIGKEIT"
    /// 120–150 Arena kippt (Zeitlupe 0,3×)
    /// 150–180 Staubwolke, Neon flackert, Normalisierung
    /// </summary>
    public class MopsKommandoSequence : MonoBehaviour
    {
        public const float FrameTime = 1f / 60f;

        public static void Play(FighterController owner, FighterController target)
        {
            var go = new GameObject("~MopsKommando");
            var seq = go.AddComponent<MopsKommandoSequence>();
            seq.StartCoroutine(seq.Run(owner, target));
        }

        IEnumerator Run(FighterController owner, FighterController target)
        {
            if (owner == null) { Destroy(gameObject); yield break; }
            Vector3 start = owner.transform.position + owner.transform.forward * 0.6f + Vector3.up * 0.9f;
            Vector3 goal = target != null
                ? target.transform.position + Vector3.up * 0.1f
                : start + owner.transform.forward * 3f;

            // ---- Frames 0–20: Zeitlupe + Zoom auf Paula ----
            CameraShake.SlowMotion(0.1f, 20 * FrameTime);
            var paula = BuildPaula(start);
            CameraController.Instance?.SetCinematic(paula.transform, true);
            VFXManager.Instance?.PlayDust(start, 0.4f);
            yield return Frames(20);

            // ---- 20–40: Sprung aus der Tasche ----
            float t = 0f;
            Vector3 landing = new Vector3(start.x, 0.25f, start.z) + owner.transform.forward * 0.5f;
            while (t < 20 * FrameTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / (20 * FrameTime));
                paula.transform.position = Vector3.Lerp(start, landing, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.5f;
                paula.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.5f, k);
                yield return null;
            }
            VFXManager.Instance?.PlayDust(landing, 0.5f);

            // ---- 40–60: Weg zum Gegner + Pfotenabdrücke ----
            t = 0f;
            Vector3 from = paula.transform.position;
            float nextPaw = 0f;
            while (t < 20 * FrameTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / (20 * FrameTime));
                paula.transform.position = Vector3.Lerp(from, goal + Vector3.up * 0.25f, k);
                paula.transform.rotation = Quaternion.LookRotation((goal - from).normalized + Vector3.forward * 0.001f);
                if (t > nextPaw)
                {
                    nextPaw = t + 0.08f;
                    SpawnPawPrint(paula.transform.position);
                }
                yield return null;
            }

            // ---- 60–80: Auge in Auge ----
            if (target != null)
            {
                CameraController.Instance?.SetCinematic(target.transform, true);
                target.ApplyGrabStun(20 * FrameTime + 0.4f);
            }
            yield return Frames(20);

            // ---- 80–100: Häufchen ----
            var pile = BuildPile(goal + Vector3.up * 0.06f);
            VFXManager.Instance?.PlayDust(pile.transform.position, 0.3f);
            FloatingText.Show(pile.transform.position + Vector3.up * 0.8f, "…plumps", PennerPalette.Earth, 1.2f);
            yield return Frames(20);

            // ---- 100–120: Debuff-Banner ----
            if (target != null)
            {
                FloatingText.Show(target.transform.position + Vector3.up * 2.4f,
                                  "FASSUNGSLOSIGKEIT", PennerPalette.PoisonGrn, 2.2f);
                target.InvertInputs(0f);            // kein Invert, nur Kanal für Debuffs
                var delay = target.GetComponent<InputHack>();
                if (delay != null) delay.Enable(0.0f);
            }
            yield return Frames(20);

            // ---- 120–150: Arena kippt in Zeitlupe ----
            CameraShake.SlowMotion(0.3f, 30 * FrameTime);
            foreach (var prop in FindObjectsOfType<ArenaProp>())
            {
                if (prop.kind == ArenaProp.PropKind.GasBottle) prop.Explode(owner);
                else prop.Tilt();
                yield return Frames(2);
            }
            ArenaManager.Instance?.TiltAllProps();
            CameraShake.Shake(9f, 0.25f);
            yield return Frames(10);

            // ---- 150–180: Staub, Neon-Flackern, zurück zur Kampfkamera ----
            for (int i = 0; i < 6; i++)
                VFXManager.Instance?.PlayDust(Extensions.RandomHorizontalPoint(goal, 4f), 1.3f);
            CameraController.Instance?.SetCinematic(null, false);
            yield return Frames(30);

            Destroy(paula, 4f);
            Destroy(pile, 12f);
            Destroy(gameObject);
        }

        static IEnumerator Frames(int frames)
        {
            yield return new WaitForSecondsRealtime(frames * FrameTime);
        }

        GameObject BuildPaula(Vector3 pos)
        {
            // Platzhalter-Mops, bis ein Modell existiert: gedrungener Körper + Kopf
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "PK_Paula";
            Destroy(body.GetComponent<Collider>());
            body.transform.position = pos;
            body.transform.localScale = new Vector3(0.28f, 0.16f, 0.28f);
            body.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Paint(body, PennerPalette.Hex("D2B48C"));

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(body.transform, false);
            head.transform.localPosition = new Vector3(0f, 0f, 1.1f);
            head.transform.localScale = Vector3.one * 0.9f;
            Paint(head, PennerPalette.Hex("6B4423"));
            return body;
        }

        GameObject BuildPile(Vector3 pos)
        {
            var pile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pile.name = "PK_Haeufchen";
            Destroy(pile.GetComponent<Collider>());
            pile.transform.position = pos;
            pile.transform.localScale = new Vector3(0.35f, 0.22f, 0.35f);
            Paint(pile, PennerPalette.Hex("5A3A1A"));

            // Dampf
            VFXManager.Instance?.PlayDust(pos + Vector3.up * 0.2f, 0.4f);
            return pile;
        }

        void SpawnPawPrint(Vector3 pos)
        {
            var paw = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(paw.GetComponent<Collider>());
            paw.name = "PK_Pfote";
            paw.transform.position = new Vector3(pos.x, 0.02f, pos.z);
            paw.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            paw.transform.localScale = Vector3.one * 0.18f;
            Paint(paw, PennerPalette.Earth.WithAlpha(0.7f), sprite: true);
            Destroy(paw, 2f);
        }

        static void Paint(GameObject go, Color c, bool sprite = false)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            mr.material = new Material(Shader.Find(sprite ? "Sprites/Default" : "Universal Render Pipeline/Lit")
                                       ?? Shader.Find("Standard"));
            mr.material.color = c;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
