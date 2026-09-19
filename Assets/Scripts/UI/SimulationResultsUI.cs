using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SolarEdu;

/// <summary>
/// Menampilkan panel hasil simulasi saat simulasi berakhir.
/// Subscribe ke SimulationManager.OnSimulationEnded dan mengisi semua label stat.
/// Attach ke sembarang GameObject. Assign semua referensi UI via Inspector.
/// </summary>
public class SimulationResultsUI : MonoBehaviour
{
    private const string MainMenuSceneName = "Menu";
    private const float ResultsDelaySeconds = 3f;

    [Header("Panel")]
    [Tooltip("Root panel hasil simulasi — di-hide saat awal, muncul saat simulasi selesai.")]
    [SerializeField] private GameObject resultsPanel;

    [Header("Stat Values")]
    [SerializeField] private TextMeshProUGUI batteryCountValue;
    [SerializeField] private TextMeshProUGUI panelCountValue;
    [SerializeField] private TextMeshProUGUI furniturePowerValue;
    [SerializeField] private TextMeshProUGUI solarEnergyValue;
    [SerializeField] private TextMeshProUGUI energyConsumedValue;
    [SerializeField] private TextMeshProUGUI durationValue;
    [SerializeField] private TextMeshProUGUI powerStatusValue;

    [Header("Button")]
    [SerializeField] private Button exitButton;

    [Tooltip("Tombol untuk melanjutkan simulasi tanpa kembali ke menu.")]
    [SerializeField] private Button continueButton;

    [Tooltip("Tombol untuk membuka log per jam.")]
    [SerializeField] private Button viewLogButton;

