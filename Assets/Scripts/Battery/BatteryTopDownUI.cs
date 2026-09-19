using UnityEngine;

/// <summary>
/// Manages the dynamic list of battery cards inside the "Battery Content" container.
/// Listens to BatteryManager.OnBatteryCountChanged and rebuilds one BatteryUI card
/// per installed battery unit whenever the count changes.
/// </summary>
public class BatteryTopDownUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The parent transform where battery cards are spawned (Battery Content).")]
    [SerializeField] private Transform batteryContent;

    [Tooltip("Prefab representing a single battery card (must have a BatteryUI component).")]
    [SerializeField] private GameObject batterySectionPrefab;

    // Flag to prevent double-subscription before Start() has run.
    private bool _hasStarted;

    private void Start()
    {
        Subscribe();
        _hasStarted = true;
        Rebuild();
    }

    private void OnEnable()
    {
        // Before Start() runs the first subscription is handled there.
        // On subsequent enables (after collapse/expand) re-subscribe safely.
        if (!_hasStarted) return;
        Subscribe();
        Rebuild();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (BatteryManager.Instance == null) return;
        // Remove first to guarantee no duplicate listeners.
        BatteryManager.Instance.OnBatteryCountChanged.RemoveListener(OnBatteryCountChanged);
        BatteryManager.Instance.OnBatteryCountChanged.AddListener(OnBatteryCountChanged);
    }

    private void Unsubscribe()
    {
        if (BatteryManager.Instance != null)
            BatteryManager.Instance.OnBatteryCountChanged.RemoveListener(OnBatteryCountChanged);
    }

    private void OnBatteryCountChanged(int _) => Rebuild();

    /// <summary>
    /// Clears all existing battery cards and spawns one per currently installed battery,
    /// labeled by installation order (Battery #1, #2, …).
    /// </summary>
    public void Rebuild()
    {
        if (batteryContent == null || batterySectionPrefab == null) return;

        // Destroy existing cards
        for (int i = batteryContent.childCount - 1; i >= 0; i--)
            Destroy(batteryContent.GetChild(i).gameObject);

        var batteries = BatteryInstaller.Instance?.GetInstalledBatteries();
        if (batteries == null || batteries.Count == 0) return;

        foreach (BatteryInstaller.InstalledBatteryInfo info in batteries)
        {
            GameObject card = Instantiate(batterySectionPrefab, batteryContent);
            BatteryUI ui = card.GetComponent<BatteryUI>();
            ui?.SetBatteryNumber(info.batteryNumber);
        }
    }
}
