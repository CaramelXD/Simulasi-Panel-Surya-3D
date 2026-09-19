using UnityEngine;

/// <summary>
/// Manages the dynamic list of solar panel cards inside the "SP Content" container.
/// Listens to SolarPanelInstaller.OnPanelCountChanged and rebuilds one PanelCardUI card
/// per installed panel whenever the count changes.
/// </summary>
public class SolarPanelTopDownUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The parent transform where panel cards are spawned (SP Content).")]
    [SerializeField] private Transform spContent;

    [Tooltip("Prefab representing a single solar panel card (must have a PanelCardUI component).")]
    [SerializeField] private GameObject panelCardPrefab;

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
        // Before Start() runs, the first subscription is handled there.
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
        if (SolarPanelInstaller.Instance == null) return;
        // Remove first to guarantee no duplicate listeners.
        SolarPanelInstaller.Instance.OnPanelCountChanged.RemoveListener(OnPanelCountChanged);
        SolarPanelInstaller.Instance.OnPanelCountChanged.AddListener(OnPanelCountChanged);
    }

    private void Unsubscribe()
    {
        if (SolarPanelInstaller.Instance != null)
            SolarPanelInstaller.Instance.OnPanelCountChanged.RemoveListener(OnPanelCountChanged);
    }

    private void OnPanelCountChanged(int _) => Rebuild();

    /// <summary>
    /// Clears all existing panel cards and spawns one per currently installed panel,
    /// labeled by installation order (Panel #1, #2, …).
    /// </summary>
    public void Rebuild()
    {
        if (spContent == null || panelCardPrefab == null) return;

        // Destroy existing cards
        for (int i = spContent.childCount - 1; i >= 0; i--)
            Destroy(spContent.GetChild(i).gameObject);

        var panels = SolarPanelInstaller.Instance?.GetInstalledPanels();
        if (panels == null || panels.Count == 0) return;

        foreach (SolarPanelInstaller.InstalledPanelInfo info in panels)
        {
            GameObject card = Instantiate(panelCardPrefab, spContent);
            PanelCardUI ui = card.GetComponent<PanelCardUI>();
            ui?.Init(info.panelIndex, info.solarPanel);
        }
    }
}
