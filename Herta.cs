using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Herta ist eine statische, unverwundbare Figur im Hinterhof (an der
    /// "Le Bindes Mops-Kommando" ausgerichtet ist). Sie ist rein kosmetisch,
    /// unangreifbar und unbeeindruckt. Sie wird über Trophäen z.B. für
    /// "Wer ist ein braves Mädchen?" (100× einsetzen) gezählt.
    /// </summary>
    public class Herta : MonoBehaviour
    {
        [Header("Herta")]
        public string displayName = "Herta";
        public int uses;
        public AudioClip pleasedSound;

        // Herta kann nie Schaden nehmen / bewegt sich nie.
        public void RegisterUse()
        {
            uses++;
            if (pleasedSound != null) AudioSource.PlayClipAtPoint(pleasedSound, transform.position);
            if (uses >= 100 && TrophyManager.Instance != null)
                TrophyManager.Instance.UpdateProgress(TrophyManager.TROPHY_BRAVES_MADCHEN, 1f);
        }

        void OnCollisionEnter(Collision col)
        {
            // Herta bleibt stehen — keine Interaktion.
        }
    }
}