    [Tooltip("Referensi HourlyHistoryUI untuk membuka panel log per jam.")]
    [SerializeField] private HourlyHistoryUI hourlyHistoryUI;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (SimulationManager.Instance != null)
            SimulationManager.Instance.OnSimulationEnded.AddListener(OnSimulationEnded);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToMenu);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        if (viewLogButton != null)
            viewLogButton.onClick.AddListener(OnViewLogClicked);
    }

    private void OnDestroy()
    {
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.OnSimulationEnded.RemoveListener(OnSimulationEnded);
    }

    // ── Handlers ──────────────────────────────────────────────────────────

    private void OnSimulationEnded()
    {
        PopulateStats();
        SaveToHistory();
        TrackSimulationResults();
        SendSimulationToServer();
        StartCoroutine(ShowResultsAfterDelay());
    }

    /// <summary>Kirim hasil simulasi ke SolarEdu xAPI dengan skor.</summary>
    private void TrackSimulationResults()
    {
        if (SolarEduManager.Instance == null) return;

        var bm = BatteryManager.Instance;
        var sm = SimulationManager.Instance;

        // Hitung skor sederhana: energi solar / energi consumed * 100
        float solarKwh    = bm != null ? bm.TotalSolarEnergyGenerated : 0f;
        float consumedKwh = bm != null ? bm.TotalEnergyConsumed : 0f;
        bool  sufficient  = bm != null && bm.WasPowerAlwaysSufficient;
        int   panelCount  = bm != null ? bm.PanelCount : 0;
        int   battCount   = bm != null ? bm.InstalledBatteryCount : 0;
        float duration    = sm != null ? sm.SimulationDurationHours : 0f;

        // Skor: 100 jika listrik cukup, proporsional jika tidak
        int score = sufficient ? 100 : (consumedKwh > 0 ? Mathf.Clamp((int)(solarKwh / consumedKwh * 100f), 0, 99) : 0);

        SolarEduManager.Instance.SendStatement(
            SolarEduVerb.Menyelesaikan,
            "solaredu://hasil-simulasi",
            $"Hasil: {solarKwh:F2}kWh solar, {consumedKwh:F2}kWh consumed, {panelCount} panel, {battCount} baterai, {duration}jam",
            new StatementResult
            {
                rawScore = score,
                minScore = 0,
                maxScore = 100,
                success = sufficient,
                completion = true,
            }
        );
    }

    /// <summary>Tunggu beberapa detik sebelum menampilkan panel hasil.</summary>
    private IEnumerator ShowResultsAfterDelay()
    {
        yield return new WaitForSeconds(ResultsDelaySeconds);

        if (resultsPanel != null)
            resultsPanel.SetActive(true);
    }

    // ── Stats ──────────────────────────────────────────────────────────────

    private void PopulateStats()
    {
        var bm = BatteryManager.Instance;
        var fm = FurnitureManager.Instance;
        var sm = SimulationManager.Instance;

        // Total baterai
        if (batteryCountValue != null)
            batteryCountValue.text = bm != null ? $"{bm.InstalledBatteryCount} unit" : "-";

        // Total panel surya
        if (panelCountValue != null)
            panelCountValue.text = bm != null ? $"{bm.PanelCount} panel" : "-";

        // Daya furnitur terpasang (instantaneous, bukan kumulatif)
        if (furniturePowerValue != null)
            furniturePowerValue.text = fm != null ? $"{fm.TotalWattConsumed:F0} W" : "-";

        // Total energi dihasilkan panel surya
        if (solarEnergyValue != null)
            solarEnergyValue.text = bm != null ? $"{bm.TotalSolarEnergyGenerated:F4} kWh" : "-";

        // Total energi yang keluar ke furnitur/perangkat
        if (energyConsumedValue != null)
            energyConsumedValue.text = bm != null ? $"{bm.TotalEnergyConsumed:F4} kWh" : "-";

        // Durasi simulasi in-game
        if (durationValue != null && sm != null)
        {
            float totalHours = sm.SimulationDurationHours;
            int h = Mathf.FloorToInt(totalHours);
            int m = Mathf.FloorToInt((totalHours - h) * 60f);
            durationValue.text = h > 0 ? $"{h} jam {m:00} menit" : $"{m} menit";
        }

        // Status kecukupan listrik
        if (powerStatusValue != null && bm != null)
        {
            bool sufficient = bm.WasPowerAlwaysSufficient;
            powerStatusValue.text  = sufficient ? "✓  Tercukupi" : "✗  Tidak tercukupi";
            powerStatusValue.color = sufficient
                ? new Color(0.18f, 0.8f, 0.44f)
                : new Color(0.9f, 0.25f, 0.25f);
        }
    }

    // ── History ──────────────────────────────────────────────────────────────

    /// <summary>Simpan hasil simulasi ke riwayat (PlayerPrefs), termasuk data log per jam.</summary>
    private void SaveToHistory()
    {
        var bm = BatteryManager.Instance;
        var fm = FurnitureManager.Instance;
        var sm = SimulationManager.Instance;
        var hl = SimulationHourlyLogger.Instance;

        var entry = new SimulationHistoryEntry
        {
            dateTime          = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            durationHours     = sm != null ? sm.SimulationDurationHours : 0f,
            solarPanelCount   = bm != null ? bm.PanelCount : 0,
            batteryCount      = bm != null ? bm.InstalledBatteryCount : 0,
            furnitureWatt     = fm != null ? fm.TotalWattConsumed : 0f,
            solarEnergyKwh    = bm != null ? bm.TotalSolarEnergyGenerated : 0f,
            energyConsumedKwh = bm != null ? bm.TotalEnergyConsumed : 0f,
            powerSufficient   = bm != null && bm.WasPowerAlwaysSufficient,
            startHour         = hl != null ? hl.StartHour : 0,
            initialBatteryWh  = hl != null ? hl.InitialBatteryWh : 0f,
            maxBatteryWh      = hl != null ? hl.MaxBatteryWh : 0f,
            hourlySnapshots   = hl != null ? new System.Collections.Generic.List<HourlySnapshot>(hl.Snapshots) : new(),
        };

        SimulationHistoryManager.Save(entry);
    }

    // ── Log Per Jam ───────────────────────────────────────────────────────

    /// <summary>Buka panel hourly log.</summary>
    private void OnViewLogClicked()
    {
        if (hourlyHistoryUI != null)
            hourlyHistoryUI.Show();
        else
            Debug.LogWarning("[SimulationResultsUI] hourlyHistoryUI belum di-assign!");

        // ── SolarEdu Tracking ──
        if (SolarEduManager.Instance != null)
        {
            SolarEduManager.Instance.SendStatement(
                SolarEduVerb.Mengamati,
                "solaredu://hasil-simulasi/log-per-jam",
                "Lihat Log Per Jam"
            );
        }
    }

    /// <summary>Tutup panel hasil dan kembalikan UI ke kondisi semula.</summary>
    private void OnContinueClicked()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (SimulationManager.Instance != null)
            SimulationManager.Instance.ExitResultsState();
    }

    // ── Navigation ─────────────────────────────────────────────────────────

    /// <summary>Muat scene menu utama.</summary>
    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    // ── Send to Server ─────────────────────────────────────────────────────

    /// <summary>Kirim data simulasi lengkap (session + hourly logs) ke dashboard API.</summary>
    private void SendSimulationToServer()
    {
        if (SolarEduManager.Instance == null || SolarEduManager.Instance.config == null) return;

        var bm = BatteryManager.Instance;
        var fm = FurnitureManager.Instance;
        var sm = SimulationManager.Instance;
        var hl = SimulationHourlyLogger.Instance;

        float solarKwh    = bm != null ? bm.TotalSolarEnergyGenerated : 0f;
        float consumedKwh = bm != null ? bm.TotalEnergyConsumed : 0f;
        bool  sufficient  = bm != null && bm.WasPowerAlwaysSufficient;
        int   score       = sufficient ? 100 : (consumedKwh > 0 ? Mathf.Clamp((int)(solarKwh / consumedKwh * 100f), 0, 99) : 0);

        // Build hourly logs array
        var hourlyLogs = new System.Collections.Generic.List<ServerHourlyLog>();
        if (hl != null)
        {
            foreach (var snap in hl.Snapshots)
            {
                var log = new ServerHourlyLog
                {
                    hour = snap.hour,
                    solarPowerWatt = snap.solarPowerWatt,
                    loadWatt = snap.loadWatt,
                    batteryRemainingWh = snap.batteryRemainingWh,
                    batteryMaxWh = snap.batteryMaxWh,
                };
                hourlyLogs.Add(log);
            }
        }

        // Build payload
        var payload = new ServerSimulationPayload
        {
            userId = SolarEduManager.Instance.actorAccountName,
            simulationDate = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            durationHours = sm != null ? sm.SimulationDurationHours : 0f,
            startHour = hl != null ? hl.StartHour : 0,
            solarPanelCount = bm != null ? bm.PanelCount : 0,
            batteryCount = bm != null ? bm.InstalledBatteryCount : 0,
            furnitureWatt = fm != null ? fm.TotalWattConsumed : 0f,
            initialBatteryWh = hl != null ? hl.InitialBatteryWh : 0f,
            maxBatteryWh = hl != null ? hl.MaxBatteryWh : 0f,
            solarEnergyKwh = solarKwh,
            energyConsumedKwh = consumedKwh,
            powerSufficient = sufficient,
            score = score,
        };

        // Serialize manually because JsonUtility can't handle List in nested object cleanly
        string logsJson = "[";
        for (int i = 0; i < hourlyLogs.Count; i++)
        {
            if (i > 0) logsJson += ",";
            logsJson += JsonUtility.ToJson(hourlyLogs[i]);
        }
        logsJson += "]";

        string baseJson = JsonUtility.ToJson(payload);
        // Insert hourlyLogs array before last '}'
        string fullJson = baseJson.TrimEnd('}') + ",\"hourlyLogs\":" + logsJson + "}";

        StartCoroutine(PostSimulationData(fullJson));
    }

    private IEnumerator PostSimulationData(string json)
    {
        var config = SolarEduManager.Instance.config;
        string url = config.apiUrl.TrimEnd('/') + "/api/simulations";

        var request = new UnityEngine.Networking.UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", config.GetAuthHeader());

        yield return request.SendWebRequest();

        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.Log($"[SolarEdu] Simulasi data terkirim! Response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"[SolarEdu] Gagal kirim simulasi data: {request.error} — {request.downloadHandler.text}");
        }
    }

    // ── Server payload classes ─────────────────────────────────────────────

    [System.Serializable]
    private class ServerSimulationPayload
    {
        public string userId;
        public string simulationDate;
        public float durationHours;
        public int startHour;
        public int solarPanelCount;
        public int batteryCount;
        public float furnitureWatt;
        public float initialBatteryWh;
        public float maxBatteryWh;
        public float solarEnergyKwh;
        public float energyConsumedKwh;
        public bool powerSufficient;
        public int score;
    }

    [System.Serializable]
    private class ServerHourlyLog
    {
        public int hour;
        public float solarPowerWatt;
        public float loadWatt;
        public float batteryRemainingWh;
        public float batteryMaxWh;
    }
}
