using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton yang mengatur aliran energi:
///
///   Solar Panels → [EnergyPool] → Battery (dicapped oleh MaxChargeRate)
///
/// EnergyPool naik setiap frame dari output semua panel terdaftar.
/// Battery mengambil dari pool dengan kecepatan maksimum MaxChargeRate (kWh/jam-sim).
/// Jika pool kosong, battery tidak bisa mengisi walau ada panel.
/// Furnitur menguras battery langsung (bukan dari pool).
/// </summary>
public class BatteryManager : MonoBehaviour
{
    public static BatteryManager Instance { get; private set; }

    // ── Events ─────────────────────────────────────────────────────────────
    /// <summary>Dipanggil setiap charge battery berubah. Parameter: currentCharge (kWh).</summary>
    public UnityEvent<float> OnChargeChanged;

    /// <summary>Dipanggil saat state daya berubah (ada daya / tidak ada daya).</summary>
    public UnityEvent<bool> OnPowerStateChanged;

    /// <summary>Dipanggil saat jumlah panel yang terdaftar berubah.</summary>
    public UnityEvent OnPanelCountChanged;

    /// <summary>Dipanggil setiap total energi yang terpakai furnitur berubah. Parameter: total kWh.</summary>
    public UnityEvent<float> OnEnergyConsumedChanged;

    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Battery Specs")]
    [Tooltip("Masukkan data baterai (Batt 10, Batt 15, atau Batt 20) ke sini.")]
    [SerializeField] private BattData activeBatteryData;

    [Tooltip("Kecepatan maksimum pengisian battery dari pool (kWh per jam simulasi).")]
    [SerializeField] private float maxChargeRate = 0.5f;

    [Tooltip("Ambang batas minimum charge sebagai rasio kapasitas (0–1) sebelum dianggap kosong. Default 0.01 = 1% kapasitas.")]
    [SerializeField] private float emptyThresholdRatio = 0.01f;

    [Tooltip("Efisiensi inverter untuk konversi DC ke AC (0-1). Default 0.90 = 90%.")]
    [SerializeField] private float inverterEfficiency = 0.90f;

    // ── State ──────────────────────────────────────────────────────────────
    public float CurrentCharge { get; private set; } = 0f;

    // Kapasitas maksimum sekarang membaca nilai ini, bukan angka statis di Inspector
    private float _currentMaxCapacity = 0f;

    public float MaxCapacity => _currentMaxCapacity;
    public float MaxChargeRate => maxChargeRate;

    /// <summary>
    /// Pool energi yang diisi panel surya dan dikuras oleh battery.
    /// Ini yang ditampilkan sebagai "Total Energy" di UI.
    /// </summary>
    public float EnergyPool { get; private set; } = 0f;

    /// <summary>Total energi kumulatif yang dikonsumsi furnitur sejak simulasi dimulai (kWh).</summary>
    public float TotalEnergyConsumed { get; private set; } = 0f;

    /// <summary>Total energi kumulatif yang dihasilkan semua panel surya selama simulasi (kWh).</summary>
    public float TotalSolarEnergyGenerated { get; private set; } = 0f;

    /// <summary>
    /// True jika selama simulasi daya tidak pernah habis saat furnitur aktif.
    /// Mengembalikan false jika baterai pernah habis setelah sempat terisi,
    /// atau jika furnitur aktif tetapi baterai tidak pernah terisi sama sekali.
    /// </summary>
    public bool WasPowerAlwaysSufficient
    {
        get
        {
            if (_powerEverInsufficient) return false;
            // Baterai tidak pernah terisi padahal furnitur aktif → tidak mencukupi
            if (_furnitureWasEverActive && !_powerWasEverSufficient) return false;
            return true;
        }
    }

    private bool _powerEverInsufficient = false;

    /// <summary>True setelah baterai pernah berada di atas threshold (HasPower = true).</summary>
    private bool _powerWasEverSufficient = false;

    /// <summary>True jika furnitur pernah aktif selama simulasi berjalan.</summary>
    private bool _furnitureWasEverActive = false;

    /// <summary>Akumulasi jam simulasi yang sudah berlalu sejak simulasi dimulai.</summary>
    private float _simElapsedHours = 0f;

