using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel overlay untuk mengkonfigurasi jam aktif (jam mulai - jam selesai) sebuah furnitur.
/// Attach ke GameObject ConfigFurniture di Canvas.
/// </summary>
public class FurnitureConfigPanel : MonoBehaviour
{
    public static FurnitureConfigPanel Instance { get; private set; }

    [Header("Jam Mulai")]
    [SerializeField] private TMP_InputField inputStartJam;
    [SerializeField] private TMP_InputField inputStartMenit;

    [Header("Jam Selesai")]
    [SerializeField] private TMP_InputField inputEndJam;
    [SerializeField] private TMP_InputField inputEndMenit;

    [Header("Buttons")]
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleText;

    private FurnitureData _currentData;

    private const int DefaultStartJam   = 6;
    private const int DefaultStartMenit = 0;
    private const int DefaultEndJam     = 22;
    private const int DefaultEndMenit   = 0;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private bool _initialized = false;
    private void OnDestroy()
    {
        // Bersihkan variabel statik saat scene dihancurkan (kembali ke menu)
        // agar di simulasi berikutnya tidak terjadi "bunuh diri" otomatis.
        if (Instance == this)
        {
            Instance = null;
        }
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        ResolveReferences();
        if (btnConfirm != null) btnConfirm.onClick.AddListener(OnConfirm);
        if (btnCancel  != null) btnCancel.onClick.AddListener(OnCancel);
    }

    // ── Reference Resolution ───────────────────────────────────────────────

    private void ResolveReferences()
    {
        Transform card = transform.Find("Card");
        if (card == null)
        {
            Debug.LogWarning("[FurnitureConfigPanel] Child 'Card' tidak ditemukan di " + gameObject.name);
            return;
        }

        if (inputStartJam   == null) inputStartJam   = card.Find("RowJam/TimeRow/InputJam")?.GetComponent<TMP_InputField>();
        if (inputStartMenit == null) inputStartMenit = card.Find("RowJam/TimeRow/InputMenit")?.GetComponent<TMP_InputField>();
        if (inputEndJam     == null) inputEndJam     = card.Find("RowJam (1)/TimeRow/InputJam")?.GetComponent<TMP_InputField>();
        if (inputEndMenit   == null) inputEndMenit   = card.Find("RowJam (1)/TimeRow/InputMenit")?.GetComponent<TMP_InputField>();
        if (btnConfirm      == null) btnConfirm      = card.Find("BtnRow/BtnMulai")?.GetComponent<Button>();
        if (btnCancel       == null) btnCancel        = card.Find("BtnRow/BtnBatal")?.GetComponent<Button>();
        if (titleText       == null) titleText        = card.Find("Title")?.GetComponent<TextMeshProUGUI>();

        Debug.Log($"[FurnitureConfigPanel] ResolveReferences — " +
                  $"inputStartJam:{inputStartJam != null} inputStartMenit:{inputStartMenit != null} " +
                  $"inputEndJam:{inputEndJam != null} inputEndMenit:{inputEndMenit != null} " +
                  $"btnConfirm:{btnConfirm != null} btnCancel:{btnCancel != null} titleText:{titleText != null}");
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Buka panel konfigurasi jam aktif untuk furnitur tertentu.</summary>
    public void Open(FurnitureData data)
    {
        // Aktifkan dulu agar Awake() jalan jika belum pernah aktif
        gameObject.SetActive(true);
        EnsureInitialized();

        _currentData = data;
        Debug.Log($"[FurnitureConfigPanel] Open() dipanggil untuk: {data?.furnitureName}");

        if (titleText != null)
            titleText.text = $"Atur Jam Aktif: {data.furnitureName}";

        // Muat nilai saat ini dari FurnitureManager, atau gunakan default
        float startH = DefaultStartJam;
        float startM = DefaultStartMenit;
        float endH   = DefaultEndJam;
        float endM   = DefaultEndMenit;

        if (FurnitureManager.Instance != null)
        {
            var (startHour, endHour) = FurnitureManager.Instance.GetUsageHours(data);
            startH = Mathf.Floor(startHour);
            startM = Mathf.Round((startHour - startH) * 60f);
            endH   = Mathf.Floor(endHour);
            endM   = Mathf.Round((endHour - endH) * 60f);
        }

        if (inputStartJam   != null) inputStartJam.text   = ((int)startH).ToString();
        if (inputStartMenit != null) inputStartMenit.text = ((int)startM).ToString("00");
        if (inputEndJam     != null) inputEndJam.text     = ((int)endH).ToString();
        if (inputEndMenit   != null) inputEndMenit.text   = ((int)endM).ToString("00");

        gameObject.SetActive(true);
    }

    // ── Button Handlers ────────────────────────────────────────────────────

    private void OnConfirm()
    {
        if (_currentData == null || FurnitureManager.Instance == null)
        {
            StartCoroutine(HideNextFrame());
            return;
        }

        int startJam   = ParseInt(inputStartJam,   DefaultStartJam);
        int startMenit = ParseInt(inputStartMenit, DefaultStartMenit);
        int endJam     = ParseInt(inputEndJam,     DefaultEndJam);
        int endMenit   = ParseInt(inputEndMenit,   DefaultEndMenit);

        startJam   = Mathf.Clamp(startJam,   0, 23);
        startMenit = Mathf.Clamp(startMenit, 0, 59);
        endJam     = Mathf.Clamp(endJam,     0, 23);
        endMenit   = Mathf.Clamp(endMenit,   0, 59);

        float startHour = startJam + startMenit / 60f;
        float endHour   = endJam   + endMenit   / 60f;

        if (endHour <= startHour)
            endHour = startHour + 1f;

        FurnitureManager.Instance.SetUsageHours(_currentData, startHour, endHour);

        Debug.Log($"[FurnitureConfigPanel] {_currentData.furnitureName}: jam aktif disimpan {startHour:0.##} - {endHour:0.##}");

        StartCoroutine(HideNextFrame());
    }

    private void OnCancel()
    {
        StartCoroutine(HideNextFrame());
    }

    /// Tunggu satu frame sebelum hide agar pointer-up tidak tembus ke button di belakang panel.
    private IEnumerator HideNextFrame()
    {
        yield return null;
        gameObject.SetActive(false);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static int ParseInt(TMP_InputField field, int fallback)
    {
        if (field == null) return fallback;
        return int.TryParse(field.text, out int v) ? v : fallback;
    }
}
