// ============================================================================
// SolarEdu Analytics - Unity Plugin
// File: SolarEduConfig.cs
// Deskripsi: ScriptableObject konfigurasi untuk menyimpan URL API & API Key.
//            Buat via menu: Assets > Create > SolarEdu > Config
// ============================================================================
using UnityEngine;

namespace SolarEdu
{
    [CreateAssetMenu(fileName = "SolarEduConfig", menuName = "SolarEdu/Config", order = 1)]
    public class SolarEduConfig : ScriptableObject
    {
        [Header("═══ Server Settings ═══")]
        [Tooltip("URL dashboard SolarEdu Analytics (tanpa trailing slash).\nContoh: https://solaredu-analytics.vercel.app")]
        public string apiUrl = "http://localhost:3001";

        [Header("═══ API Key ═══")]
        [Tooltip("API Key dari dashboard (mulai dengan 'se_...')\nAmbil di menu Settings > API Keys > Generate")]
        public string apiKey = "";

        [Header("═══ Default Actor ═══")]
        [Tooltip("Homepage untuk identifikasi akun siswa.\nBiasanya URL dashboard.")]
        public string actorHomePage = "https://solaredu-analytics.vercel.app";

        /// <summary>
        /// Menghasilkan header Authorization Basic Auth dari keyId:secret
        /// </summary>
        public string GetAuthHeader()
        {
            string credentials = $"{apiKey}:";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(credentials);
            return "Basic " + System.Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Full endpoint URL untuk POST xAPI statements
        /// </summary>
        public string StatementsEndpoint => $"{apiUrl.TrimEnd('/')}/api/xapi/statements";

        /// <summary>
        /// Validasi apakah config sudah diisi
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(apiUrl)
                && !string.IsNullOrEmpty(apiKey);
        }
    }
}
