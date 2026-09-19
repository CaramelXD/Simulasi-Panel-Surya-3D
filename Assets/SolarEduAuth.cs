// ============================================================================
// SolarEdu Analytics - Unity Plugin
// File: SolarEduAuth.cs
// Deskripsi: Handle Login & Register siswa dari Unity ke Dashboard.
//            Setelah login, actor di SolarEduManager otomatis ter-set.
// ============================================================================
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SolarEdu
{
    /// <summary>
    /// Data user setelah login berhasil
    /// </summary>
    [Serializable]
    public class SolarEduUser
    {
        public string id;
        public string name;
        public string email;
        public string role;
        public string orgName;
        public string className;
        public string classCode;
    }

    /// <summary>
    /// Komponen Auth — Login & Register siswa ke dashboard SolarEdu.
    /// Attach ke GameObject yang sama dengan SolarEduManager, atau standalone.
    /// </summary>
    [AddComponentMenu("SolarEdu/Auth")]
    public class SolarEduAuth : MonoBehaviour
    {
        [Header("═══ Config ═══")]
        [Tooltip("Drag SolarEduConfig ke sini")]
        public SolarEduConfig config;

        [Header("═══ Status ═══")]
        [Tooltip("User yang sedang login (null = belum login)")]
        public SolarEduUser currentUser;

        /// <summary>True jika sedang proses login/register</summary>
        public bool IsLoading { get; private set; }

        /// <summary>True jika user sudah login</summary>
        public bool IsLoggedIn => currentUser != null && !string.IsNullOrEmpty(currentUser.id);

        // ─── Events ───
        public static event Action<SolarEduUser> OnLoginSuccess;
        public static event Action<string> OnLoginFailed;
        public static event Action<SolarEduUser> OnRegisterSuccess;
        public static event Action<string> OnRegisterFailed;
        public static event Action OnLogout;

        // ═══════════════════════════════════════════════════
        //  LOGIN
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Login siswa dengan email dan password.
        /// Callback: onSuccess(user) atau onError(message)
        /// </summary>
        public void Login(string email, string password, Action<SolarEduUser> onSuccess = null, Action<string> onError = null)
        {
            if (config == null)
            {
                string err = "SolarEduConfig belum di-assign!";
                Debug.LogError($"[SolarEdu Auth] {err}");
                onError?.Invoke(err);
                return;
            }

            StartCoroutine(LoginCoroutine(email, password, onSuccess, onError));
        }

        private IEnumerator LoginCoroutine(string email, string password, Action<SolarEduUser> onSuccess, Action<string> onError)
        {
            IsLoading = true;
            string url = $"{config.apiUrl.TrimEnd('/')}/api/auth/unity-login";

            string json = "{" +
                "\"email\":\"" + EscapeJson(email) + "\"," +
                "\"password\":\"" + EscapeJson(password) + "\"" +
            "}";

            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15;

            Debug.Log($"[SolarEdu Auth] Login: {email}...");
            yield return request.SendWebRequest();

            IsLoading = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                var response = JsonUtility.FromJson<LoginResponse>(responseText);

                if (response.success && response.user != null)
                {
                    currentUser = new SolarEduUser
                    {
                        id = response.user.id,
                        name = response.user.name,
                        email = response.user.email,
                        role = response.user.role,
                        orgName = response.user.organization != null ? response.user.organization.name : "",
                        className = response.user.classes != null && response.user.classes.Length > 0
                            ? response.user.classes[0].name : "",
                        classCode = response.user.classes != null && response.user.classes.Length > 0
                            ? response.user.classes[0].code : "",
                    };

                    // Auto-set actor di SolarEduManager
                    if (SolarEduManager.Instance != null)
                    {
                        SolarEduManager.Instance.actorName = currentUser.name;
                        SolarEduManager.Instance.actorAccountName = currentUser.email;
                    }

                    // Save login untuk auto-login berikutnya
                    PlayerPrefs.SetString("solaredu_user_id", currentUser.id);
                    PlayerPrefs.SetString("solaredu_user_name", currentUser.name);
                    PlayerPrefs.SetString("solaredu_user_email", currentUser.email);
                    PlayerPrefs.SetString("solaredu_user_org", currentUser.orgName);
                    PlayerPrefs.SetString("solaredu_user_class", currentUser.className);
                    PlayerPrefs.Save();

                    Debug.Log($"[SolarEdu Auth] Login berhasil: {currentUser.name} ({currentUser.className})");
                    onSuccess?.Invoke(currentUser);
                    OnLoginSuccess?.Invoke(currentUser);
                }
                else
                {
                    string err = response.error ?? "Login gagal";
                    Debug.LogWarning($"[SolarEdu Auth] {err}");
                    onError?.Invoke(err);
                    OnLoginFailed?.Invoke(err);
                }
            }
            else
            {
                // Parse error message dari server
                string err = "Koneksi gagal";
                try
                {
                    var errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                    if (!string.IsNullOrEmpty(errorResponse.error)) err = errorResponse.error;
                }
                catch { err = request.error; }

                Debug.LogWarning($"[SolarEdu Auth] Login error: {err}");
                onError?.Invoke(err);
                OnLoginFailed?.Invoke(err);
            }

            request.Dispose();
        }

        // ═══════════════════════════════════════════════════
        //  REGISTER
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Register siswa baru dengan kode kelas.
        /// Callback: onSuccess(user) atau onError(message)
        /// </summary>
        public void Register(string name, string email, string password, string classCode,
            Action<SolarEduUser> onSuccess = null, Action<string> onError = null)
        {
            if (config == null)
            {
                string err = "SolarEduConfig belum di-assign!";
                Debug.LogError($"[SolarEdu Auth] {err}");
                onError?.Invoke(err);
                return;
            }

            StartCoroutine(RegisterCoroutine(name, email, password, classCode, onSuccess, onError));
        }

        private IEnumerator RegisterCoroutine(string name, string email, string password, string classCode,
            Action<SolarEduUser> onSuccess, Action<string> onError)
        {
            IsLoading = true;
            string url = $"{config.apiUrl.TrimEnd('/')}/api/register";

            string json = "{" +
                "\"name\":\"" + EscapeJson(name) + "\"," +
                "\"email\":\"" + EscapeJson(email) + "\"," +
                "\"password\":\"" + EscapeJson(password) + "\"," +
                "\"invitationCode\":\"" + EscapeJson(classCode) + "\"" +
            "}";

            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15;

            Debug.Log($"[SolarEdu Auth] Register: {email} dengan kode {classCode}...");
            yield return request.SendWebRequest();

            IsLoading = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<RegisterResponse>(request.downloadHandler.text);

                if (response.user != null)
                {
                    currentUser = new SolarEduUser
                    {
                        id = response.user.id,
                        name = response.user.name,
                        email = response.user.email,
                        role = "LEARNER",
                        orgName = response.organization ?? "",
                        className = response.className ?? "",
                        classCode = classCode.ToUpper(),
                    };

                    // Auto-set actor
                    if (SolarEduManager.Instance != null)
                    {
                        SolarEduManager.Instance.actorName = currentUser.name;
                        SolarEduManager.Instance.actorAccountName = currentUser.email;
                    }

                    // Save login
                    PlayerPrefs.SetString("solaredu_user_id", currentUser.id);
                    PlayerPrefs.SetString("solaredu_user_name", currentUser.name);
                    PlayerPrefs.SetString("solaredu_user_email", currentUser.email);
                    PlayerPrefs.SetString("solaredu_user_org", currentUser.orgName);
                    PlayerPrefs.SetString("solaredu_user_class", currentUser.className);
                    PlayerPrefs.Save();

                    Debug.Log($"[SolarEdu Auth] Register berhasil: {currentUser.name}");
                    onSuccess?.Invoke(currentUser);
                    OnRegisterSuccess?.Invoke(currentUser);
                }
                else
                {
                    string err = response.error ?? "Registrasi gagal";
                    Debug.LogWarning($"[SolarEdu Auth] {err}");
                    onError?.Invoke(err);
                    OnRegisterFailed?.Invoke(err);
                }
            }
            else
            {
                string err = "Koneksi gagal";
                try
                {
                    var errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                    if (!string.IsNullOrEmpty(errorResponse.error)) err = errorResponse.error;
                }
                catch { err = request.error; }

                Debug.LogWarning($"[SolarEdu Auth] Register error: {err}");
                onError?.Invoke(err);
                OnRegisterFailed?.Invoke(err);
            }

            request.Dispose();
        }

        // ═══════════════════════════════════════════════════
        //  LOGOUT & AUTO-LOGIN
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Logout — hapus data login tersimpan
        /// </summary>
        public void Logout()
        {
            currentUser = null;
            PlayerPrefs.DeleteKey("solaredu_user_id");
            PlayerPrefs.DeleteKey("solaredu_user_name");
            PlayerPrefs.DeleteKey("solaredu_user_email");
            PlayerPrefs.DeleteKey("solaredu_user_org");
            PlayerPrefs.DeleteKey("solaredu_user_class");
            PlayerPrefs.Save();

            Debug.Log("[SolarEdu Auth] Logout berhasil");
            OnLogout?.Invoke();
        }

        /// <summary>
        /// Cek apakah ada login tersimpan dari sesi sebelumnya.
        /// Jika ada, otomatis set currentUser dan actor.
        /// </summary>
        public bool TryAutoLogin()
        {
            string userId = PlayerPrefs.GetString("solaredu_user_id", "");
            if (string.IsNullOrEmpty(userId)) return false;

            currentUser = new SolarEduUser
            {
                id = userId,
                name = PlayerPrefs.GetString("solaredu_user_name", ""),
                email = PlayerPrefs.GetString("solaredu_user_email", ""),
                orgName = PlayerPrefs.GetString("solaredu_user_org", ""),
                className = PlayerPrefs.GetString("solaredu_user_class", ""),
            };

            // Auto-set actor
            if (SolarEduManager.Instance != null)
            {
                SolarEduManager.Instance.actorName = currentUser.name;
                SolarEduManager.Instance.actorAccountName = currentUser.email;
            }

            Debug.Log($"[SolarEdu Auth] Auto-login: {currentUser.name}");
            return true;
        }

        // ═══════════════════════════════════════════════════
        //  JSON RESPONSE CLASSES
        // ═══════════════════════════════════════════════════

        [Serializable] private class LoginResponse
        {
            public bool success;
            public string error;
            public LoginUser user;
        }

        [Serializable] private class LoginUser
        {
            public string id;
            public string name;
            public string email;
            public string role;
            public OrgInfo organization;
            public ClassInfo[] classes;
        }

        [Serializable] private class OrgInfo
        {
            public string id;
            public string name;
        }

        [Serializable] private class ClassInfo
        {
            public string id;
            public string name;
            public string code;
        }

        [Serializable] private class RegisterResponse
        {
            public string message;
            public string error;
            public RegisterUser user;
            public string className;
            public string organization;
        }

        [Serializable] private class RegisterUser
        {
            public string id;
            public string name;
            public string email;
        }

        [Serializable] private class ErrorResponse
        {
            public string error;
        }

        // ═══════════════════════════════════════════════════
        //  UTIL
        // ═══════════════════════════════════════════════════

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
    }
}
