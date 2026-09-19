using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SunController : MonoBehaviour
{
    public static SunController Instance { get; private set; }

    [Range(0, 24)]
    public float timeOfDay = 12f;

    [Header("Day Cycle")]
    [SerializeField] bool dayCycleActive = false;
    [SerializeField] float dayDuration = 24f; // durasi 1 hari penuh dalam detik (1 jam = 1 detik)

    [Header("UI")]
    [SerializeField] TMP_Text timeLabel;
    [SerializeField] Slider timeSlider;
    [SerializeField] Toggle dayCycleToggle;

    Light sun;

    const float FullDay = 24f;

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
        sun = GetComponent<Light>();

        if (timeSlider != null)
        {
            timeSlider.minValue = 0f;
            timeSlider.maxValue = FullDay;
            timeSlider.value    = timeOfDay;
            timeSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        if (dayCycleToggle != null)
        {
            dayCycleToggle.isOn = dayCycleActive;
            dayCycleToggle.onValueChanged.AddListener(OnDayCycleToggled);
        }

        UpdateSliderInteractable();
        UpdateSun();
    }

    void Update()
    {
        bool simulationRunning = SimulationManager.Instance != null
                                 && SimulationManager.Instance.IsSimulationRunning;

        if (!dayCycleActive || !simulationRunning) return;

        timeOfDay += (FullDay / dayDuration) * Time.deltaTime;

        if (timeOfDay >= FullDay)
            timeOfDay -= FullDay;

        if (timeSlider != null)
            timeSlider.SetValueWithoutNotify(timeOfDay);

        UpdateSun();
    }

    /// <summary>
    /// Toggle dan slider hanya interactable saat simulasi BELUM berjalan.
    /// Saat simulasi aktif, semua kontrol dikunci.
    /// </summary>
    void LateUpdate()
    {
        bool simulationRunning = SimulationManager.Instance != null
                                 && SimulationManager.Instance.IsSimulationRunning;

        if (dayCycleToggle != null)
            dayCycleToggle.interactable = !simulationRunning;

        if (timeSlider != null)
            timeSlider.interactable = !simulationRunning && !dayCycleActive;
    }

    /// <summary>Dipanggil saat slider digeser secara manual (hanya aktif ketika day cycle non-aktif).</summary>
    void OnSliderChanged(float value)
    {
        if (dayCycleActive) return;
        timeOfDay = value;
        UpdateSun();
    }

    /// <summary>Dipanggil saat toggle Day Cycle berubah.</summary>
    void OnDayCycleToggled(bool isOn)
    {
        dayCycleActive = isOn;
        UpdateSliderInteractable();
    }

    void UpdateSliderInteractable()
    {
        if (timeSlider != null)
            timeSlider.interactable = !dayCycleActive;
    }

    /// <summary>Waktu hari saat ini dalam jam (0–24), hanya baca.</summary>
    public float TimeOfDay => timeOfDay;

    /// <summary>
    /// Berapa jam simulasi per detik real-time.
    /// Dengan dayDuration=24, hasilnya 1.0 (1 jam simulasi = 1 detik real).
    /// </summary>
    public float SimulationHoursPerSecond
    {
        get { return FullDay / dayDuration; }
        set { dayDuration = FullDay / value; }
    }

    /// <summary>Set waktu hari secara programatik (0–24).</summary>
    public void SetTime(float time)
    {
        timeOfDay = Mathf.Clamp(time, 0f, FullDay);
        if (timeSlider != null)
            timeSlider.SetValueWithoutNotify(timeOfDay);
        UpdateSun();
    }

    /// <summary>Aktifkan atau nonaktifkan siklus hari secara programatik.</summary>
    public void SetDayCycleActive(bool active)
    {
        dayCycleActive = active;
        if (dayCycleToggle != null)
            dayCycleToggle.SetIsOnWithoutNotify(active);
        UpdateSliderInteractable();
    }

    void UpdateSun()
    {
        float angle = (timeOfDay / FullDay) * 360f;
        transform.rotation = Quaternion.Euler(angle - 90f, 170f, 0f);

        float intensity = Mathf.Clamp01(Mathf.Sin(timeOfDay / FullDay * Mathf.PI));
        sun.intensity = intensity * 1.5f;

        UpdateTimeLabel();
    }

    void UpdateTimeLabel()
    {
        if (timeLabel == null) return;

        int hours   = Mathf.FloorToInt(timeOfDay) % 24;
        int minutes = Mathf.FloorToInt((timeOfDay % 1f) * 60f);
        timeLabel.text = $"{hours:00}:{minutes:00}";
    }
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
