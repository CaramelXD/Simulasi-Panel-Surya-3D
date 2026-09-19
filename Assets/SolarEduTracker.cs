// ============================================================================
// SolarEdu Analytics - Unity Plugin
// File: SolarEduTracker.cs
// Deskripsi: Komponen yang ditempel ke GameObject untuk tracking interaksi.
//            Bisa trigger manual (dari script) atau otomatis (via button/event).
// ============================================================================
using UnityEngine;
using UnityEngine.UI;

namespace SolarEdu
{
    /// <summary>
    /// Komponen tracking xAPI — ditempel ke GameObject apapun.
    /// Pilih verb & object dari Inspector, lalu panggil Track() kapan saja.
    /// </summary>
    [AddComponentMenu("SolarEdu/Tracker")]
    public class SolarEduTracker : MonoBehaviour
    {
        [Header("═══ Aksi / Verb ═══")]
        [Tooltip("Pilih aksi yang akan dicatat saat Track() dipanggil")]
        public SolarEduVerb verb = SolarEduVerb.Melihat;

        [Header("═══ Object / Materi ═══")]
        [Tooltip("ID unik object (misal: 'solaredu://activity/panel-surya-1')")]
        public string objectId = "solaredu://activity/";

        [Tooltip("Nama object yang user-friendly")]
        public string objectName = "Panel Surya";

        [Header("═══ Skor (Opsional) ═══")]
        [Tooltip("Centang jika ingin mengirim skor bersama statement")]
        public bool includeSkor = false;

        [Tooltip("Skor yang didapat (0-100)")]
        [Range(0, 100)]
        public float skor = 0;

        [Tooltip("Apakah berhasil?")]
        public bool berhasil = false;

        [Tooltip("Apakah selesai?")]
        public bool selesai = false;

        [Tooltip("Durasi aktivitas (detik). 0 = tidak dikirim.")]
        public float durasiDetik = 0;

        [Header("═══ Auto Trigger ═══")]
        [Tooltip("Otomatis track saat GameObject aktif (OnEnable)")]
        public bool trackOnEnable = false;

        [Tooltip("Otomatis track saat Start")]
        public bool trackOnStart = false;

        [Tooltip("Jika ada UI Button, otomatis link onClick ke Track()")]
        public Button autoLinkButton;

        // ─── Lifecycle ───

        void Start()
        {
            // Auto-link button jika di-assign
            if (autoLinkButton != null)
            {
                autoLinkButton.onClick.AddListener(Track);
            }

            if (trackOnStart)
            {
                Track();
            }
        }

        void OnEnable()
        {
            if (trackOnEnable)
            {
                Track();
            }
        }

        // ═══════════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Kirim xAPI statement berdasarkan setting di Inspector.
        /// Bisa dipanggil dari script lain, Button onClick, atau Event.
        /// </summary>
        public void Track()
        {
            if (SolarEduManager.Instance == null)
            {
                Debug.LogError("[SolarEdu] SolarEduManager tidak ditemukan di scene!");
                return;
            }

            if (includeSkor)
            {
                var result = new StatementResult
                {
                    rawScore = skor,
                    minScore = 0,
                    maxScore = 100,
                    success = berhasil,
                    completion = selesai,
                    durationSeconds = durasiDetik,
                };
                SolarEduManager.Instance.SendStatement(verb, objectId, objectName, result);
            }
            else
            {
                SolarEduManager.Instance.SendStatement(verb, objectId, objectName);
            }

            Debug.Log($"[SolarEdu Tracker] {verb} → {objectName}");
        }

        /// <summary>
        /// Track dengan override skor (berguna dipanggil dari kode).
        /// </summary>
        public void TrackWithScore(float rawScore, bool success, bool completion, float duration = 0)
        {
            var result = new StatementResult
            {
                rawScore = rawScore,
                minScore = 0,
                maxScore = 100,
                success = success,
                completion = completion,
                durationSeconds = duration,
            };
            SolarEduManager.Instance.SendStatement(verb, objectId, objectName, result);
        }

        /// <summary>
        /// Track dengan verb berbeda (override tanpa ubah Inspector).
        /// </summary>
        public void TrackVerb(SolarEduVerb overrideVerb)
        {
            SolarEduManager.Instance.SendStatement(overrideVerb, objectId, objectName);
        }
    }
}
