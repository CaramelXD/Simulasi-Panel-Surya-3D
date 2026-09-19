using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Merekam snapshot data simulasi setiap kali jam in-game berganti.
/// Menghasilkan daftar HourlySnapshot yang bisa diakses oleh UI setelah simulasi selesai.
/// </summary>
public class SimulationHourlyLogger : MonoBehaviour
{
    public static SimulationHourlyLogger Instance { get; private set; }

    /// <summary>Daftar snapshot per jam yang sudah direkam selama simulasi berjalan.</summary>
    public List<HourlySnapshot> Snapshots { get; private set; } = new();

    /// <summary>Jam awal simulasi (digunakan untuk header SYSTEM BOOT).</summary>
    public int StartHour { get; private set; }

    /// <summary>Kapasitas baterai awal saat simulasi dimulai (Wh).</summary>
    public float InitialBatteryWh { get; private set; }

    /// <summary>Kapasitas maksimum baterai (Wh).</summary>
    public float MaxBatteryWh { get; private set; }

    private const float LowBatteryThreshold = 0.3f;
    private const float CriticalBatteryThreshold = 0.05f;

    private int _lastRecordedHour = -1;
    private bool _isLogging;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.OnSimulationEnded.AddListener(OnSimulationEnded);
    }

    void OnDestroy()
    {
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.OnSimulationEnded.RemoveListener(OnSimulationEnded);
    }

    void Update()
    {
        if (!_isLogging) return;

        var sim = SimulationManager.Instance;
        var sun = FindFirstObjectByType<SunController>();

        if (sim == null || !sim.IsSimulationRunning || sun == null) return;

        int currentHour = Mathf.FloorToInt(sun.TimeOfDay);

        if (currentHour != _lastRecordedHour)
        {
            RecordSnapshot(currentHour);
            _lastRecordedHour = currentHour;
        }
    }

    /// <summary>
    /// Dipanggil dari luar (misal SimulationManager) saat simulasi dimulai
    /// untuk mereset state dan mulai merekam.
    /// </summary>
    public void BeginLogging(float startHour)
    {
        Snapshots.Clear();
        StartHour = Mathf.FloorToInt(startHour);
        _lastRecordedHour = -1;
        _isLogging = true;

        var bm = BatteryManager.Instance;
        if (bm != null)
        {
            // PERBAIKAN: Kalikan 1000 agar dari kWh menjadi Wh
            InitialBatteryWh = bm.CurrentCharge * 1000f;
            MaxBatteryWh = bm.MaxCapacity * 1000f;
        }

        Debug.Log($"[HourlyLogger] Mulai merekam dari JAM {StartHour:00}:00");
    }

    /// <summary>Rekam satu snapshot pada jam tertentu.</summary>
    private void RecordSnapshot(int hour)
    {
        var bm = BatteryManager.Instance;
        var fm = FurnitureManager.Instance;

        var snapshot = new HourlySnapshot
        {
            hour = hour,
            solarPowerWatt = bm != null ? bm.CurrentSolarPower : 0f,
            loadWatt = fm != null ? fm.GetActiveWattConsumed(hour) : 0f,

            // PERBAIKAN: Kalikan 1000 agar dari kWh menjadi Wh saat dicatat
            batteryRemainingWh = bm != null ? bm.CurrentCharge * 1000f : 0f,
            batteryMaxWh = bm != null ? bm.MaxCapacity * 1000f : 0f,
        };

        // Detail beban elektronik — kelompokkan yang sama
        if (fm != null)
        {
            var grouped = fm.GetPlacedFurnitureData()
                .GroupBy(f => new { f.furnitureName, f.wattConsumption })
                .Select(g => new ApplianceEntry(
                    g.Key.furnitureName,
                    g.Key.wattConsumption,
                    g.Count()))
                .ToList();

            snapshot.appliances = grouped;
        }

        // Peringatan berdasarkan level baterai
        if (bm != null)
        {
            float ratio = bm.ChargeRatio;

            if (bm.CurrentCharge <= bm.MaxCapacity * CriticalBatteryThreshold
                && fm != null && fm.GetActiveWattConsumed(hour) > 0f)
            {
                snapshot.warnings.Add("Kritis! Baterai habis. Energi terkuras, mengalami PEMADAMAN.");
            }
            else if (ratio < LowBatteryThreshold && ratio > CriticalBatteryThreshold)
            {
                snapshot.warnings.Add("Peringatan: Baterai < 30%. Deep-discharge dapat merusak sel penyimpan.");
            }

            // Surplus: solar > beban dan baterai penuh
            if (bm.CurrentCharge >= bm.MaxCapacity * 0.99f && snapshot.solarPowerWatt > snapshot.loadWatt)
            {
                snapshot.warnings.Add("Baterai penuh. Energi surplus dari panel surya tidak tersimpan.");
            }
        }

        Snapshots.Add(snapshot);
        Debug.Log($"[HourlyLogger] Snapshot JAM {hour:00}:00 — Solar: {snapshot.solarPowerWatt:F0}W, " +
                  $"Beban: {snapshot.loadWatt:F0}W, Bat: {snapshot.batteryRemainingWh:F0}Wh");
    }

    private void OnSimulationEnded()
    {
        _isLogging = false;
        Debug.Log($"[HourlyLogger] Selesai. Total {Snapshots.Count} snapshot terekam.");
    }
}