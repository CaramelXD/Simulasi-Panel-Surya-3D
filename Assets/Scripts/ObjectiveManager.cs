using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengelola status objective simulasi:
/// - Minimal 1 furnitur terpasang  → Furniture Toggle dicentang otomatis.
/// - Minimal 1 solar panel terdaftar → Solar Panel Toggle dicentang otomatis.
/// - Minimal 1 baterai terpasang   → Battery Toggle dicentang otomatis.
/// Toggle dibuat non-interactable sehingga tidak bisa diklik user.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }
    [Header("Furniture Objective")]
    [Tooltip("Komponen Toggle pada CheckList Furniture Objective.")]
    [SerializeField] private Toggle furnitureToggle;

    [Header("Solar Panel Objective")]
    [Tooltip("Komponen Toggle pada CheckList Solar Panel Objective.")]
    [SerializeField] private Toggle solarPanelToggle;

    [Header("Battery Objective")]
    [Tooltip("Komponen Toggle pada CheckList Battery Objective.")]
    [SerializeField] private Toggle batteryToggle;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Non-interactable: centang dikontrol kode, bukan user
        if (furnitureToggle  != null) furnitureToggle.interactable  = false;
        if (solarPanelToggle != null) solarPanelToggle.interactable = false;
        if (batteryToggle    != null) batteryToggle.interactable    = false;

        if (FurnitureManager.Instance != null)
            FurnitureManager.Instance.OnFurnitureCountChanged.AddListener(OnFurnitureCountChanged);

        if (BatteryManager.Instance != null)
        {
            BatteryManager.Instance.OnPanelCountChanged.AddListener(OnPanelCountChanged);
            BatteryManager.Instance.OnBatteryCountChanged.AddListener(OnBatteryCountChanged);
        }

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (FurnitureManager.Instance != null)
            FurnitureManager.Instance.OnFurnitureCountChanged.RemoveListener(OnFurnitureCountChanged);

        if (BatteryManager.Instance != null)
        {
            BatteryManager.Instance.OnPanelCountChanged.RemoveListener(OnPanelCountChanged);
            BatteryManager.Instance.OnBatteryCountChanged.RemoveListener(OnBatteryCountChanged);
        }
    }

    // ── Public Queries ─────────────────────────────────────────────────────

    /// <summary>True jika semua objective telah terpenuhi.</summary>
    public bool AreAllObjectivesComplete()
    {
        bool furniture = furnitureToggle  != null && furnitureToggle.isOn;
        bool solar     = solarPanelToggle != null && solarPanelToggle.isOn;
        bool battery   = batteryToggle    != null && batteryToggle.isOn;
        return furniture && solar && battery;
    }

    /// <summary>Mengembalikan daftar nama objective yang belum terpenuhi.</summary>
    public List<string> GetIncompleteObjectives()
    {
        var list = new List<string>();
        if (furnitureToggle  == null || !furnitureToggle.isOn)  list.Add("Furnitur");
        if (solarPanelToggle == null || !solarPanelToggle.isOn) list.Add("Panel Surya");
        if (batteryToggle    == null || !batteryToggle.isOn)    list.Add("Baterai");
        return list;
    }

    // ── Event Handlers ─────────────────────────────────────────────────────

    private void OnFurnitureCountChanged(int count)
    {
        SetToggle(furnitureToggle, count >= 1);
    }

    private void OnPanelCountChanged()
    {
        int count = BatteryManager.Instance != null ? BatteryManager.Instance.PanelCount : 0;
        SetToggle(solarPanelToggle, count >= 1);
    }

    private void OnBatteryCountChanged(int count)
    {
        SetToggle(batteryToggle, count >= 1);
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    private void RefreshAll()
    {
        int furnitureCount = FurnitureManager.Instance != null ? FurnitureManager.Instance.PlacedCount : 0;
        SetToggle(furnitureToggle, furnitureCount >= 1);

        int panelCount = BatteryManager.Instance != null ? BatteryManager.Instance.PanelCount : 0;
        SetToggle(solarPanelToggle, panelCount >= 1);

        int batteryCount = BatteryManager.Instance != null ? BatteryManager.Instance.InstalledBatteryCount : 0;
        SetToggle(batteryToggle, batteryCount >= 1);
    }

    private static void SetToggle(Toggle toggle, bool value)
    {
        if (toggle != null)
            toggle.isOn = value;
    }
}
