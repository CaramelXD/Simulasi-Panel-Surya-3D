using System;
using System.Collections.Generic;

/// <summary>
/// Data satu sesi simulasi yang disimpan ke history.
/// </summary>
[Serializable]
public class SimulationHistoryEntry
{
    /// <summary>Tanggal dan waktu nyata saat simulasi dijalankan (ISO 8601).</summary>
    public string dateTime;

    /// <summary>Durasi simulasi dalam jam in-game.</summary>
    public float durationHours;

    /// <summary>Jumlah panel surya yang terpasang.</summary>
    public int solarPanelCount;

    /// <summary>Jumlah baterai yang terpasang.</summary>
    public int batteryCount;

    /// <summary>Total daya furnitur terpasang (Watt).</summary>
    public float furnitureWatt;

    /// <summary>Total energi yang dihasilkan panel surya (kWh).</summary>
    public float solarEnergyKwh;

    /// <summary>Total energi yang dikonsumsi furnitur (kWh).</summary>
    public float energyConsumedKwh;

    /// <summary>True jika daya selalu tercukupi selama simulasi.</summary>
    public bool powerSufficient;

    // ── Hourly Log Data ───────────────────────────────────────────────────

    /// <summary>Jam awal simulasi (untuk header SYSTEM BOOT).</summary>
    public int startHour;

    /// <summary>Kapasitas baterai awal saat simulasi dimulai (Wh).</summary>
    public float initialBatteryWh;

    /// <summary>Kapasitas maksimum baterai (Wh).</summary>
    public float maxBatteryWh;

    /// <summary>Snapshot data per jam selama simulasi berjalan.</summary>
    public List<HourlySnapshot> hourlySnapshots = new();
}
