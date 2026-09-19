using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SolarEdu;

/// <summary>
/// Mengontrol tombol-tombol di scene Menu dan navigasi antar tampilan.
/// Mengelola peralihan antara menu utama dan panel history.
/// Semua referensi UI di-wire otomatis via AutoWire() — tidak bergantung pada Inspector.
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Scene Target")]
    [Tooltip("Nama scene yang dibuka saat menekan Btn_Buat.")]
    [SerializeField] private string simulasiSceneName = "Simulasi";

    [Header("Materi PDF")]
    [Tooltip("Nama file PDF di dalam folder StreamingAssets (termasuk ekstensi .pdf).")]
    [SerializeField] private string materiFileName = "Materi.pdf";

    [Header("Petunjuk PDF")]
    [Tooltip("Nama file PDF petunjuk di dalam folder StreamingAssets (termasuk ekstensi .pdf).")]
    [SerializeField] private string petunjukFileName = "Petunjuk.pdf";

    [Header("Menu Elements")]
    [Tooltip("Elemen-elemen yang ditampilkan saat di menu utama.")]
    [SerializeField] private GameObject[] menuElements;

    [Header("History")]
    [Tooltip("Panel history yang di-toggle saat tombol History ditekan.")]
    [SerializeField] private GameObject historyPanel;

    [Tooltip("Tombol kembali di dalam history panel.")]
    [SerializeField] private Button backFromHistoryButton;

    // Tombol-tombol menu utama
    private Button _btnMateri;
    private Button _btnPetunjuk;
    private Button _btnBuat;
    private Button _btnHistory;
    private Button _btnKeluar;

    private void Start()
    {
        AutoWire();
        WireButtons();

        // Pastikan menu utama tampil dan history tersembunyi
        SetMenuVisible(true);
        SetHistoryVisible(false);

        if (backFromHistoryButton != null)
            backFromHistoryButton.onClick.AddListener(OnBackFromHistory);
    }

    private void AutoWire()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Auto-find menu elements jika belum di-assign
        if (menuElements == null || menuElements.Length == 0)
        {
            Transform bg = canvas.transform.Find("RawImage");
            Transform title = canvas.transform.Find("Text (TMP)");
            Transform buttons = canvas.transform.Find("ButtonContainer");

            var list = new System.Collections.Generic.List<GameObject>();
            if (bg != null) list.Add(bg.gameObject);
            if (title != null) list.Add(title.gameObject);
            if (buttons != null) list.Add(buttons.gameObject);
            menuElements = list.ToArray();
        }

        // Auto-find history panel
        if (historyPanel == null)
        {
            Transform hp = canvas.transform.Find("HistoryPanel");
            if (hp != null) historyPanel = hp.gameObject;
        }

        // Auto-find back button di HistoryPanel
        if (backFromHistoryButton == null && historyPanel != null)
        {
            Transform backBtn = historyPanel.transform.Find("BackButton");
            if (backBtn != null) backFromHistoryButton = backBtn.GetComponent<Button>();
        }

        // Auto-find tombol-tombol menu (semuanya ada di dalam ButtonContainer)
        Transform container = canvas.transform.Find("ButtonContainer");
        if (container != null)
        {
            Transform materi = container.Find("Btn_Materi");
            if (materi != null) _btnMateri = materi.GetComponent<Button>();

            Transform petunjuk = container.Find("Btn_HELP");
            if (petunjuk != null) _btnPetunjuk = petunjuk.GetComponent<Button>();

            Transform buat = container.Find("Btn_Buat");
            if (buat != null) _btnBuat = buat.GetComponent<Button>();

            Transform history = container.Find("Btn_History");
            if (history != null) _btnHistory = history.GetComponent<Button>();

            Transform keluar = container.Find("Btn_Keluar");
            if (keluar != null) _btnKeluar = keluar.GetComponent<Button>();
        }
    }

    /// <summary>Hubungkan semua tombol menu via kode agar tidak bergantung pada Inspector.</summary>
    private void WireButtons()
    {
        if (_btnMateri != null)
            _btnMateri.onClick.AddListener(OnMateriClicked);

        if (_btnPetunjuk != null)
            _btnPetunjuk.onClick.AddListener(OnPetunjukClicked);

        if (_btnBuat != null)
            _btnBuat.onClick.AddListener(OnBuatClicked);

        if (_btnHistory != null)
            _btnHistory.onClick.AddListener(OnHistoryClicked);

        if (_btnKeluar != null)
            _btnKeluar.onClick.AddListener(OnKeluarClicked);
    }

    // ── Public Button Handlers ──────────────────────────────────────────────

    /// <summary>Buka file PDF materi di DALAM GAME.</summary>
    public void OnMateriClicked()
    {
        if (PDFViewerManager.Instance != null)
        {
            // Panggil dokumen dengan ID "Materi"
            PDFViewerManager.Instance.OpenPDF("Materi");
        }

        // ── SolarEdu Tracking ──
        if (SolarEduManager.Instance != null)
        {
            SolarEduManager.Instance.SendStatement(
                SolarEduVerb.Mengamati,
                "solaredu://materi/pdf",
                "Materi Pembelajaran"
            );
        }
    }

    /// <summary>Buka file PDF petunjuk di DALAM GAME.</summary>
    public void OnPetunjukClicked()
    {
        if (PDFViewerManager.Instance != null)
        {
            // Panggil dokumen dengan ID "Petunjuk"
            PDFViewerManager.Instance.OpenPDF("Petunjuk");
        }

        // ── SolarEdu Tracking ──
        if (SolarEduManager.Instance != null)
        {
            SolarEduManager.Instance.SendStatement(
                SolarEduVerb.Mengamati,
                "solaredu://materi/petunjuk",
                "Petunjuk Penggunaan"
            );
        }
    }


    /// <summary>Pindah ke scene simulasi.</summary>
    public void OnBuatClicked()
    {
        Debug.Log($"[MenuController] Memuat scene: {simulasiSceneName}");

        // ── SolarEdu Tracking ──
        if (SolarEduManager.Instance != null)
        {
            SolarEduManager.Instance.SendStatement(
                SolarEduVerb.Memulai,
                "solaredu://sesi/simulasi-baru",
                "Mulai Sesi Simulasi Baru"
            );
        }

        SceneManager.LoadScene(simulasiSceneName);
    }

    /// <summary>Tampilkan tampilan history.</summary>
    public void OnHistoryClicked()
    {
        Debug.Log("[MenuController] Menampilkan history.");
        SetMenuVisible(false);
        SetHistoryVisible(true);
    }

    /// <summary>Kembali dari history ke menu utama.</summary>
    public void OnBackFromHistory()
    {
        Debug.Log("[MenuController] Kembali ke menu utama.");
        SetHistoryVisible(false);
        SetMenuVisible(true);
    }

    /// <summary>Logout akun aktif lalu kembali ke scene Login.</summary>
    public void OnKeluarClicked()
    {
        // Logout SolarEdu (hapus data auto-login)
        var auth = FindFirstObjectByType<SolarEduAuth>();
        if (auth != null) auth.Logout();

        // Logout lokal (sistem lama)
        UserRepository.Logout();

        Debug.Log("[MenuController] Logout, kembali ke Login.");
        SceneManager.LoadScene("Login");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void SetMenuVisible(bool visible)
    {
        if (menuElements == null) return;
        foreach (var el in menuElements)
        {
            if (el != null) el.SetActive(visible);
        }
    }

    private void SetHistoryVisible(bool visible)
    {
        if (historyPanel != null)
            historyPanel.SetActive(visible);
    }
}