    /// <summary>
    /// Grace period (jam simulasi) sebelum pengecekan kecukupan daya dimulai.
    /// Hanya digunakan bersama _powerWasEverSufficient agar false-positive di awal simulasi
    /// (saat baterai masih dalam fase pengisian awal) tidak ikut ter-flag.
    /// </summary>
    private const float SufficiencyGracePeriodHours = 0.05f;

    /// <summary>Persentase charge saat ini (0-1).</summary>
    public float ChargeRatio => _currentMaxCapacity > 0f ? CurrentCharge / _currentMaxCapacity : 0f;

    /// <summary>True jika battery masih punya daya di atas threshold.</summary>
    public bool HasPower => CurrentCharge > _currentMaxCapacity * emptyThresholdRatio;

    /// <summary>Jumlah solar panel yang saat ini terdaftar.</summary>
    public int PanelCount => _registeredPanels.Count;

    /// <summary>Total output daya semua panel surya yang terdaftar saat ini (Watt).</summary>
    public float CurrentSolarPower { get; private set; } = 0f;

    /// <summary>Output AC saat ini (Watt) = daya DC x efisiensi inverter.</summary>
    public float CurrentACOutput { get; private set; } = 0f;

    /// <summary>Efisiensi inverter DC-AC (0-1).</summary>
    public float InverterEfficiency => inverterEfficiency;

    private bool _lastHasPower = false;
    private readonly List<SolarPanel> _registeredPanels = new();
    private SunController _sunController;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Set kapasitas awal dari data baterai di Inspector
        if (activeBatteryData != null)
        {
            _currentMaxCapacity = Mathf.Max(0.1f, activeBatteryData.capacity);
        }

