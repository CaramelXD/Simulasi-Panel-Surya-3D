using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menampilkan riwayat simulasi di scene Menu.
/// Terdiri dari tiga tampilan:
/// 1. ListView   — daftar entry (tanggal + jam) dengan tombol Lihat dan Hapus.
/// 2. DetailView — tampilan hasil simulasi (mirip SimulationResultPanel di scene Simulasi).
/// 3. HourlyLog  — log terminal per jam dari data yang tersimpan.
/// 4. DeleteConfirm - Panel konfirmasi penghapusan data.
/// </summary>
public class SimulationHistoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private TextMeshProUGUI emptyText;
    [SerializeField] private Button clearButton;

    // --- FITUR BARU: Konfirmasi Hapus ---
    [Header("Konfirmasi Hapus (Sistem AutoWire)")]
    [Tooltip("Jika otomatis gagal, kamu bisa drag-and-drop manual objeknya ke sini")]
    [SerializeField] private GameObject _deleteConfirmPanel;
    [SerializeField] private Button _btnConfirmHapus;
    [SerializeField] private Button _btnConfirmBatal;
    [SerializeField] private TextMeshProUGUI _txtConfirmMessage;

    private enum DeleteTarget { None, All, Single }
    private DeleteTarget _deleteTarget = DeleteTarget.None;
    private int _deleteIndex = -1;

    // ── Warna Desain (match SimulationResultPanel) ─────────────────────────
    private static readonly Color OverlayBg = new Color(0.05f, 0.05f, 0.10f, 1f);
    private static readonly Color CardBg = new Color(0.10f, 0.14f, 0.20f, 1f);
    private static readonly Color RowItemBg = new Color(0.12f, 0.13f, 0.17f, 1f);
    private static readonly Color BtnLihatColor = new Color(0.17f, 0.55f, 0.85f, 1f);
    private static readonly Color BtnHapusColor = new Color(0.75f, 0.22f, 0.22f, 1f);
    private static readonly Color BtnBackColor = new Color(0.17f, 0.55f, 0.85f, 1f);
    private static readonly Color BtnLogColor = new Color(0.20f, 0.45f, 0.65f, 1f);
    private static readonly Color AccentGreen = new Color(0.18f, 0.80f, 0.44f, 1f);
    private static readonly Color AccentRed = new Color(0.90f, 0.25f, 0.25f, 1f);
    private static readonly Color TextWhite = Color.white;
    private static readonly Color TextMuted = new Color(0.55f, 0.58f, 0.65f, 1f);
    private static readonly Color LabelColor = new Color(0.65f, 0.68f, 0.75f, 1f);

    // Warna terminal hourly log
    private static readonly Color TermBg = new Color(0.08f, 0.08f, 0.12f, 1f);
    private static readonly Color HeaderCyan = new Color(0.2f, 0.9f, 0.9f, 1f);
    private static readonly Color InitWhite = new Color(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Color HourGreen = new Color(0.4f, 1f, 0.4f, 1f);
    private static readonly Color HourYellow = new Color(1f, 0.9f, 0.3f, 1f);
    private static readonly Color WarnYellow = new Color(1f, 0.85f, 0.3f, 1f);
    private static readonly Color WarnRed = new Color(1f, 0.35f, 0.3f, 1f);
    private static readonly Color LabelGreen = new Color(0.4f, 0.85f, 0.4f, 1f);

    private const int FontSizeNormal = 16;
    private const int FontSizeHeader = 18;
    private const int FontSizeSmall = 14;

    // Detail overlay
    private GameObject _detailOverlay;
    private TextMeshProUGUI _detailTitle;
    private TextMeshProUGUI _detailDuration;
    private TextMeshProUGUI _detailPanels;
    private TextMeshProUGUI _detailBatteries;
    private TextMeshProUGUI _detailFurniture;
    private TextMeshProUGUI _detailSolar;
    private TextMeshProUGUI _detailConsumed;
    private TextMeshProUGUI _detailStatus;

    // Hourly log overlay
    private GameObject _hourlyOverlay;
    private Transform _hourlyContentParent;

    // Data sementara untuk tombol log per jam
    private SimulationHistoryEntry _currentDetailEntry;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        AutoWire();

        // Tombol Hapus Riwayat sekarang memanggil konfirmasi, bukan langsung hapus
        if (clearButton != null)
            clearButton.onClick.AddListener(PromptDeleteAll);

        // Sambungkan tombol di panel konfirmasi
        if (_btnConfirmHapus != null) _btnConfirmHapus.onClick.AddListener(ExecuteDelete);
        if (_btnConfirmBatal != null) _btnConfirmBatal.onClick.AddListener(CloseDeleteConfirm);

        // Pastikan panel konfirmasi mati saat game mulai
        if (_deleteConfirmPanel != null) _deleteConfirmPanel.SetActive(false);

        BuildDetailOverlay();
        BuildHourlyOverlay();
        Rebuild();
    }

    private void OnEnable()
    {
        if (contentParent != null)
            Rebuild();
    }

    private void OnDisable()
    {
        if (_detailOverlay != null) _detailOverlay.SetActive(false);
        if (_hourlyOverlay != null) _hourlyOverlay.SetActive(false);
        CloseDeleteConfirm();
    }

    private void OnDestroy()
    {
        if (_detailOverlay != null) Destroy(_detailOverlay);
        if (_hourlyOverlay != null) Destroy(_hourlyOverlay);
    }

    private void AutoWire()
    {
        Transform root = transform;

        if (contentParent == null)
        {
            Transform sv = root.Find("Scroll View/Viewport/Content");
            if (sv != null) contentParent = sv;
        }

        if (emptyText == null)
        {
            Transform et = root.Find("EmptyText");
            if (et != null) emptyText = et.GetComponent<TextMeshProUGUI>();
        }

        if (clearButton == null)
        {
            Transform cb = root.Find("Footer/ClearButton");
            if (cb == null) cb = root.Find("ClearButton");
            if (cb != null) clearButton = cb.GetComponent<Button>();
        }

        // --- Auto-Wire Panel Konfirmasi ---
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Transform dcp = canvas.transform.Find("DeleteConfirmPanel");
            if (dcp != null)
            {
                _deleteConfirmPanel = dcp.gameObject;

                Transform btnHapus = dcp.Find("Btn_Hapus");
                if (btnHapus != null) _btnConfirmHapus = btnHapus.GetComponent<Button>();

                Transform btnBatal = dcp.Find("Btn_Batal");
                if (btnBatal != null) _btnConfirmBatal = btnBatal.GetComponent<Button>();

                Transform txtPesan = dcp.Find("TextPesan");
                if (txtPesan != null) _txtConfirmMessage = txtPesan.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    // ── Logika Konfirmasi Hapus ────────────────────────────────────────────

    private void PromptDeleteAll()
    {
        _deleteTarget = DeleteTarget.All;
        if (_txtConfirmMessage != null)
            _txtConfirmMessage.text = "Apakah anda yakin ingin menghapus semua riwayat?";

        if (_deleteConfirmPanel != null)
            _deleteConfirmPanel.SetActive(true);
    }

    private void PromptDeleteSingle(int index)
    {
        _deleteTarget = DeleteTarget.Single;
        _deleteIndex = index;

        if (_txtConfirmMessage != null)
            _txtConfirmMessage.text = "Apakah anda yakin ingin menghapus riwayat ini?";

        if (_deleteConfirmPanel != null)
            _deleteConfirmPanel.SetActive(true);
    }

    private void CloseDeleteConfirm()
    {
        if (_deleteConfirmPanel != null)
            _deleteConfirmPanel.SetActive(false);

        _deleteTarget = DeleteTarget.None;
        _deleteIndex = -1;
    }

    private void ExecuteDelete()
    {
        // Mengeksekusi penghapusan sesuai target yang dipilih
        if (_deleteTarget == DeleteTarget.All)
        {
            SimulationHistoryManager.ClearAll();
        }
        else if (_deleteTarget == DeleteTarget.Single && _deleteIndex >= 0)
        {
            SimulationHistoryManager.DeleteAt(_deleteIndex);
        }

        // Sembunyikan panel lalu bangun ulang tampilan list
        CloseDeleteConfirm();
        Rebuild();
    }


    // ── List Build ─────────────────────────────────────────────────────────

    public void Rebuild()
    {
        if (contentParent == null) return;

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        List<SimulationHistoryEntry> entries = SimulationHistoryManager.LoadAll();

        if (emptyText != null)
            emptyText.gameObject.SetActive(entries.Count == 0);

        if (clearButton != null)
            clearButton.gameObject.SetActive(entries.Count > 0);

        for (int i = 0; i < entries.Count; i++)
            CreateListItem(entries[i], i);
    }

    private void CreateListItem(SimulationHistoryEntry entry, int index)
    {
        GameObject row = CreateUiElement("HistoryRow", contentParent);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0, 72);

        var rowImg = row.AddComponent<Image>();
        rowImg.color = RowItemBg;

        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 72;
        rowLe.minWidth = 1500f;
        rowLe.flexibleWidth = 1;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 20, 10, 10);
        hlg.spacing = 16;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // Status dot
        GameObject dot = CreateUiElement("Dot", row.transform);
        var dotImg = dot.AddComponent<Image>();
        dotImg.color = entry.powerSufficient ? AccentGreen : AccentRed;
        var dotLe = dot.AddComponent<LayoutElement>();
        dotLe.preferredWidth = 12;
        dotLe.preferredHeight = 12;

        // Date/Time text
        GameObject dateGo = CreateUiElement("DateText", row.transform);
        var dateTmp = dateGo.AddComponent<TextMeshProUGUI>();
        dateTmp.text = entry.dateTime;
        dateTmp.fontSize = 22;
        dateTmp.fontStyle = FontStyles.Bold;
        dateTmp.color = TextWhite;
        dateTmp.alignment = TextAlignmentOptions.MidlineLeft;
        dateTmp.raycastTarget = false;
        var dateLe = dateGo.AddComponent<LayoutElement>();
        dateLe.flexibleWidth = 1;
        dateLe.preferredHeight = 52;

        // Duration hint
        string durText = FormatDuration(entry.durationHours);
        GameObject durGo = CreateUiElement("DurText", row.transform);
        var durTmp = durGo.AddComponent<TextMeshProUGUI>();
        durTmp.text = durText;
        durTmp.fontSize = 18;
        durTmp.color = TextMuted;
        durTmp.alignment = TextAlignmentOptions.MidlineRight;
        durTmp.raycastTarget = false;
        var durLe = durGo.AddComponent<LayoutElement>();
        durLe.preferredWidth = 160;
        durLe.preferredHeight = 52;

        int capturedIndex = index;
        CreateRowButton(row.transform, "Lihat", BtnLihatColor, 100, () => ShowDetail(capturedIndex));

        // PERUBAHAN: Tombol hapus di list sekarang memanggil konfirmasi, bukan langsung hapus
        CreateRowButton(row.transform, "Hapus", BtnHapusColor, 100, () => PromptDeleteSingle(capturedIndex));
    }

    private void CreateRowButton(Transform parent, string label, Color bgColor, float width,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGo = CreateUiElement("Btn_" + label, parent);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = bgColor;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.95f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.75f, 1f);
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var btnLe = btnGo.AddComponent<LayoutElement>();
        btnLe.preferredWidth = width;
        btnLe.preferredHeight = 46;

        GameObject lblGo = CreateUiElement("Label", btnGo.transform);
        var lblRt = lblGo.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.sizeDelta = Vector2.zero;
        lblRt.anchoredPosition = Vector2.zero;

        var lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
        lblTmp.text = label;
        lblTmp.fontSize = 18;
        lblTmp.fontStyle = FontStyles.Bold;
        lblTmp.color = TextWhite;
        lblTmp.alignment = TextAlignmentOptions.Center;
        lblTmp.raycastTarget = false;
    }

    // ── Detail Overlay ────────────────────────────────────────────────────

    private void BuildDetailOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform canvasRoot = canvas != null ? canvas.transform : transform.root;

        _detailOverlay = CreateUiElement("DetailOverlay", canvasRoot);
        var overlayRt = _detailOverlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        var ignoreLe = _detailOverlay.AddComponent<LayoutElement>();
        ignoreLe.ignoreLayout = true;

        var overlayImg = _detailOverlay.AddComponent<Image>();
        overlayImg.color = OverlayBg;
        overlayImg.raycastTarget = true;

        GameObject card = CreateUiElement("Card", _detailOverlay.transform);
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(700, 0);
        cardRt.anchoredPosition = Vector2.zero;

        var cardImg = card.AddComponent<Image>();
        cardImg.color = CardBg;

        var cardVlg = card.AddComponent<VerticalLayoutGroup>();
        cardVlg.padding = new RectOffset(40, 40, 32, 32);
        cardVlg.spacing = 14;
        cardVlg.childAlignment = TextAnchor.UpperCenter;
        cardVlg.childControlWidth = true;
        cardVlg.childControlHeight = true;
        cardVlg.childForceExpandWidth = true;
        cardVlg.childForceExpandHeight = false;

        var cardCsf = card.AddComponent<ContentSizeFitter>();
        cardCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        cardCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _detailTitle = AddCardLabel(card.transform, "Hasil Simulasi", 28, TextWhite,
            FontStyles.Bold, TextAlignmentOptions.Center, 44);

        CreateDivider(card.transform);

        _detailDuration = CreateStatRow(card.transform, "Durasi");
        _detailPanels = CreateStatRow(card.transform, "Panel Surya");
        _detailBatteries = CreateStatRow(card.transform, "Baterai");
        _detailFurniture = CreateStatRow(card.transform, "Daya Furnitur");
        _detailSolar = CreateStatRow(card.transform, "Energi Solar");
        _detailConsumed = CreateStatRow(card.transform, "Energi Terpakai");
        _detailStatus = CreateStatRow(card.transform, "Status Listrik");

        CreateDivider(card.transform);

        CreateOverlayButton(card.transform, "Lihat Log Per Jam", BtnLogColor, OnViewHourlyLogClicked);
        CreateOverlayButton(card.transform, "Kembali", BtnBackColor, HideDetail);

        _detailOverlay.SetActive(false);
    }

    private void CreateOverlayButton(Transform parent, string label, Color bgColor,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGo = CreateUiElement("Btn_" + label.Replace(" ", ""), parent);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = bgColor;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.95f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.75f, 1f);
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var btnLe = btnGo.AddComponent<LayoutElement>();
        btnLe.preferredHeight = 46;
        btnLe.flexibleWidth = 1;

        GameObject lblGo = CreateUiElement("Label", btnGo.transform);
        var lblRt = lblGo.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = Vector2.zero;
        lblRt.offsetMax = Vector2.zero;

        var lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
        lblTmp.text = label;
        lblTmp.fontSize = 20;
        lblTmp.fontStyle = FontStyles.Bold;
        lblTmp.color = TextWhite;
        lblTmp.alignment = TextAlignmentOptions.Center;
        lblTmp.raycastTarget = false;
    }

    // ── Hourly Log Overlay ────────────────────────────────────────────────

    private void BuildHourlyOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform canvasRoot = canvas != null ? canvas.transform : transform.root;

        _hourlyOverlay = CreateUiElement("HourlyLogOverlay", canvasRoot);
        var overlayRt = _hourlyOverlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        var ignoreLe = _hourlyOverlay.AddComponent<LayoutElement>();
        ignoreLe.ignoreLayout = true;

        var overlayImg = _hourlyOverlay.AddComponent<Image>();
        overlayImg.color = TermBg;
        overlayImg.raycastTarget = true;

        GameObject card = CreateUiElement("Card", _hourlyOverlay.transform);
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.1f, 0.05f);
        cardRt.anchorMax = new Vector2(0.9f, 0.95f);
        cardRt.offsetMin = Vector2.zero;
        cardRt.offsetMax = Vector2.zero;

        var cardImg = card.AddComponent<Image>();
        cardImg.color = TermBg;

        var cardVlg = card.AddComponent<VerticalLayoutGroup>();
        cardVlg.padding = new RectOffset(16, 16, 12, 12);
        cardVlg.spacing = 6;
        cardVlg.childAlignment = TextAnchor.UpperCenter;
        cardVlg.childControlWidth = true;
        cardVlg.childControlHeight = true;
        cardVlg.childForceExpandWidth = true;
        cardVlg.childForceExpandHeight = false;

        AddCardLabel(card.transform, "Log Per Jam", 20, HeaderCyan,
            FontStyles.Bold, TextAlignmentOptions.Center, 32);

        GameObject scrollView = CreateUiElement("ScrollView", card.transform);
        var svLe = scrollView.AddComponent<LayoutElement>();
        svLe.flexibleHeight = 1;
        svLe.flexibleWidth = 1;

        var svImg = scrollView.AddComponent<Image>();
        svImg.color = new Color(0, 0, 0, 0);
        svImg.raycastTarget = true;

        var scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateUiElement("Viewport", scrollView.transform);
        var vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;

        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = Color.white;
        vpImg.raycastTarget = true;

        scrollRect.viewport = vpRt;

        GameObject content = CreateUiElement("Content", viewport.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0, 0);

        var contentVlg = content.AddComponent<VerticalLayoutGroup>();
        contentVlg.padding = new RectOffset(8, 8, 4, 4);
        contentVlg.spacing = 2;
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = true;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;

        var contentCsf = content.AddComponent<ContentSizeFitter>();
        contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;

        _hourlyContentParent = content.transform;

        GameObject btnClose = CreateUiElement("BtnClose", card.transform);
        var closeBtnImg = btnClose.AddComponent<Image>();
        closeBtnImg.color = BtnBackColor;
        var closeBtn = btnClose.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImg;
        closeBtn.onClick.AddListener(HideHourlyLog);
        var closeLe = btnClose.AddComponent<LayoutElement>();
        closeLe.preferredHeight = 36;
        closeLe.flexibleWidth = 1;

        GameObject closeLbl = CreateUiElement("Label", btnClose.transform);
        var closeLblRt = closeLbl.GetComponent<RectTransform>();
        closeLblRt.anchorMin = Vector2.zero;
        closeLblRt.anchorMax = Vector2.one;
        closeLblRt.offsetMin = Vector2.zero;
        closeLblRt.offsetMax = Vector2.zero;
        var closeTmp = closeLbl.AddComponent<TextMeshProUGUI>();
        closeTmp.text = "Kembali";
        closeTmp.fontSize = 16;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.color = TextWhite;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.raycastTarget = false;

        _hourlyOverlay.SetActive(false);
    }

    private void OnViewHourlyLogClicked()
    {
        if (_currentDetailEntry == null) return;
        BuildHourlyLogContent(_currentDetailEntry);
        if (_hourlyOverlay != null) _hourlyOverlay.SetActive(true);
    }

    private void HideHourlyLog()
    {
        if (_hourlyOverlay != null) _hourlyOverlay.SetActive(false);
    }

    private void BuildHourlyLogContent(SimulationHistoryEntry entry)
    {
        if (_hourlyContentParent == null) return;

        foreach (Transform child in _hourlyContentParent)
            Destroy(child.gameObject);

        if (entry.hourlySnapshots == null || entry.hourlySnapshots.Count == 0)
        {
            AddTerminalLine("Belum ada data log per jam untuk simulasi ini.", InitWhite, FontSizeNormal);
            return;
        }

        AddTerminalLine($">>> SYSTEM BOOT... Memuat Kalkulasi Parameter dari JAM {entry.startHour:00}:00",
            HeaderCyan, FontSizeHeader, FontStyles.Bold);

        float initPercent = entry.maxBatteryWh > 0 ? (entry.initialBatteryWh / entry.maxBatteryWh) * 100f : 0f;

        AddTerminalLine($"[INIT] Baterai Awal disetel: {entry.initialBatteryWh:F0} Wh ({initPercent:F0}%)",
            InitWhite, FontSizeNormal);

        AddTerminalSpacer();

        for (int i = 0; i < entry.hourlySnapshots.Count; i++)
        {
            HourlySnapshot snap = entry.hourlySnapshots[i];
            BuildHourBlock(snap);
            if (i < entry.hourlySnapshots.Count - 1) AddTerminalSpacer();
        }
    }

    private void BuildHourBlock(HourlySnapshot snap)
    {
        bool hasWarning = snap.warnings != null && snap.warnings.Count > 0;
        Color hourColor = hasWarning ? HourYellow : HourGreen;
        AddTerminalLine($"[HISTORY] JAM {snap.hour:00}:00", hourColor, FontSizeHeader, FontStyles.Bold);

        AddTerminalRichLine($"> P (Surya) : <b>{snap.solarPowerWatt:F0} W</b>");
        AddTerminalRichLine($"> E (Beban) : {BuildLoadDetail(snap)}");

        Color batColor = GetBatteryColor(snap);
        AddTerminalLine($"> Sisa Bat : {snap.batteryRemainingWh:F0} Wh", batColor, FontSizeNormal, FontStyles.Bold);

        if (snap.warnings != null)
        {
            foreach (string warning in snap.warnings)
            {
                bool isCritical = warning.Contains("Kritis");
                Color warnColor = isCritical ? WarnRed : WarnYellow;
                string tag = isCritical ? "Sains" : "Engineering";
                AddTerminalLine($"  [{tag}]  {warning}", warnColor, FontSizeSmall, FontStyles.Italic);
            }
        }
    }

    private static string BuildLoadDetail(HourlySnapshot snap)
    {
        if (snap.appliances == null || snap.appliances.Count == 0) return "[ - ] = 0 W";

        var parts = new List<string>();
        foreach (ApplianceEntry app in snap.appliances)
        {
            if (app.count > 1) parts.Add($"{app.name} ({app.count}x{app.watt:F0}W)");
            else parts.Add($"{app.name} ({app.watt:F0}W)");
        }

        return $"[ {string.Join(", ", parts)} ] = <b>{snap.loadWatt:F0} W</b>";
    }

    private Color GetBatteryColor(HourlySnapshot snap)
    {
        if (snap.batteryMaxWh <= 0) return TextWhite;
        float ratio = snap.batteryRemainingWh / snap.batteryMaxWh;
        if (ratio <= 0.05f) return WarnRed;
        if (ratio < 0.3f) return WarnYellow;
        return LabelGreen;
    }

    // ── Terminal Line Helpers ──────────────────────────────────────────────

    private void AddTerminalLine(string text, Color color, int fontSize, FontStyles style = FontStyles.Normal)
    {
        GameObject lineGo = CreateUiElement("LogLine", _hourlyContentParent);
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
        var csf = lineGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void AddTerminalRichLine(string richText)
    {
        GameObject lineGo = CreateUiElement("LogLine", _hourlyContentParent);
        var tmp = lineGo.AddComponent<TextMeshProUGUI>();
        tmp.text = richText;
        tmp.color = TextWhite;
        tmp.fontSize = FontSizeNormal;
        tmp.richText = true;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        var le = lineGo.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        var csf = lineGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void AddTerminalSpacer()
    {
        GameObject spacer = CreateUiElement("Spacer", _hourlyContentParent);
        var le = spacer.AddComponent<LayoutElement>();
        le.preferredHeight = 12f;
        le.flexibleWidth = 1f;
    }

    // ── Detail Helpers ────────────────────────────────────────────────────

    private void CreateDivider(Transform parent)
    {
        GameObject divider = CreateUiElement("Divider", parent);
        var divImg = divider.AddComponent<Image>();
        divImg.color = new Color(0.20f, 0.22f, 0.28f, 1f);
        var divLe = divider.AddComponent<LayoutElement>();
        divLe.preferredHeight = 1;
        divLe.flexibleWidth = 1;
    }

    private TextMeshProUGUI CreateStatRow(Transform parent, string label)
    {
        GameObject row = CreateUiElement("Row_" + label.Replace(" ", ""), parent);
        var rowHlg = row.AddComponent<HorizontalLayoutGroup>();
        rowHlg.spacing = 12;
        rowHlg.childAlignment = TextAnchor.MiddleLeft;
        rowHlg.childControlWidth = false;
        rowHlg.childControlHeight = true;
        rowHlg.childForceExpandWidth = false;
        rowHlg.childForceExpandHeight = false;
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 46;
        rowLe.flexibleWidth = 1;

        GameObject lblGo = CreateUiElement("Label", row.transform);
        var lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
        lblTmp.text = label;
        lblTmp.fontSize = 20;
        lblTmp.color = LabelColor;
        lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
        lblTmp.raycastTarget = false;
        var lblLe = lblGo.AddComponent<LayoutElement>();
        lblLe.preferredWidth = 280;
        lblLe.preferredHeight = 46;

        GameObject valGo = CreateUiElement("Value", row.transform);
        var valTmp = valGo.AddComponent<TextMeshProUGUI>();
        valTmp.text = "-";
        valTmp.fontSize = 20;
        valTmp.fontStyle = FontStyles.Bold;
        valTmp.color = TextWhite;
        valTmp.alignment = TextAlignmentOptions.MidlineRight;
        valTmp.raycastTarget = false;
        var valLe = valGo.AddComponent<LayoutElement>();
        valLe.preferredWidth = 320;
        valLe.preferredHeight = 46;

        return valTmp;
    }

    private void ShowDetail(int index)
    {
        List<SimulationHistoryEntry> entries = SimulationHistoryManager.LoadAll();
        if (index < 0 || index >= entries.Count) return;

        _currentDetailEntry = entries[index];
        PopulateDetail(_currentDetailEntry);

        if (_detailOverlay != null) _detailOverlay.SetActive(true);
    }

    private void HideDetail()
    {
        if (_detailOverlay != null) _detailOverlay.SetActive(false);
        _currentDetailEntry = null;
    }

    private void PopulateDetail(SimulationHistoryEntry entry)
    {
        if (_detailTitle != null) _detailTitle.text = $"Hasil Simulasi  -  {entry.dateTime}";
        if (_detailDuration != null) _detailDuration.text = FormatDuration(entry.durationHours);
        if (_detailPanels != null) _detailPanels.text = $"{entry.solarPanelCount} panel";
        if (_detailBatteries != null) _detailBatteries.text = $"{entry.batteryCount} unit";
        if (_detailFurniture != null) _detailFurniture.text = $"{entry.furnitureWatt:F0} W";
        if (_detailSolar != null) _detailSolar.text = $"{entry.solarEnergyKwh:F4} kWh";
        if (_detailConsumed != null) _detailConsumed.text = $"{entry.energyConsumedKwh:F4} kWh";
        if (_detailStatus != null)
        {
            bool sufficient = entry.powerSufficient;
            _detailStatus.text = sufficient ? "\u2713  Tercukupi" : "\u2717  Tidak tercukupi";
            _detailStatus.color = sufficient ? AccentGreen : AccentRed;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static GameObject CreateUiElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI AddCardLabel(Transform parent, string text, int fontSize,
        Color color, FontStyles style, TextAlignmentOptions alignment, float height)
    {
        GameObject go = CreateUiElement("Title", parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth = 1;
        return tmp;
    }

    private static string FormatDuration(float hours)
    {
        int h = Mathf.FloorToInt(hours);
        int m = Mathf.FloorToInt((hours - h) * 60f);
        return h > 0 ? $"{h} jam {m:00} menit" : $"{m} menit";
    }
}