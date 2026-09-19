using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menampilkan state battery di UI:
/// - Label nama baterai (Battery #N)
/// - Progress bar charge (0-1)
/// - Label persentase charge
/// - Label kWh saat ini
/// - Indikator status (Mengisi / Penuh / Kosong)
/// Update dilakukan setiap frame agar selalu sinkron tanpa bergantung pada urutan event.
/// </summary>
public class BatteryUI : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private TextMeshProUGUI batteryLabel;

    [Header("References")]
    [SerializeField] private Slider chargeSlider;
    [SerializeField] private Image chargeFill;
    [SerializeField] private TextMeshProUGUI percentageLabel;
    [SerializeField] private TextMeshProUGUI kwhLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;

    [Header("Colors")]
    [SerializeField] private Color colorFull = new Color(0.20f, 0.75f, 0.25f, 1f);
    [SerializeField] private Color colorMid  = new Color(0.95f, 0.75f, 0.10f, 1f);
    [SerializeField] private Color colorLow  = new Color(0.85f, 0.20f, 0.15f, 1f);

    [Tooltip("Threshold charge ratio di bawah ini dianggap 'Low'.")]
    [SerializeField] private float lowThreshold = 0.20f;

    [Tooltip("Threshold charge ratio di bawah ini dianggap 'Mid'.")]
    [SerializeField] private float midThreshold = 0.60f;

    /// <summary>Sets the battery number label shown on this card (e.g. "Battery #2").</summary>
    public void SetBatteryNumber(int number)
    {
        if (batteryLabel != null)
            batteryLabel.text = $"Battery #{number}";
    }

    private void Update()
    {
        if (BatteryManager.Instance == null) return;
        RefreshUI();
    }

    private void RefreshUI()
    {
        float ratio  = BatteryManager.Instance.ChargeRatio;
        float charge = BatteryManager.Instance.CurrentCharge;
        float maxCap = BatteryManager.Instance.MaxCapacity;

        if (chargeSlider != null)
            chargeSlider.value = ratio;

        if (chargeFill != null)
            chargeFill.color = GetFillColor(ratio);

        if (percentageLabel != null)
            percentageLabel.text = $"{ratio * 100f:F0}%";

        if (kwhLabel != null)
            kwhLabel.text = $"{charge:F2} / {maxCap:F1} kWh";

        if (statusLabel != null)
            statusLabel.text = GetStatusText(ratio);
    }

    private Color GetFillColor(float ratio)
    {
        if (ratio <= lowThreshold) return colorLow;
        if (ratio <= midThreshold) return colorMid;
        return colorFull;
    }

    private string GetStatusText(float ratio)
    {
        if (ratio >= 1f) return "Penuh";
        if (ratio <= 0f) return "Kosong";
        return "Mengisi";
    }
}
