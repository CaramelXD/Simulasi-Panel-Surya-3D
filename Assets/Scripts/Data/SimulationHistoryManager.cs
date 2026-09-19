using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Menyimpan dan memuat riwayat simulasi via PlayerPrefs (JSON).
/// </summary>
public static class SimulationHistoryManager
{
    private const string PrefsKey = "SimulationHistory";
    private const int MaxEntries = 50;

    [System.Serializable]
    private class HistoryWrapper
    {
        public List<SimulationHistoryEntry> entries = new();
    }

    /// <summary>Simpan satu entry baru ke riwayat.</summary>
    public static void Save(SimulationHistoryEntry entry)
    {
        var list = LoadAll();
        list.Insert(0, entry);

        if (list.Count > MaxEntries)
            list.RemoveRange(MaxEntries, list.Count - MaxEntries);

        SaveList(list);
        Debug.Log($"[SimulationHistory] Saved. Total entries: {list.Count}");
    }

    /// <summary>Muat semua entry riwayat (terbaru di index 0).</summary>
    public static List<SimulationHistoryEntry> LoadAll()
    {
        if (!PlayerPrefs.HasKey(PrefsKey))
            return new List<SimulationHistoryEntry>();

        string json = PlayerPrefs.GetString(PrefsKey);
        var wrapper = JsonUtility.FromJson<HistoryWrapper>(json);
        return wrapper?.entries ?? new List<SimulationHistoryEntry>();
    }

    /// <summary>Hapus satu entry berdasarkan index.</summary>
    public static void DeleteAt(int index)
    {
        var list = LoadAll();
        if (index < 0 || index >= list.Count) return;

        list.RemoveAt(index);
        SaveList(list);
        Debug.Log($"[SimulationHistory] Deleted entry at index {index}. Remaining: {list.Count}");
    }

    /// <summary>Hapus semua riwayat.</summary>
    public static void ClearAll()
    {
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
        Debug.Log("[SimulationHistory] History cleared.");
    }

    private static void SaveList(List<SimulationHistoryEntry> list)
    {
        var wrapper = new HistoryWrapper { entries = list };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(PrefsKey, json);
        PlayerPrefs.Save();
    }
}
