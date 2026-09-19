using TMPro;
using UnityEngine;

/// <summary>
/// Menampilkan total pengeluaran energi furnitur:
/// - Daya terpakai saat ini dalam Watt
/// - Total energi kumulatif yang dikonsumsi selama simulasi dalam kWh
///
/// Attach ke GameObject UI manapun. Sambungkan label TextMeshPro via Inspector.
/// </summary>
public class EnergyConsumptionUI : MonoBehaviour
{
    [Header("Labels")]
    [Tooltip("Label untuk daya terpakai saat ini (Watt).")]
    [SerializeField] private TextMeshProUGUI currentPowerLabel;

    [Tooltip("Label untuk total energi kumulatif yang dikonsumsi (kWh).")]
    [SerializeField] private TextMeshProUGUI totalEnergyLabel;

    private bool _hasStarted;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        Subscribe();
        _hasStarted = true;
        Refresh();
    }

    private void OnEnable()
    {
        if (!_hasStarted) return;
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // ── Subscription ──────────────────────────────────────────────────────

    private void Subscribe()
    {
        if (FurnitureManager.Instance != null)
        {
            FurnitureManager.Instance.OnPowerChanged.RemoveListener(OnPowerChanged);
            FurnitureManager.Instance.OnPowerChanged.AddListener(OnPowerChanged);
        }

        if (BatteryManager.Instance != null)
        {
            BatteryManager.Instance.OnEnergyConsumedChanged.RemoveListener(OnEnergyConsumedChanged);
            BatteryManager.Instance.OnEnergyConsumedChanged.AddListener(OnEnergyConsumedChanged);
        }
    }

    private void Unsubscribe()
    {
        if (FurnitureManager.Instance != null)
            FurnitureManager.Instance.OnPowerChanged.RemoveListener(OnPowerChanged);

        if (BatteryManager.Instance != null)
            BatteryManager.Instance.OnEnergyConsumedChanged.RemoveListener(OnEnergyConsumedChanged);
    }

    // ── Handlers ──────────────────────────────────────────────────────────

    private void OnPowerChanged(float watt) => SetCurrentPower(watt);

    private void OnEnergyConsumedChanged(float totalKwh) => SetTotalEnergy(totalKwh);

    // ── Display ───────────────────────────────────────────────────────────

    /// <summary>Sinkronkan tampilan dari state manager saat ini.</summary>
    private void Refresh()
    {
        if (FurnitureManager.Instance != null)
            SetCurrentPower(FurnitureManager.Instance.TotalWattConsumed);

        if (BatteryManager.Instance != null)
            SetTotalEnergy(BatteryManager.Instance.TotalEnergyConsumed);
    }

    private void SetCurrentPower(float watt)
    {
        if (currentPowerLabel != null)
            currentPowerLabel.text = $"{watt:F1} W";
    }

    private void SetTotalEnergy(float totalKwh)
    {
        if (totalEnergyLabel != null)
            totalEnergyLabel.text = $"{totalKwh:F4} kWh";
    }
}
