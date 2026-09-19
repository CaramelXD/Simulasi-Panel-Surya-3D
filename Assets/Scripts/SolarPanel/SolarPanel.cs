using UnityEngine;
using TMPro;

public class SolarPanel : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] SunController sunController;

    [Header("Spesifikasi Panel")]
    [SerializeField] float panelArea = 1.6f;          // luas panel dalam m2
    [SerializeField] float panelEfficiency = 0.20f;   // efisiensi panel (0-1), default 20%
    [SerializeField] float peakIrradiance = 1000f;    // irradiansi puncak dalam W/m2
    [SerializeField] Vector3 panelNormal = Vector3.up; // arah permukaan panel dalam lokal space

    [Header("UI Output")]
    [SerializeField] TMP_Text powerLabel;   // label daya saat ini (Watt)
    [SerializeField] TMP_Text energyLabel;  // label energi terakumulasi (kWh)

    [Header("Deteksi Bayangan")]
    [SerializeField] LayerMask shadowBlockerLayers = Physics.DefaultRaycastLayers;
    [SerializeField] float raycastDistance = 500f;

    // Hasil kalkulasi yang bisa dibaca script lain
    public float CurrentPower { get; private set; }
    public float TotalEnergy  { get; private set; }
    public bool  IsBlocked    { get; private set; }

    const float SecondsPerHour = 3600f;

    /// <summary>Reset energi kumulatif panel ke nol. Dipanggil oleh BatteryManager saat simulasi baru dimulai.</summary>
    public void ResetTotalEnergy()
    {
        TotalEnergy = 0f;
    }

    void Update()
    {
        // Hentikan semua kalkulasi jika simulasi tidak berjalan
        bool simulationOff = SimulationManager.Instance != null
                             && !SimulationManager.Instance.IsSimulationRunning;

        if (simulationOff)
        {
            CurrentPower = 0f;
            IsBlocked    = false;
            UpdateLabels();
            return;
        }

        IsBlocked = CheckIfBlocked();

        if (IsBlocked)
        {
            CurrentPower = 0f;
            UpdateLabels();
            return;
        }

        float sunIntensityFactor = CalculateSunIntensityFactor();
        float angleFactor        = CalculateAngleFactor();

        // Daya (Watt) = irradiansi x luas x efisiensi x faktor intensitas x faktor sudut
        CurrentPower = peakIrradiance * panelArea * panelEfficiency * sunIntensityFactor * angleFactor;

        // Energi (kWh) = akumulasi daya x waktu simulasi
        // deltaSimHours = jam simulasi yang berlalu per frame
        float deltaSimHours = sunController.SimulationHoursPerSecond * Time.deltaTime;
        TotalEnergy += CurrentPower * deltaSimHours / 1000f; // Watt * jam / 1000 = kWh

        UpdateLabels();
    }

    /// <summary>Cek apakah ada objek yang menghalangi sinar matahari ke panel menggunakan raycast.</summary>
    bool CheckIfBlocked()
    {
        if (sunController == null) return false;

        Vector3 towardSun = -sunController.transform.forward;

        // Kalau matahari masih di bawah horizon, tidak perlu raycast
        if (towardSun.y <= 0f) return false;

        Vector3 worldNormal = transform.TransformDirection(panelNormal.normalized);
        Vector3 origin      = transform.position + worldNormal * 0.1f;

        RaycastHit[] hits = Physics.RaycastAll(origin, towardSun, raycastDistance, shadowBlockerLayers);

        foreach (RaycastHit hit in hits)
        {
            // Abaikan collider yang masih bagian dari GameObject panel surya ini
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.gameObject == gameObject)
                continue;

            Debug.Log($"[SolarPanel] Diblokir oleh: {hit.collider.gameObject.name} (layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Menghitung faktor intensitas matahari berdasarkan waktu dari SunController (0-1).
    /// Rumus: 0.85 * (1 - (0.004 * 25 * MAX(COS(RADIANS(180 - (timeOfDay/24)*360)), 0)))
    /// </summary>
    float CalculateSunIntensityFactor()
    {
        if (sunController == null) return 0f;

        float timeOfDay = sunController.timeOfDay;

        // Konversi sudut: 180 - (timeOfDay / 24) * 360, lalu ke radian
        float angleDeg = 180f - (timeOfDay / 24f) * 360f;
        float angleRad = angleDeg * Mathf.Deg2Rad;

        float cosValue = Mathf.Max(Mathf.Cos(angleRad), 0f);

        const float tempCoefficient = 0.004f;
        const float tempDelta = 25f;
        const float baseEfficiency = 0.85f;

        float factor = baseEfficiency * (1f - (tempCoefficient * tempDelta * cosValue));
        return Mathf.Clamp01(factor);
    }

    /// <summary>
    /// Menghitung faktor sudut antara normal panel dan arah datangnya sinar matahari (0-1).
    /// Nilai 1 berarti panel tegak lurus menghadap matahari.
    /// </summary>
    float CalculateAngleFactor()
    {
        if (sunController == null) return 0f;

        Vector3 sunDirection = sunController.transform.forward;
        Vector3 worldNormal  = transform.TransformDirection(panelNormal.normalized);
        float dot = Vector3.Dot(worldNormal, -sunDirection);
        return Mathf.Max(0f, dot);
    }

    void UpdateLabels()
    {
        if (powerLabel != null)
            powerLabel.text = $"{CurrentPower:F1} W";

        if (energyLabel != null)
            energyLabel.text = $"{TotalEnergy:F3} kWh";
    }

    void OnDrawGizmosSelected()
    {
        Vector3 worldNormal = transform.TransformDirection(panelNormal.normalized);
        Vector3 origin      = transform.position;
        Vector3 rayOrigin   = origin + worldNormal * 0.1f;

        float angleFactor = 0f;
        if (sunController != null)
        {
            Vector3 sunDirection = sunController.transform.forward;
            angleFactor = Mathf.Max(0f, Vector3.Dot(worldNormal, -sunDirection));
        }

        Gizmos.color = Color.Lerp(Color.red, Color.green, angleFactor);
        DrawArrow(origin, worldNormal, 1.5f);

        if (sunController != null)
        {
            Vector3 sunDir = -sunController.transform.forward;

            if (sunDir.y > 0f)
            {
                Gizmos.color = IsBlocked ? Color.red : Color.green;
                Gizmos.DrawLine(rayOrigin, rayOrigin + sunDir * raycastDistance);
            }

            Gizmos.color = Color.yellow;
            DrawArrow(origin, sunDir, 1.5f);
        }
    }

    void DrawArrow(Vector3 origin, Vector3 direction, float length)
    {
        Vector3 tip = origin + direction * length;
        Gizmos.DrawLine(origin, tip);
        Gizmos.DrawSphere(tip, 0.05f);
    }
}