        CurrentCharge = 0f;
        EnergyPool = 0f;
        TotalEnergyConsumed = 0f;
        TotalSolarEnergyGenerated = 0f;
        _powerEverInsufficient = false;
        _powerWasEverSufficient = false;
        _furnitureWasEverActive = false;
        _simElapsedHours = 0f;
        _lastHasPower = false;
    }

    private void Start()
    {
        _sunController = FindFirstObjectByType<SunController>();

        // Auto-discover panel yang sudah ada di scene
        foreach (SolarPanel panel in FindObjectsByType<SolarPanel>(FindObjectsSortMode.None))
            RegisterPanel(panel);
    }

    private void Update()
    {
        // Selalu perbarui CurrentSolarPower agar kabel bisa bereaksi meski simulasi belum jalan
        float solarInput = 0f;
        foreach (SolarPanel panel in _registeredPanels)
            if (panel != null) solarInput += panel.CurrentPower;
        CurrentSolarPower = solarInput;

        // Output AC = daya DC x efisiensi inverter
        CurrentACOutput = solarInput * inverterEfficiency;

        if (SimulationManager.Instance == null || !SimulationManager.Instance.IsSimulationRunning)
            return;

        float simHoursPerSec = _sunController != null ? _sunController.SimulationHoursPerSecond : 1f;
        float deltaSimHours = simHoursPerSec * Time.deltaTime;

        _simElapsedHours += deltaSimHours;

        TotalSolarEnergyGenerated += solarInput * deltaSimHours / 1000f;

        float currentSimHour = _sunController != null ? _sunController.TimeOfDay : 0f;
        float activeWatt = FurnitureManager.Instance != null ? FurnitureManager.Instance.GetActiveWattConsumed(currentSimHour) : 0f;

        float actualDrainWatt = inverterEfficiency > 0f ? (activeWatt / inverterEfficiency) : activeWatt;

        if (actualDrainWatt > 0f)
        {
            TotalEnergyConsumed += (actualDrainWatt * deltaSimHours / 1000f);
            OnEnergyConsumedChanged?.Invoke(TotalEnergyConsumed);
        }

        float netWatt = solarInput - actualDrainWatt;
        float netEnergy_kWh = netWatt * deltaSimHours / 1000f;

        float prevCharge = CurrentCharge;

        if (netEnergy_kWh > 0f)
        {
            float maxChargeThisFrame = maxChargeRate * deltaSimHours;
            float rawChargeToAdd = Mathf.Min(netEnergy_kWh, maxChargeThisFrame);
            float actualChargeToAdd = rawChargeToAdd * 0.92f;

            // Menggunakan _currentMaxCapacity dari BattData
            CurrentCharge = Mathf.Clamp(CurrentCharge + actualChargeToAdd, 0f, _currentMaxCapacity);
            EnergyPool = CurrentCharge;
        }
        else if (netEnergy_kWh < 0f)
        {
            // Menggunakan _currentMaxCapacity dari BattData
            CurrentCharge = Mathf.Clamp(CurrentCharge + netEnergy_kWh, 0f, _currentMaxCapacity);
            EnergyPool = CurrentCharge;
        }

        if (HasPower)
            _powerWasEverSufficient = true;

        float currentSimHourForCheck = _sunController != null ? _sunController.TimeOfDay : 0f;
        if (FurnitureManager.Instance != null
            && FurnitureManager.Instance.GetActiveWattConsumed(currentSimHourForCheck) > 0f)
        {
            _furnitureWasEverActive = true;
        }

        if (_simElapsedHours >= SufficiencyGracePeriodHours
            && _powerWasEverSufficient
            && !HasPower
            && FurnitureManager.Instance != null
            && FurnitureManager.Instance.GetActiveWattConsumed(currentSimHourForCheck) > 0f)
        {
            _powerEverInsufficient = true;
        }

        if (!Mathf.Approximately(CurrentCharge, prevCharge))
            OnChargeChanged?.Invoke(CurrentCharge);

        bool currentHasPower = HasPower;
        if (currentHasPower != _lastHasPower)
        {
            _lastHasPower = currentHasPower;
            OnPowerStateChanged?.Invoke(currentHasPower);
        }
    }

    // ── BattData Integration ───────────────────────────────────────────────

    /// <summary>
    /// Panggil fungsi ini jika ingin mengganti baterai (misal: dari 10 ke 15 kWh) saat game berjalan.
    /// </summary>
    public void SetBatteryData(BattData newBattData)
    {
        if (newBattData == null) return;

        activeBatteryData = newBattData;

        // Ambil kapasitas langsung dari file BattData
        _currentMaxCapacity = Mathf.Max(0.1f, newBattData.capacity);

        // Sesuaikan isi baterai jika kapasitas barunya lebih kecil dari isi baterai saat ini
        CurrentCharge = Mathf.Clamp(CurrentCharge, 0f, _currentMaxCapacity);
        EnergyPool = CurrentCharge;

        OnChargeChanged?.Invoke(CurrentCharge);

        Debug.Log($"[BatteryManager] Baterai sekarang: {newBattData.battName} dengan kapasitas {_currentMaxCapacity} kWh");
    }

    // ── Consumption Reset ──────────────────────────────────────────────────

    public void ResetConsumption()
    {
        CurrentCharge = 0f;
        EnergyPool = 0f;
        TotalEnergyConsumed = 0f;
        TotalSolarEnergyGenerated = 0f;
        _powerEverInsufficient = false;
        _powerWasEverSufficient = false;
        _furnitureWasEverActive = false;
        _simElapsedHours = 0f;

        foreach (SolarPanel panel in _registeredPanels)
            if (panel != null) panel.ResetTotalEnergy();

        OnEnergyConsumedChanged?.Invoke(TotalEnergyConsumed);
        OnChargeChanged?.Invoke(CurrentCharge);
    }

    // ── Battery Count ──────────────────────────────────────────────────────

    private int _installedBatteryCount = 0;
    public int InstalledBatteryCount => _installedBatteryCount;
    public UnityEvent<int> OnBatteryCountChanged;

    public void NotifyBatteryInstalled()
    {
        _installedBatteryCount++;
        OnBatteryCountChanged?.Invoke(_installedBatteryCount);
    }

    public void NotifyBatteryRemoved()
    {
        _installedBatteryCount = Mathf.Max(0, _installedBatteryCount - 1);
        OnBatteryCountChanged?.Invoke(_installedBatteryCount);
    }

    // ── Panel Registration ─────────────────────────────────────────────────

    public void RegisterPanel(SolarPanel panel)
    {
        if (panel == null || _registeredPanels.Contains(panel)) return;
        _registeredPanels.Add(panel);
        if (panel.TotalEnergy > 0f) EnergyPool += panel.TotalEnergy;
        OnPanelCountChanged?.Invoke();
    }

    public void UnregisterPanel(SolarPanel panel)
    {
        _registeredPanels.Remove(panel);
        OnPanelCountChanged?.Invoke();
    }
}