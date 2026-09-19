using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menampilkan log simulasi per jam dalam format terminal/console.
/// Ditampilkan di panel hasil simulasi setelah simulasi selesai.
/// </summary>
public class HourlyHistoryUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Root panel yang berisi seluruh tampilan hourly log.")]
    [SerializeField] private GameObject logPanel;

    [Tooltip("Parent content di dalam ScrollView untuk menampung teks log.")]
    [SerializeField] private Transform contentParent;

    [Tooltip("Button untuk menutup panel log.")]
    [SerializeField] private Button closeButton;

    // ── Warna Terminal ────────────────────────────────────────────────────
    private static readonly Color BgColor = new(0.08f, 0.08f, 0.12f, 1f);
    private static readonly Color HeaderCyan = new(0.2f, 0.9f, 0.9f, 1f);
    private static readonly Color InitWhite = new(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Color HourGreen = new(0.4f, 1f, 0.4f, 1f);
    private static readonly Color HourYellow = new(1f, 0.9f, 0.3f, 1f);
    private static readonly Color ValueWhite = Color.white;
    private static readonly Color ValueBold = new(1f, 1f, 1f, 1f);
    private static readonly Color WarnYellow = new(1f, 0.85f, 0.3f, 1f);
    private static readonly Color WarnRed = new(1f, 0.35f, 0.3f, 1f);
    private static readonly Color DimGray = new(0.5f, 0.5f, 0.5f, 1f);
    private static readonly Color LabelGreen = new(0.4f, 0.85f, 0.4f, 1f);

    private const int FontSizeNormal = 16;
    private const int FontSizeHeader = 18;
    private const int FontSizeSmall = 14;
    private const float LineSpacing = 4f;

    void Start()
    {
        if (logPanel != null)
            logPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    /// <summary>Tampilkan panel log.</summary>
    public void Show()
    {
        BuildLog();
        if (logPanel != null)
            logPanel.SetActive(true);
    }

    /// <summary>Sembunyikan panel log.</summary>
    public void Hide()
    {
        if (logPanel != null)
            logPanel.SetActive(false);
    }

    /// <summary>Bangun ulang konten log dari data SimulationHourlyLogger.</summary>
    private void BuildLog()
    {
        if (contentParent == null) return;

        // Bersihkan konten lama
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        var logger = SimulationHourlyLogger.Instance;
        if (logger == null || logger.Snapshots.Count == 0)
        {
            AddLogLine("Belum ada data simulasi.", InitWhite, FontSizeNormal);
            return;
        }

        // ── SYSTEM BOOT ────────────────────────────────────────────────────
        AddLogLine(
            $">>> SYSTEM BOOT... Memuat Kalkulasi Parameter dari JAM {logger.StartHour:00}:00",
            HeaderCyan, FontSizeHeader, FontStyles.Bold);

        float initPercent = logger.MaxBatteryWh > 0
            ? (logger.InitialBatteryWh / logger.MaxBatteryWh) * 100f
            : 0f;

        AddLogLine(
            $"[INIT] Baterai Awal disetel: {logger.InitialBatteryWh:F0} Wh ({initPercent:F0}%)",
            InitWhite, FontSizeNormal);

        AddSpacer();

        // ── Per jam entries ────────────────────────────────────────────────
        for (int i = 0; i < logger.Snapshots.Count; i++)
        {
            HourlySnapshot snap = logger.Snapshots[i];
            BuildHourBlock(snap);

            if (i < logger.Snapshots.Count - 1)
                AddSpacer();
        }
    }

    /// <summary>Bangun satu blok jam di log.</summary>
    private void BuildHourBlock(HourlySnapshot snap)
    {
        // Header jam
        bool hasWarning = snap.warnings.Count > 0;
        Color hourColor = hasWarning ? HourYellow : HourGreen;
        AddLogLine($"[HISTORY] JAM {snap.hour:00}:00", hourColor, FontSizeHeader, FontStyles.Bold);

        // Daya surya
        AddRichLine($"> P (Surya) : <b>{snap.solarPowerWatt:F0} W</b>");

        // Beban elektronik dengan detail
        string loadDetail = BuildLoadDetail(snap);
        AddRichLine($"> E (Beban) : {loadDetail}");

        // Sisa baterai
        Color batColor = GetBatteryColor(snap);
        AddLogLine($"> Sisa Bat : {snap.batteryRemainingWh:F0} Wh", batColor, FontSizeNormal, FontStyles.Bold);

        // Peringatan
        foreach (string warning in snap.warnings)
        {
            bool isCritical = warning.Contains("Kritis");
            Color warnColor = isCritical ? WarnRed : WarnYellow;
            string tag = isCritical ? "Sains" : "Engineering";

            AddLogLine($"  [{tag}]  {warning}", warnColor, FontSizeSmall, FontStyles.Italic);
        }
    }

    /// <summary>Bangun string detail beban elektronik.</summary>
    private string BuildLoadDetail(HourlySnapshot snap)
    {
        if (snap.appliances == null || snap.appliances.Count == 0)
            return "[ - ] = 0 W";

        var parts = new List<string>();
        foreach (ApplianceEntry app in snap.appliances)
        {
            if (app.count > 1)
                parts.Add($"{app.name} ({app.count}x{app.watt:F0}W)");
            else
                parts.Add($"{app.name} ({app.watt:F0}W)");
        }

        string detail = string.Join(", ", parts);
        return $"[ {detail} ] = <b>{snap.loadWatt:F0} W</b>";
    }

    /// <summary>Tentukan warna teks sisa baterai berdasarkan level.</summary>
    private Color GetBatteryColor(HourlySnapshot snap)
    {
        if (snap.batteryMaxWh <= 0) return ValueWhite;

        float ratio = snap.batteryRemainingWh / snap.batteryMaxWh;

        if (ratio <= 0.05f) return WarnRed;
        if (ratio < 0.3f) return WarnYellow;
        return LabelGreen;
    }

    // ── UI Helpers ─────────────────────────────────────────────────────────

    /// <summary>Tambahkan satu baris teks ke konten log.</summary>
    private TextMeshProUGUI AddLogLine(string text, Color color, int fontSize,
        FontStyles style = FontStyles.Normal)
    {
        GameObject lineGo = new GameObject("LogLine", typeof(RectTransform));
        lineGo.transform.SetParent(contentParent, false);

        var tmp = lineGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        var le = lineGo.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;

        // Force content size
        var csf = lineGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return tmp;
    }

    /// <summary>Tambahkan satu baris teks rich (dengan tag TMP) ke konten log.</summary>
    private void AddRichLine(string richText)
    {
        var tmp = AddLogLine(richText, ValueWhite, FontSizeNormal);
        tmp.richText = true;
    }

    /// <summary>Tambahkan spacer kosong antar blok jam.</summary>
    private void AddSpacer()
    {
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(contentParent, false);

        var le = spacer.AddComponent<LayoutElement>();
        le.preferredHeight = 12f;
        le.flexibleWidth = 1f;
    }
}
