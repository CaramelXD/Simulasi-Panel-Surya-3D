using System;
using System.Collections.Generic;

/// <summary>
/// Snapshot data per jam selama simulasi berjalan.
/// Dicatat setiap kali jam berganti (misal 06:00, 07:00, dst.).
/// </summary>
[Serializable]
public class HourlySnapshot
{
    /// <summary>Jam in-game saat snapshot diambil (0-24).</summary>
    public int hour;

    /// <summary>Total daya yang dihasilkan panel surya saat snapshot (Watt).</summary>
    public float solarPowerWatt;

    /// <summary>Total beban elektronik saat snapshot (Watt).</summary>
    public float loadWatt;

    /// <summary>Sisa energi di baterai saat snapshot (Wh).</summary>
    public float batteryRemainingWh;

    /// <summary>Kapasitas maksimum baterai (Wh).</summary>
    public float batteryMaxWh;

    /// <summary>Detail beban elektronik: nama alat dan wattnya.</summary>
    public List<ApplianceEntry> appliances = new();

    /// <summary>Pesan peringatan/catatan untuk jam ini (bisa kosong).</summary>
    public List<string> warnings = new();
}

/// <summary>
/// Entry satu alat elektronik yang aktif saat snapshot.
/// </summary>
[Serializable]
public class ApplianceEntry
{
    public string name;
    public float watt;
    public int count;

    public ApplianceEntry(string name, float watt, int count = 1)
    {
        this.name = name;
        this.watt = watt;
        this.count = count;
    }
}
