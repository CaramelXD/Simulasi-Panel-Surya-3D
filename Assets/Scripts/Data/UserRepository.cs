using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Model data untuk satu akun pengguna.
/// </summary>
[Serializable]
public class UserAccount
{
    public string username;
    public string email;
    public string passwordHash;
    public string classCode;
}

/// <summary>
/// Menyimpan, memuat, dan memverifikasi akun pengguna via PlayerPrefs (JSON).
/// Password di-hash dengan FNV-1a 32-bit sebelum disimpan.
/// </summary>
public static class UserRepository
{
    private const string AccountsPrefsKey = "UserAccounts";
    private const string SessionPrefsKey  = "LoggedInUser";

    [Serializable]
    private class AccountsWrapper
    {
        public List<UserAccount> accounts = new();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Daftarkan akun baru dengan kode kelas.
    /// Mengembalikan pesan error (string) jika gagal, atau null jika berhasil.
    /// </summary>
    public static string Register(string username, string email, string password, string classCode)
    {
        var accounts = LoadAll();

        if (accounts.Exists(a => string.Equals(a.username, username, StringComparison.OrdinalIgnoreCase)))
            return "Username sudah digunakan.";

        if (accounts.Exists(a => string.Equals(a.email, email, StringComparison.OrdinalIgnoreCase)))
            return "Email sudah terdaftar.";

        accounts.Add(new UserAccount
        {
            username     = username,
            email        = email,
            passwordHash = Hash(password),
            classCode    = classCode.Trim()
        });

        SaveAll(accounts);
        Debug.Log($"[UserRepository] Registered: {username} (kelas: {classCode})");
        return null;
    }

    /// <summary>
    /// Login dengan username dan password.
    /// Menyimpan sesi jika berhasil.
    /// Mengembalikan pesan error (string) jika gagal, atau null jika berhasil.
    /// </summary>
    public static string Login(string username, string password)
    {
        var accounts = LoadAll();
        var account  = accounts.Find(a => string.Equals(a.username, username, StringComparison.OrdinalIgnoreCase));

        if (account == null)
            return "Username tidak ditemukan.";

        if (account.passwordHash != Hash(password))
            return "Password salah.";

        PlayerPrefs.SetString(SessionPrefsKey, account.username);
        PlayerPrefs.Save();
        Debug.Log($"[UserRepository] Logged in: {username}");
        return null;
    }

    /// <summary>Mengembalikan username yang sedang aktif, atau null jika belum login.</summary>
    public static string GetCurrentUser() =>
        PlayerPrefs.HasKey(SessionPrefsKey) ? PlayerPrefs.GetString(SessionPrefsKey) : null;

    /// <summary>Hapus sesi login yang aktif.</summary>
    public static void Logout()
    {
        PlayerPrefs.DeleteKey(SessionPrefsKey);
        PlayerPrefs.Save();
        Debug.Log("[UserRepository] Logged out.");
    }

    // ── Internal helpers ───────────────────────────────────────────────────

    private static List<UserAccount> LoadAll()
    {
        if (!PlayerPrefs.HasKey(AccountsPrefsKey))
            return new List<UserAccount>();

        string json     = PlayerPrefs.GetString(AccountsPrefsKey);
        var    wrapper  = JsonUtility.FromJson<AccountsWrapper>(json);
        return wrapper?.accounts ?? new List<UserAccount>();
    }

    private static void SaveAll(List<UserAccount> accounts)
    {
        var wrapper = new AccountsWrapper { accounts = accounts };
        PlayerPrefs.SetString(AccountsPrefsKey, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    /// <summary>Hash ringan FNV-1a 32-bit — cukup untuk penyimpanan lokal.</summary>
    private static string Hash(string input)
    {
        uint hash = 2166136261u;
        foreach (char c in input)
        {
            hash ^= (uint)c;
            hash *= 16777619u;
        }
        return hash.ToString("X8");
    }
}
