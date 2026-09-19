using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel konfigurasi simulasi: jam mulai, menit mulai, dan durasi dalam JAM.
/// Attach ke GameObject ConfigOverlay.
/// </summary>
public class SimulationConfigPanel : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] TMP_InputField inputJam;
    [SerializeField] TMP_InputField inputMenit;
    [SerializeField] TMP_InputField inputDurasi;

    [Header("UI")]
    [SerializeField] TMP_Text previewLabel;
    [SerializeField] Button   btnMulai;
    [SerializeField] Button   btnBatal;

    private const int   DefaultJam    = 6;
    private const int   DefaultMenit  = 0;
    private const float DefaultDurasi = 2f;   // dalam jam

    void Awake()
    {
        ResolveReferences();
    }

    void OnEnable()
    {
        if (inputJam    != null) inputJam.text    = DefaultJam.ToString();
        if (inputMenit  != null) inputMenit.text  = DefaultMenit.ToString("00");
        if (inputDurasi != null) inputDurasi.text = DefaultDurasi.ToString("0.##");

        UpdatePreview();
    }

    void Start()
    {
        if (btnMulai != null) btnMulai.onClick.AddListener(OnMulai);
        if (btnBatal != null) btnBatal.onClick.AddListener(OnBatal);

        if (inputJam    != null) inputJam.onValueChanged.AddListener(_    => UpdatePreview());
        if (inputMenit  != null) inputMenit.onValueChanged.AddListener(_  => UpdatePreview());
        if (inputDurasi != null) inputDurasi.onValueChanged.AddListener(_ => UpdatePreview());
    }

    /// <summary>Cari referensi via hierarchy jika Inspector reference hilang.</summary>
    private void ResolveReferences()
    {
        Transform card = transform.Find("Card");
        if (card == null) return;

        if (inputJam     == null) inputJam     = card.Find("RowJam/TimeRow/InputJam")?.GetComponent<TMP_InputField>();
        if (inputMenit   == null) inputMenit   = card.Find("RowJam/TimeRow/InputMenit")?.GetComponent<TMP_InputField>();
        if (inputDurasi  == null) inputDurasi  = card.Find("RowDurasi/InputDurasi")?.GetComponent<TMP_InputField>();
        if (previewLabel == null) previewLabel = card.Find("PreviewLabel")?.GetComponent<TMP_Text>();
        if (btnMulai     == null) btnMulai     = card.Find("BtnRow/BtnMulai")?.GetComponent<Button>();
        if (btnBatal     == null) btnBatal     = card.Find("BtnRow/BtnBatal")?.GetComponent<Button>();
    }

    private void OnMulai()
    {
        int   jam    = ParseInt(inputJam,    DefaultJam);
        int   menit  = ParseInt(inputMenit,  DefaultMenit);
        float durasi = ParseFloat(inputDurasi, DefaultDurasi);

        jam    = Mathf.Clamp(jam,   0, 23);
        menit  = Mathf.Clamp(menit, 0, 59);
        durasi = Mathf.Max(0.1f, durasi);  // minimal 6 menit

        float startHour = jam + menit / 60f;

        SunController sun = FindFirstObjectByType<SunController>();
        if (sun != null)
        {
            sun.SetTime(startHour);
            sun.SetDayCycleActive(true);
        }

        if (SimulationManager.Instance != null)
            SimulationManager.Instance.StartSimulation(startHour, durasi);

        gameObject.SetActive(false);
    }

    private void OnBatal()
    {
        gameObject.SetActive(false);
    }

    private void UpdatePreview()
    {
        if (previewLabel == null) return;

        int   jam    = ParseInt(inputJam,    DefaultJam);
        int   menit  = ParseInt(inputMenit,  DefaultMenit);
        float durasi = ParseFloat(inputDurasi, DefaultDurasi);

        jam    = Mathf.Clamp(jam,   0, 23);
        menit  = Mathf.Clamp(menit, 0, 59);

        float startHour = jam + menit / 60f;
        float endHour   = startHour + Mathf.Max(0f, durasi);

        int endH = Mathf.FloorToInt(endHour) % 24;
        int endM = Mathf.FloorToInt((endHour % 1f) * 60f);

        previewLabel.text = $"Mulai: {jam:00}:{menit:00}  →  Selesai: {endH:00}:{endM:00}";
    }

    private static int ParseInt(TMP_InputField field, int fallback)
    {
        if (field == null) return fallback;
        return int.TryParse(field.text, out int v) ? v : fallback;
    }

    private static float ParseFloat(TMP_InputField field, float fallback)
    {
        if (field == null) return fallback;
        return float.TryParse(field.text, System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out float v)
               ? v : fallback;
    }
}
