using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a single furniture item in the top-down panel list.
/// Shows the furniture name, icon, watt consumption, and powered status.
/// Updates powered status every frame based on BatteryManager state.
/// </summary>
public class FurnitureTopDownCardUI : MonoBehaviour
{
    [Header("Card Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI wattText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Icon")]
    [SerializeField] private Image iconImage;

    private FurnitureData _data;
    private SunController _sunController;

    /// <summary>
    /// Initializes the card with an index and the associated FurnitureData.
    /// </summary>
    public void Init(int index, FurnitureData data)
    {
        _data = data;
        _sunController = FindFirstObjectByType<SunController>();

        if (titleText != null)
            titleText.text = $"#{index} {data.furnitureName}";

        if (wattText != null)
            wattText.text = $"{data.wattConsumption} W";

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;
    }

    private void Update()
    {
        if (_data == null || statusText == null) return;

        bool hasPower = BatteryManager.Instance != null
                        && BatteryManager.Instance.CurrentCharge > 0f;

        bool inActiveHours = true;
        if (FurnitureManager.Instance != null && _sunController != null)
        {
            var (startHour, endHour) = FurnitureManager.Instance.GetUsageHours(_data);
            float currentHour = _sunController.TimeOfDay;
            inActiveHours = currentHour >= startHour && currentHour < endHour;
        }

        bool isOn = hasPower && inActiveHours;

        statusText.text  = isOn ? "Menyala" : "Mati";
        statusText.color = isOn
            ? new Color(0.20f, 0.75f, 0.25f, 1f)
            : new Color(0.85f, 0.20f, 0.15f, 1f);
    }
}
