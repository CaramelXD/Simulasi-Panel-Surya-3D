// ============================================================================
// SolarEdu Analytics - Unity Plugin
// File: SolarEduEditorWindow.cs
// Deskripsi: Editor Window visual untuk testing kirim statement dari Unity Editor.
//            Buka via menu: Tools > SolarEdu > Test Panel
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Globalization;

namespace SolarEdu.Editor
{
    public class SolarEduEditorWindow : EditorWindow
    {
        // ─── Config ───
        private SolarEduConfig config;
        private bool configFoldout = true;

        // ─── Actor ───
        private string actorName = "Test Siswa";
        private string actorAccount = "test@solaredu.app";

        // ─── Statement ───
        private SolarEduVerb selectedVerb = SolarEduVerb.Melihat;
        private string objectId = "solaredu://activity/test-panel";
        private string objectName = "Panel Surya Test";

        // ─── Score ───
        private bool includeScore = false;
        private float score = 75;
        private bool success = true;
        private bool completion = true;

        // ─── Status ───
        private string lastResponse = "";
        private string lastError = "";
        private bool isSending = false;

        // ─── Log ───
        private string logText = "";
        private Vector2 logScroll;

        [MenuItem("Tools/SolarEdu/Test Panel", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<SolarEduEditorWindow>("SolarEdu Test Panel");
            window.minSize = new Vector2(380, 600);
        }

        void OnGUI()
        {
            // ═══ HEADER ═══
            EditorGUILayout.Space(8);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
            };
            EditorGUILayout.LabelField("SolarEdu Analytics", headerStyle);
            EditorGUILayout.LabelField("xAPI Statement Test Panel", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(4);
            DrawLine();

            // ═══ CONFIG ═══
            configFoldout = EditorGUILayout.Foldout(configFoldout, "Konfigurasi", true, EditorStyles.foldoutHeader);
            if (configFoldout)
            {
                EditorGUI.indentLevel++;
                config = (SolarEduConfig)EditorGUILayout.ObjectField("Config", config, typeof(SolarEduConfig), false);

                if (config == null)
                {
                    EditorGUILayout.HelpBox("Drag SolarEduConfig ke sini.\nBuat via: Assets > Create > SolarEdu > Config", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField("API URL", config.apiUrl);
                    EditorGUILayout.LabelField("Endpoint", config.StatementsEndpoint);

                    if (!config.IsValid())
                    {
                        EditorGUILayout.HelpBox("API Key belum diisi di Config!", MessageType.Error);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Config OK", MessageType.Info);
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);
            DrawLine();

            // ═══ ACTOR ═══
            EditorGUILayout.LabelField("Actor (Siswa)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            actorName = EditorGUILayout.TextField("Nama", actorName);
            actorAccount = EditorGUILayout.TextField("Account", actorAccount);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);
            DrawLine();

            // ═══ VERB ═══
            EditorGUILayout.LabelField("Statement", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            selectedVerb = (SolarEduVerb)EditorGUILayout.EnumPopup("Aksi (Verb)", selectedVerb);

            var verbInfo = SolarEduVerbData.Get(selectedVerb);
            EditorGUILayout.LabelField("Verb URI", verbInfo.id, EditorStyles.miniLabel);

            EditorGUILayout.Space(4);
            objectId = EditorGUILayout.TextField("Object ID", objectId);
            objectName = EditorGUILayout.TextField("Object Name", objectName);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);
            DrawLine();

            // ═══ SCORE ═══
            includeScore = EditorGUILayout.ToggleLeft("  Sertakan Skor", includeScore, EditorStyles.boldLabel);
            if (includeScore)
            {
                EditorGUI.indentLevel++;
                score = EditorGUILayout.Slider("Skor", score, 0, 100);
                success = EditorGUILayout.Toggle("Berhasil", success);
                completion = EditorGUILayout.Toggle("Selesai", completion);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);
            DrawLine();

            // ═══ KIRIM BUTTON ═══
            EditorGUI.BeginDisabledGroup(config == null || !config.IsValid() || isSending);

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 40,
            };

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button(isSending ? "Mengirim..." : "Kirim Statement", buttonStyle))
            {
                SendTestStatement();
            }
            GUI.backgroundColor = Color.white;

            EditorGUI.EndDisabledGroup();

            // ═══ RESPONSE ═══
            if (!string.IsNullOrEmpty(lastResponse))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox($"Berhasil! Response:\n{lastResponse}", MessageType.Info);
            }
            if (!string.IsNullOrEmpty(lastError))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox($"Error:\n{lastError}", MessageType.Error);
            }

            // ═══ LOG ═══
            EditorGUILayout.Space(8);
            DrawLine();
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.Height(120));
            EditorGUILayout.TextArea(logText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Clear Log"))
            {
                logText = "";
                lastResponse = "";
                lastError = "";
            }
        }

        private void SendTestStatement()
        {
            if (config == null || !config.IsValid()) return;

            isSending = true;
            lastResponse = "";
            lastError = "";

            // Build JSON manually (no MonoBehaviour needed in Editor)
            string resultJson = "";
            if (includeScore)
            {
                string rawStr = score.ToString(CultureInfo.InvariantCulture);
                string scaledStr = (score / 100f).ToString("F4", CultureInfo.InvariantCulture);
                string successStr = success ? "true" : "false";
                string completionStr = completion ? "true" : "false";
                resultJson = $",\"result\":{{\"score\":{{\"raw\":{rawStr},\"min\":0,\"max\":100,\"scaled\":{scaledStr}}},\"success\":{successStr},\"completion\":{completionStr}}}";
            }

            var verbInfo = SolarEduVerbData.Get(selectedVerb);
            string ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            string json = "{" +
                "\"actor\":{" +
                    "\"name\":\"" + EscapeJson(actorName) + "\"," +
                    "\"account\":{" +
                        "\"homePage\":\"" + EscapeJson(config.actorHomePage) + "\"," +
                        "\"name\":\"" + EscapeJson(actorAccount) + "\"" +
                    "}" +
                "}," +
                "\"verb\":{" +
                    "\"id\":\"" + verbInfo.id + "\"," +
                    "\"display\":{\"id\":\"" + verbInfo.display + "\"}" +
                "}," +
                "\"object\":{" +
                    "\"id\":\"" + EscapeJson(objectId) + "\"," +
                    "\"definition\":{" +
                        "\"name\":{\"id\":\"" + EscapeJson(objectName) + "\"}" +
                    "}" +
                "}," +
                "\"timestamp\":\"" + ts + "\"" +
                resultJson +
            "}";

            AppendLog($"[{DateTime.Now:HH:mm:ss}] Mengirim: {SolarEduVerbData.Get(selectedVerb).display} → {objectName}");

            // Gunakan UnityWebRequest via EditorCoroutine
            var request = new UnityEngine.Networking.UnityWebRequest(config.StatementsEndpoint, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", config.GetAuthHeader());
            request.SetRequestHeader("X-Experience-API-Version", "1.0.3");
            request.timeout = 10;

            var operation = request.SendWebRequest();
            operation.completed += (op) =>
            {
                isSending = false;
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    lastResponse = request.downloadHandler.text;
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] Berhasil! ID: {lastResponse}");
                }
                else
                {
                    lastError = $"{request.error}\n{request.downloadHandler?.text}";
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] GAGAL: {lastError}");
                }
                request.Dispose();
                Repaint();
            };
        }

        private void AppendLog(string msg)
        {
            logText = msg + "\n" + logText;
            if (logText.Length > 5000) logText = logText.Substring(0, 5000);
        }

        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private void DrawLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(4);
        }
    }
}
#endif
