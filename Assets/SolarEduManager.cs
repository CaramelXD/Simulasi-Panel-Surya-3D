// ============================================================================
// SolarEdu Analytics - Unity Plugin
// File: SolarEduManager.cs
// Deskripsi: Singleton manager yang mengirim xAPI statements ke dashboard.
//            Attach ke GameObject yang persistent (DontDestroyOnLoad).
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SolarEdu
{
    /// <summary>
    /// Verb-verb xAPI yang digunakan di SolarEdu
    /// </summary>
    [Serializable]
    public enum SolarEduVerb
    {
        [InspectorName("membuka (launched)")]
        Membuka,
        [InspectorName("memulai (initialized)")]
        Memulai,
        [InspectorName("merancang (designed)")]
        Merancang,
        [InspectorName("membangun (built)")]
        Membangun,
        [InspectorName("menguji (tested)")]
        Menguji,
        [InspectorName("mendesain-ulang (redesigned)")]
        MendesainUlang,
        [InspectorName("memasang (installed)")]
        Memasang,
        [InspectorName("menghapus (removed)")]
        Menghapus,
        [InspectorName("mengkonfigurasi (configured)")]
        Mengkonfigurasi,
        [InspectorName("menghitung (calculated)")]
        Menghitung,
        [InspectorName("menyelesaikan (completed)")]
        Menyelesaikan,
        [InspectorName("melihat (viewed)")]
        Melihat,
        [InspectorName("mengamati (observed)")]
        Mengamati,
    }

    /// <summary>
    /// Data verb: mapping enum ke URI dan display name
    /// </summary>
    public static class SolarEduVerbData
    {
        public struct VerbInfo
        {
            public string id;
            public string display;
        }

        private static readonly Dictionary<SolarEduVerb, VerbInfo> _verbs = new Dictionary<SolarEduVerb, VerbInfo>
        {
            { SolarEduVerb.Membuka,          new VerbInfo { id = "http://adlnet.gov/expapi/verbs/launched",           display = "membuka" } },
            { SolarEduVerb.Memulai,           new VerbInfo { id = "http://adlnet.gov/expapi/verbs/initialized",       display = "memulai" } },
            { SolarEduVerb.Merancang,        new VerbInfo { id = "https://solaredu.app/xapi/verbs/designed",          display = "merancang" } },
            { SolarEduVerb.Membangun,        new VerbInfo { id = "https://solaredu.app/xapi/verbs/built",             display = "membangun" } },
            { SolarEduVerb.Menguji,          new VerbInfo { id = "https://solaredu.app/xapi/verbs/tested",            display = "menguji" } },
            { SolarEduVerb.MendesainUlang,   new VerbInfo { id = "https://solaredu.app/xapi/verbs/redesigned",        display = "mendesain-ulang" } },
            { SolarEduVerb.Memasang,         new VerbInfo { id = "https://solaredu.app/xapi/verbs/installed",          display = "memasang" } },
            { SolarEduVerb.Menghapus,        new VerbInfo { id = "https://solaredu.app/xapi/verbs/removed",            display = "menghapus" } },
            { SolarEduVerb.Mengkonfigurasi,  new VerbInfo { id = "https://solaredu.app/xapi/verbs/configured",         display = "mengkonfigurasi" } },
            { SolarEduVerb.Menghitung,       new VerbInfo { id = "https://solaredu.app/xapi/verbs/calculated",         display = "menghitung" } },
            { SolarEduVerb.Menyelesaikan,    new VerbInfo { id = "http://adlnet.gov/expapi/verbs/completed",           display = "menyelesaikan" } },
            { SolarEduVerb.Melihat,          new VerbInfo { id = "http://adlnet.gov/expapi/verbs/viewed",              display = "melihat" } },
            { SolarEduVerb.Mengamati,        new VerbInfo { id = "https://solaredu.app/xapi/verbs/observed",           display = "mengamati" } },
        };

        public static VerbInfo Get(SolarEduVerb verb) => _verbs[verb];
    }

    /// <summary>
    /// Singleton Manager — mengelola pengiriman xAPI statements ke dashboard.
    /// Letakkan di scene pertama, akan persist antar scene.
    /// </summary>
    public class SolarEduManager : MonoBehaviour
    {
        // ─── Singleton ───
        private static SolarEduManager _instance;
        public static SolarEduManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Cari instance yang sudah ada di scene (jangan auto-create)
                    _instance = FindFirstObjectByType<SolarEduManager>();
                }
                return _instance;
            }
        }

        [Header("═══ Config ═══")]
        [Tooltip("Drag SolarEduConfig ScriptableObject ke sini")]
        public SolarEduConfig config;

        [Header("═══ Actor (Siswa aktif) ═══")]
        [Tooltip("Nama siswa yang sedang bermain")]
        public string actorName = "Siswa";

        [Tooltip("Account name / email siswa")]
        public string actorAccountName = "siswa@solaredu.app";

        [Header("═══ Debug ═══")]
        public bool enableLogging = true;

        // ─── Statistik ───
        [HideInInspector] public int totalSent = 0;
        [HideInInspector] public int totalSuccess = 0;
        [HideInInspector] public int totalFailed = 0;

        // ─── Events ───
        public static event Action<string> OnStatementSent;
        public static event Action<string> OnStatementError;

        private Queue<string> _queue = new Queue<string>();
        private bool _isSending = false;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Jika instance lama tidak punya config tapi ini punya, ganti
                if (_instance.config == null && this.config != null)
                {
                    Destroy(_instance.gameObject);
                    _instance = this;
                    DontDestroyOnLoad(gameObject);
                    return;
                }
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ═══════════════════════════════════════════════════
        //  PUBLIC API — Kirim statement
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Kirim xAPI statement sederhana (verb + object).
        /// </summary>
        public void SendStatement(SolarEduVerb verb, string objectId, string objectName)
        {
            SendStatement(verb, objectId, objectName, null);
        }

        /// <summary>
        /// Kirim xAPI statement dengan skor langsung (shortcut).
        /// </summary>
        public void SendStatement(SolarEduVerb verb, string objectId, string objectName,
            float score = -1, bool success = false, bool completion = false)
        {
            if (score < 0)
            {
                SendStatement(verb, objectId, objectName, null);
                return;
            }

            var result = new StatementResult
            {
                rawScore = score,
                minScore = 0,
                maxScore = 100,
                success = success,
                completion = completion,
            };
            SendStatement(verb, objectId, objectName, result);
        }

        /// <summary>
        /// Kirim xAPI statement dengan skor/result.
        /// </summary>
        public void SendStatement(SolarEduVerb verb, string objectId, string objectName, StatementResult result)
        {
            if (config == null || !config.IsValid())
            {
                LogError("SolarEduConfig belum diisi! Drag Config ke SolarEduManager.");
                return;
            }

            var verbInfo = SolarEduVerbData.Get(verb);
            var statement = new XapiStatement
            {
                actor = new XapiActor
                {
                    name = actorName,
                    account = new XapiAccount
                    {
                        homePage = config.actorHomePage,
                        name = actorAccountName,
                    },
                },
                verb = new XapiVerb
                {
                    id = verbInfo.id,
                    display = new XapiDisplay { id = verbInfo.display },
                },
                objectData = new XapiObject
                {
                    id = objectId,
                    definition = new XapiDefinition
                    {
                        name = new XapiDisplay { id = objectName },
                    },
                },
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            };

            if (result != null)
            {
                statement.result = new XapiResult
                {
                    score = new XapiScore
                    {
                        raw = result.rawScore,
                        min = result.minScore,
                        max = result.maxScore,
                        scaled = result.maxScore > 0 ? (float)result.rawScore / result.maxScore : 0,
                    },
                    success = result.success,
                    completion = result.completion,
                    duration = result.durationSeconds > 0 ? $"PT{result.durationSeconds}S" : null,
                };
            }

            string json = JsonUtility.ToJson(statement);
            // Fix: JsonUtility serializes "objectData" tapi API expects "object"
            json = json.Replace("\"objectData\":", "\"object\":");

            _queue.Enqueue(json);
            if (!_isSending) StartCoroutine(ProcessQueue());
        }

        /// <summary>
        /// Kirim raw JSON statement (untuk advanced usage).
        /// </summary>
        public void SendRawStatement(string json)
        {
            _queue.Enqueue(json);
            if (!_isSending) StartCoroutine(ProcessQueue());
        }

        // ═══════════════════════════════════════════════════
        //  QUEUE PROCESSOR
        // ═══════════════════════════════════════════════════

        private IEnumerator ProcessQueue()
        {
            _isSending = true;
            while (_queue.Count > 0)
            {
                string json = _queue.Dequeue();
                yield return StartCoroutine(PostStatement(json));
            }
            _isSending = false;
        }

        private IEnumerator PostStatement(string json)
        {
            totalSent++;

            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            var request = new UnityWebRequest(config.StatementsEndpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", config.GetAuthHeader());
            request.SetRequestHeader("X-Experience-API-Version", "1.0.3");
            request.timeout = 10;

            Log($"Mengirim statement ke {config.StatementsEndpoint}...");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                totalSuccess++;
                Log($"Statement terkirim! Response: {request.downloadHandler.text}");
                OnStatementSent?.Invoke(request.downloadHandler.text);
            }
            else
            {
                totalFailed++;
                string error = $"Gagal kirim statement: {request.error} | {request.downloadHandler?.text}";
                LogError(error);
                OnStatementError?.Invoke(error);
            }

            request.Dispose();
        }

        // ═══════════════════════════════════════════════════
        //  LOGGING
        // ═══════════════════════════════════════════════════

        private void Log(string msg)
        {
            if (enableLogging) Debug.Log($"[SolarEdu] {msg}");
        }

        private void LogError(string msg)
        {
            Debug.LogError($"[SolarEdu] {msg}");
        }

        // ═══════════════════════════════════════════════════
        //  JSON DATA CLASSES (untuk JsonUtility)
        // ═══════════════════════════════════════════════════

        [Serializable]
        public class XapiStatement
        {
            public XapiActor actor;
            public XapiVerb verb;
            public XapiObject objectData; // akan di-rename ke "object" di JSON
            public XapiResult result;
            public string timestamp;
        }

        [Serializable]
        public class XapiActor
        {
            public string name;
            public XapiAccount account;
        }

        [Serializable]
        public class XapiAccount
        {
            public string homePage;
            public string name;
        }

        [Serializable]
        public class XapiVerb
        {
            public string id;
            public XapiDisplay display;
        }

        [Serializable]
        public class XapiDisplay
        {
            public string id;
        }

        [Serializable]
        public class XapiObject
        {
            public string id;
            public XapiDefinition definition;
        }

        [Serializable]
        public class XapiDefinition
        {
            public XapiDisplay name;
        }

        [Serializable]
        public class XapiResult
        {
            public XapiScore score;
            public bool success;
            public bool completion;
            public string duration;
        }

        [Serializable]
        public class XapiScore
        {
            public float scaled;
            public float raw;
            public float min;
            public float max;
        }
    }

    /// <summary>
    /// Helper class untuk result / skor
    /// </summary>
    public class StatementResult
    {
        public float rawScore = 0;
        public float minScore = 0;
        public float maxScore = 100;
        public bool success = false;
        public bool completion = false;
        public float durationSeconds = 0;
    }
}
