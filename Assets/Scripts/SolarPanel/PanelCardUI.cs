using UnityEngine;
using TMPro;

/// <summary>
/// Ditempel pada setiap card di panel list UI.
/// Setiap frame membaca CurrentPower dan TotalEnergy dari SolarPanel terhubung,
/// lalu memperbarui teks power dan energi di card.
/// </summary>
public class PanelCardUI : MonoBehaviour
{
    [Header("Card Texts")]
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI powerText;
    [SerializeField] TextMeshProUGUI energyText;
    [SerializeField] TextMeshProUGUI statusText;

    private SolarPanel solarPanel;

    /// <summary>
    /// Inisialisasi card dengan nomor urut dan referensi SolarPanel.
    /// Dipanggil oleh SolarPanelInstaller saat panel dipasang.
    /// </summary>
    public void Init(int panelIndex, SolarPanel panel)
    {
        solarPanel = panel;

        if (titleText != null)
            titleText.text = $"Panel #{panelIndex}";
    }

    void Update()
    {
        if (solarPanel == null) return;

        if (powerText != null)
            powerText.text = $"{solarPanel.CurrentPower:F1} W";

        if (energyText != null)
            energyText.text = $"{solarPanel.TotalEnergy:F3} kWh";

        if (statusText != null)
            statusText.text = solarPanel.IsBlocked ? "Terhalang" : "Aktif";
    }
}
