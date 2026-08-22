using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Visuelles Vokabular pro Charakter (Spec §2 und §6): jede Signatur-Combo
    /// ist hier als kurze, wiederverwendbare Inszenierung hinterlegt.
    /// Charakterklassen rufen nur noch <c>SignatureFx.LeBinde_Reif(this)</c>
    /// o. Ä. auf — Partikel, Kamera, Screen-FX und Callout stecken hier.
    /// </summary>
    public class SignatureFx : MonoBehaviour
    {
        public static SignatureFx Instance { get; private set; }

        public static SignatureFx Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~SignatureFx");
                Instance = go.AddComponent<SignatureFx>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        static Vector3 Chest(FighterController f) => f.transform.position + Vector3.up * 1.1f;
        static Vector3 Over(FighterController f) => f.transform.position + Vector3.up * 2.4f;

        // ==================================================================
        //  🍺 LE BINDE
        // ==================================================================

        /// <summary>§2.1.1 Fett nachschmieren: öliger Glanz, Fettspritzer, Bodenfilm.</summary>
        public static void LeBinde_Grease(FighterController f, int charges)
        {
            Ensure();
            VFXManager.Instance?.PlayGrease(Chest(f), 10);
            f.GetComponent<CharacterVisuals>()?.BuffFlash(PennerPalette.Gold, 0.5f);
            FloatingText.Show(Over(f), $"SCHMIER {charges}", PennerPalette.Gold, 0.9f);
            Instance.StartCoroutine(Instance.GreaseFilm(f));
        }

        IEnumerator GreaseFilm(FighterController f)
        {
            // Schmierfilm auf dem Boden für 8 s
            float t = 0f;
            while (t < 8f && f != null)
            {
                t += 0.5f;
                VFXManager.Instance?.PlayGrease(f.transform.position + Vector3.up * 0.05f, 2);
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>§2.1.1 Angriff rutscht am Fett ab: weiße Schleifspur + Vorteil.</summary>
        public static void LeBinde_Slip(FighterController defender, FighterController attacker)
        {
            Ensure();
            VFXManager.Instance?.PlayHit(Chest(defender), defender.transform.forward, 0f, HitTier.Light,
                                         GameConstants.CharLeBinde);
            FloatingText.Show(Over(defender), "ABGERUTSCHT", PennerPalette.Pure, 0.8f);
            CameraShake.Shake(2f, 0.05f);
        }

        /// <summary>§2.1.3 Flaschenhals: Glassplitter + Blutungs-DoT-Anzeige.</summary>
        public static void LeBinde_Flaschenhals(FighterController f, FighterController target, bool ex)
        {
            Ensure();
            Vector3 p = target != null ? Chest(target) : Chest(f);
            VFXManager.Instance?.PlayGlass(p, f.transform.forward);
            if (ex)
            {
                for (int i = 0; i < 3; i++)
                    VFXManager.Instance?.PlayGlass(p + Random.insideUnitSphere * 0.4f, Vector3.up);
                CameraShake.Shake(5f, 0.1f);
            }
            if (target != null) Instance.StartCoroutine(Instance.BleedDot(target, 5f));
        }

        IEnumerator BleedDot(FighterController target, float seconds)
        {
            float t = 0f;
            while (t < seconds && target != null && target.gameObject.activeInHierarchy)
            {
                t += 0.5f;
                VFXManager.Instance?.PlayHit(Chest(target), Vector3.down, 2f, HitTier.Light, null);
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>§2.1.4 REIF!: Zoom aufs Gesicht, rote Adern, 6 s Glow.</summary>
        public static void LeBinde_Reif(FighterController f)
        {
            Ensure();
            Instance.StartCoroutine(Instance.ReifRoutine(f));
        }

        IEnumerator ReifRoutine(FighterController f)
        {
            CameraController.Instance?.SetCinematic(f.transform, true);
            ScreenEffects.FlashColor(PennerPalette.BloodRed, 0.3f, 0.4f);
            CameraShake.Shake(3f, 0.6f);
            FloatingText.Show(Over(f), "REIF!", PennerPalette.BloodRed, 1.6f);
            f.GetComponent<CharacterVisuals>()?.BuffFlash(PennerPalette.BloodRed, 6f);
            yield return new WaitForSecondsRealtime(0.8f);
            CameraController.Instance?.SetCinematic(null, false);

            // 6 s Buff-Funken
            float t = 0f;
            while (t < 6f && f != null)
            {
                t += 0.4f;
                VFXManager.Instance?.PlayHit(Chest(f), Vector3.up, 1f, HitTier.Light, GameConstants.CharLeBinde);
                yield return new WaitForSeconds(0.4f);
            }
            // Erschöpfung: 2 s verwundbar
            FloatingText.Show(Over(f), "…keuch", PennerPalette.Earth, 1.2f);
        }

        /// <summary>§2.1.4 Der verstärkte Treffer: riesige orange Explosion.</summary>
        public static void LeBinde_ReifImpact(FighterController f, FighterController target, float damage)
        {
            Ensure();
            Vector3 p = target != null ? Chest(target) : Chest(f);
            VFXManager.Instance?.PlayFire(p, f.transform.forward, 34);
            VFXManager.Instance?.PlayHit(p, f.transform.forward, damage, HitTier.Ex, GameConstants.CharLeBinde);
            CameraShake.Shake(10f, 0.2f);
            FloatingText.ShowDamage(p + Vector3.up * 0.5f, damage);
        }

        /// <summary>§2.1.5 Aus der Pfanne: Feuerball, Burn-DoT, Fett verbrennt.</summary>
        public static void LeBinde_Pfanne(FighterController f, FighterController target)
        {
            Ensure();
            Vector3 p = target != null ? Chest(target) : Chest(f) + f.transform.forward;
            VFXManager.Instance?.PlayFire(p, Vector3.up, 20);
            CameraShake.Shake(5f, 0.1f);
            if (target != null) Instance.StartCoroutine(Instance.BurnDot(target, 3f));
            FloatingText.Show(Over(f), "FETT VERBRANNT", PennerPalette.WarmOrange, 1.1f);
        }

        IEnumerator BurnDot(FighterController target, float seconds)
        {
            float t = 0f;
            while (t < seconds && target != null && target.gameObject.activeInHierarchy)
            {
                t += 0.4f;
                VFXManager.Instance?.PlayFire(Chest(target), Vector3.up, 6);
                yield return new WaitForSeconds(0.4f);
            }
        }

        // ==================================================================
        //  ⚡ MELL
        // ==================================================================

        /// <summary>§2.2.2 Doppelschicht: weiße, dann rote (unblockbare) Dash-Spur.</summary>
        public static void Mell_Doppelschicht(FighterController f, FighterController target, int step)
        {
            Ensure();
            Color c = step == 1 ? PennerPalette.Pure : PennerPalette.BloodRed;
            VFXManager.Instance?.PlayDust(f.transform.position, 0.6f);
            if (target != null)
            {
                VFXManager.Instance?.PlayHit(Chest(target), f.transform.forward, step == 1 ? 6f : 11f,
                                             step == 1 ? HitTier.Light : HitTier.Special, GameConstants.CharMell);
                if (step == 2) FloatingText.Show(Over(target), "UNBLOCKBAR", c, 0.9f);
            }
        }

        /// <summary>§2.2.3 Defi: 10 blaue Arcs, Überbelichtung, „BUFF GEPURGT".</summary>
        public static void Mell_Defi(FighterController f, FighterController target)
        {
            Ensure();
            VFXManager.Instance?.PlayElectro(Chest(f) + f.transform.forward * 0.4f, 10);
            if (target != null)
            {
                VFXManager.Instance?.PlayElectro(Chest(target), 10);
                FloatingText.Show(Over(target), "BUFF GEPURGT", PennerPalette.NeonBlue, 1.2f);
            }
            ScreenEffects.FlashColor(Color.white, 0.45f, 0.12f);
        }

        /// <summary>§2.2.4 Sechzehn Stunden: 11 Kopien, Regenbogen-Treffer, Finale.</summary>
        public static void Mell_SechzehnStunden(FighterController f, FighterController target)
        {
            Ensure();
            Instance.StartCoroutine(Instance.SechzehnStundenRoutine(f, target));
        }

        IEnumerator SechzehnStundenRoutine(FighterController f, FighterController target)
        {
            FloatingText.Show(Over(f), "SECHZEHN STUNDEN!", PennerPalette.NeonBlue, 1.6f);
            ScreenEffects.RainbowSweep(0.6f);
            ScreenEffects.FlashColor(PennerPalette.NeonBlue, 0.5f, 0.12f);

            Vector3 center = target != null ? target.transform.position : f.transform.position + f.transform.forward * 2f;
            for (int i = 0; i < 11; i++)
            {
                float a = i * (360f / 11f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.4f;
                Vector3 pos = center + offset + Vector3.up * 1.1f;

                SpawnAfterImage(f, pos, Color.HSVToRGB(i / 11f, 0.85f, 1f));
                VFXManager.Instance?.PlayRainbowHit(center + Vector3.up * 1.1f, -offset.normalized, i);
                yield return new WaitForSeconds(0.5f / 11f);
            }

            // Finaler goldener Treffer
            VFXManager.Instance?.PlayHit(center + Vector3.up * 1.1f, f.transform.forward, 38f,
                                         HitTier.Ex, GameConstants.CharMell);
            VFXManager.Instance?.PlayFireworks(center + Vector3.up, 3);
            CameraShake.Shake(12f, 0.2f);
        }

        void SpawnAfterImage(FighterController f, Vector3 pos, Color tint)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(go.GetComponent<Collider>());
            go.name = "PK_AfterImage";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.4f, 0.85f, 0.4f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Sprites/Default"));
            mr.material.color = tint.WithAlpha(0.45f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(go, 0.35f);
        }

        // ==================================================================
        //  🎲 MOJO BOB
        // ==================================================================

        /// <summary>§2.3.1 Mojo-Wette: goldener Würfel dreht sich über Bob.</summary>
        public static void MojoBob_Gamble(FighterController f, int points, float newChance)
        {
            Ensure();
            Instance.StartCoroutine(Instance.DiceRoutine(f, newChance));
        }

        IEnumerator DiceRoutine(FighterController f, float chance)
        {
            var die = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(die.GetComponent<Collider>());
            die.name = "PK_MojoWuerfel";
            die.transform.localScale = Vector3.one * 0.35f;
            var mr = die.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Sprites/Default"));
            mr.material.color = PennerPalette.Gold;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            float t = 0f;
            while (t < 0.8f && f != null)
            {
                t += Time.deltaTime;
                die.transform.position = Over(f);
                die.transform.Rotate(360f * Time.deltaTime, 540f * Time.deltaTime, 180f * Time.deltaTime);
                yield return null;
            }
            FloatingText.Show(Over(f), $"KRIT {Mathf.RoundToInt(chance * 100f)} %", PennerPalette.Gold, 1f);
            Destroy(die);
        }

        /// <summary>§2.3.1 Wette verloren: roter Blitz, Mojo weg, 10 % HP.</summary>
        public static void MojoBob_GambleLost(FighterController f)
        {
            Ensure();
            ScreenEffects.FlashColor(PennerPalette.BloodRed, 0.4f, 0.25f);
            FloatingText.Show(Over(f), "MOJO WEG", PennerPalette.BloodRed, 1.3f);
            CameraShake.Shake(6f, 0.12f);
        }

        /// <summary>§2.3.2 Löffelsturm: 17 silberne Flugbahnen mit Glitzer-Trail.</summary>
        public static void MojoBob_Loeffelsturm(FighterController f, int crits)
        {
            Ensure();
            Vector3 origin = Chest(f) + f.transform.forward * 0.4f;
            for (int i = 0; i < 17; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, Mathf.Lerp(-28f, 28f, i / 16f), 0f) * f.transform.forward;
                VFXManager.Instance?.PlayHit(origin, dir, 3f, HitTier.Light, GameConstants.CharMojoBob);
            }
            if (crits > 0)
            {
                FloatingText.Show(Over(f), $"{crits}× KRIT", PennerPalette.Gold, 1.1f);
                ScreenEffects.FlashColor(PennerPalette.Gold, 0.25f, 0.15f);
            }
        }

        /// <summary>§2.3.3 Dingeneldang!: goldene Säule, Bildschirm gold, 8 s Buff.</summary>
        public static void MojoBob_Dingeneldang(FighterController f, float duration)
        {
            Ensure();
            Instance.StartCoroutine(Instance.DingeneldangRoutine(f, duration));
        }

        IEnumerator DingeneldangRoutine(FighterController f, float duration)
        {
            ScreenEffects.SetState(ScreenState.Dingeneldang);
            CameraController.Instance?.SetCinematic(f.transform, true);
            var pillar = BuildPillar(f.transform.position);
            FloatingText.Show(Over(f), "DINGENELDANG!", PennerPalette.Gold, 2f);
            CameraShake.Shake(8f, 0.25f);
            yield return new WaitForSecondsRealtime(0.5f);
            CameraController.Instance?.SetCinematic(null, false);

            float t = 0f;
            while (t < duration && f != null)
            {
                t += 0.25f;
                if (pillar != null) pillar.transform.position = f.transform.position;
                VFXManager.Instance?.PlayHit(Chest(f), Vector3.up, 2f, HitTier.Critical, GameConstants.CharMojoBob);
                yield return new WaitForSeconds(0.25f);
            }
            if (pillar != null) Destroy(pillar);
            ScreenEffects.SetState(ScreenState.MojoLockout);
            FloatingText.Show(Over(f), "AUSGESPERRT 15 s", PennerPalette.Pure, 1.6f);
            yield return new WaitForSeconds(15f);
            ScreenEffects.SetState(ScreenState.Normal);
        }

        GameObject BuildPillar(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.name = "PK_GoldSaeule";
            go.transform.position = pos + Vector3.up;
            go.transform.localScale = new Vector3(1.1f, 2f, 1.1f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Sprites/Default"));
            mr.material.color = PennerPalette.Gold.WithAlpha(0.28f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        // ==================================================================
        //  💥 ÜBRIGE SECHS (Spec §2.4 Kurzübersicht)
        // ==================================================================

        /// <summary>Dieter — Rohrbruch/Kanalisation: Schlammfontäne + Rutschzone.</summary>
        public static void Dieter_Kanal(FighterController f, Vector3 at)
        {
            Ensure();
            VFXManager.Instance?.PlaySludge(at + Vector3.up * 0.2f, Vector3.up, 26);
            VFXManager.Instance?.PlayDust(at, 0.8f);
            CameraShake.Shake(5f, 0.1f);
        }

        /// <summary>Dieter — Abflussreiniger: grüne Giftwolke, +30 % Schaden.</summary>
        public static void Dieter_Abflussreiniger(FighterController f)
        {
            Ensure();
            VFXManager.Instance?.PlayPoison(Chest(f), 1.4f);
            f.GetComponent<CharacterVisuals>()?.BuffFlash(PennerPalette.PoisonGrn, 1f);
            FloatingText.Show(Over(f), "ABFLUSSREINIGER", PennerPalette.PoisonGrn, 1.1f);
        }

        /// <summary>Uschi — Gurke/Suppe/Topfdeckel: grüne Heilpartikel bzw. blauer Schild.</summary>
        public static void Uschi_Heal(FighterController f, float amount, bool aoe)
        {
            Ensure();
            var ps = VFXManager.Instance;
            ps?.PlayPoison(Chest(f), aoe ? 2.2f : 0.8f);
            FloatingText.Show(Over(f), $"+{Mathf.RoundToInt(amount)} HP", PennerPalette.PoisonGrn, 1f);
            f.GetComponent<CharacterVisuals>()?.BuffFlash(PennerPalette.PoisonGrn, 0.6f);
        }

        public static void Uschi_Topfdeckel(FighterController f)
        {
            Ensure();
            f.GetComponent<CharacterVisuals>()?.BuffFlash(PennerPalette.NeonBlue, 1.2f);
            FloatingText.Show(Over(f), "TOPFDECKEL", PennerPalette.NeonBlue, 1f);
        }

        /// <summary>TetraPak — Fusel-Atem: Feuerstoß + oranger Bildschirmfilter.</summary>
        public static void TetraPak_FuselAtem(FighterController f, FighterController target)
        {
            Ensure();
            VFXManager.Instance?.PlayFire(Chest(f) + f.transform.forward * 0.5f, f.transform.forward, 30);
            if (!f.isAI) ScreenEffects.SetState(ScreenState.Fusel);
            CameraShake.Shake(5f, 0.1f);
            if (target != null) Instance.StartCoroutine(Instance.BurnDot(target, 3f));
            Instance.StartCoroutine(Instance.ResetStateAfter(2.5f, f));
        }

        /// <summary>TetraPak — Trinken: Buff + verschwommenes Sehen.</summary>
        public static void TetraPak_Trinken(FighterController f)
        {
            Ensure();
            VFXManager.Instance?.PlaySludge(Chest(f), Vector3.down, 8);
            if (!f.isAI) ScreenEffects.SetState(ScreenState.Fusel);
            FloatingText.Show(Over(f), "PROST", PennerPalette.WarmOrange, 1f);
            Instance.StartCoroutine(Instance.ResetStateAfter(4f, f));
        }

        /// <summary>TetraPak — Zweiter Wind (≤20 % HP): weißer Glow, +20 % Schaden.</summary>
        public static void TetraPak_ZweiterWind(FighterController f)
        {
            Ensure();
            f.GetComponent<CharacterVisuals>()?.BuffFlash(Color.white, 8f);
            ScreenEffects.FlashColor(Color.white, 0.4f, 0.3f);
            FloatingText.Show(Over(f), "ZWEITER WIND", Color.white, 1.4f);
        }

        /// <summary>Sigi — Root-Zugriff/DDoS/SQL: grüner Matrix-Effekt.</summary>
        public static void Sigi_Hack(FighterController f, FighterController target, string label)
        {
            Ensure();
            if (target != null)
            {
                VFXManager.Instance?.PlayElectro(Chest(target), 4);
                FloatingText.Show(Over(target), label, PennerPalette.Hex("39FF14"), 1.2f);
            }
            if (!f.isAI) ScreenEffects.SetState(ScreenState.Matrix);
            Instance.StartCoroutine(Instance.ResetStateAfter(3f, f));
        }

        /// <summary>Rolf — Ratten/Rattenkönig/Ratengift.</summary>
        public static void Rolf_Ratten(FighterController f, int count)
        {
            Ensure();
            for (int i = 0; i < count; i++)
                VFXManager.Instance?.PlayDust(Extensions.RandomHorizontalPoint(f.transform.position, 1.5f), 0.3f);
            if (!f.isAI) ScreenEffects.SetState(ScreenState.RatSwarm);
            Instance.StartCoroutine(Instance.ResetStateAfter(3f, f));
        }

        public static void Rolf_Gift(FighterController f, Vector3 at)
        {
            Ensure();
            VFXManager.Instance?.PlayPoison(at, 1.8f);
        }

        /// <summary>Kalle — Arbeitshandschuh (Zug), Rohrzange (Anti-Air), Reparatur.</summary>
        public static void Kalle_Zug(FighterController f, FighterController target)
        {
            Ensure();
            if (target == null) return;
            VFXManager.Instance?.PlayHit(Chest(target), (f.transform.position - target.transform.position).normalized,
                                         6f, HitTier.Special, GameConstants.CharKalle);
            FloatingText.Show(Over(target), "KOMM HER", PennerPalette.NeonBlue, 0.9f);
        }

        public static void Kalle_Reparatur(FighterController f, float amount)
        {
            Ensure();
            f.GetComponent<CharacterVisuals>()?.BuffFlash(PennerPalette.NeonBlue, 1f);
            FloatingText.Show(Over(f), $"+{Mathf.RoundToInt(amount)} HP", PennerPalette.NeonBlue, 1f);
        }

        IEnumerator ResetStateAfter(float seconds, FighterController f)
        {
            yield return new WaitForSeconds(seconds);
            if (f == null || !f.isAI) ScreenEffects.SetState(ScreenState.Normal);
        }
    }
}
