using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject yang menyimpan daftar kode kelas yang valid.
/// Buat asset-nya via menu: Assets > Create > PanelSurya > Class Code Config.
/// Assign ke field ClassCodeConfig di LoginController.
/// </summary>
[CreateAssetMenu(menuName = "PanelSurya/Class Code Config", fileName = "ClassCodeConfig")]
public class ClassCodeConfig : ScriptableObject
{
    [Tooltip("Daftar kode kelas yang diizinkan untuk mendaftar.")]
    [SerializeField] private List<string> validCodes = new();

    /// <summary>
    /// Periksa apakah <paramref name="code"/> termasuk dalam daftar kode yang valid.
    /// Perbandingan bersifat case-insensitive dan mengabaikan spasi di awal/akhir.
    /// </summary>
    public bool IsValid(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        string trimmed = code.Trim();
        foreach (string valid in validCodes)
        {
            if (string.Equals(valid.Trim(), trimmed, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
