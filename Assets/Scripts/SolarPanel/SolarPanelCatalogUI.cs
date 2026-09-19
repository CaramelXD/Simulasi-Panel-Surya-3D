using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catalog panel yang menampilkan semua jenis panel surya yang tersedia.
/// Attach ke GameObject Canvas > UI Left SP > CatalogPanel.
/// </summary>
public class SolarPanelCatalogUI : MonoBehaviour
{
    [Header("Data Panel Surya")]
    [Tooltip("Daftar semua SolarPanelData ScriptableObject")]
    public List<SolarPanelData> panelTypes = new();

    [Header("UI References")]
    [Tooltip("Prefab card jenis panel surya (harus punya SolarPanelTypeCardUI component)")]
    public GameObject cardPrefab;

    [Tooltip("Content object dalam ScrollView yang jadi parent card")]
    public Transform contentParent;

    [Header("Installer Reference")]
    [Tooltip("Referensi SolarPanelInstaller untuk mengubah jenis panel aktif")]
    public SolarPanelInstaller solarPanelInstaller;

    private readonly List<SolarPanelTypeCardUI> cards = new();
    private SolarPanelData selectedType;

    private void Start()
    {
        BuildCatalog();

        // Auto-select tipe pertama
        if (panelTypes.Count > 0)
            SelectPanelType(panelTypes[0]);
    }

    /// <summary>
    /// Bangun catalog card dari daftar panelTypes.
    /// </summary>
    private void BuildCatalog()
    {
        if (cardPrefab == null || contentParent == null)
        {
            Debug.LogWarning("[SolarPanelCatalogUI] cardPrefab atau contentParent belum di-assign!");
            return;
        }

        // Hapus card lama
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        cards.Clear();

        foreach (var data in panelTypes)
        {
            if (data == null) continue;

            GameObject cardGO = Instantiate(cardPrefab, contentParent);
            var card = cardGO.GetComponent<SolarPanelTypeCardUI>();

            if (card != null)
            {
                card.Initialize(data, this);
                cards.Add(card);
            }
            else
            {
                Debug.LogWarning("[SolarPanelCatalogUI] cardPrefab tidak punya komponen SolarPanelTypeCardUI!");
            }
        }
    }

    /// <summary>
    /// Pilih jenis panel surya aktif. Dipanggil dari SolarPanelTypeCardUI.
    /// </summary>
    public void SelectPanelType(SolarPanelData panelData)
    {
        selectedType = panelData;

        // Update visual semua card
        for (int i = 0; i < cards.Count; i++)
        {
            bool isSelected = panelTypes[i] == selectedType;
            cards[i].RefreshVisual(isSelected);
        }

        // Update installer dengan prefab dan spesifikasi baru
        if (solarPanelInstaller != null && panelData != null)
        {
            solarPanelInstaller.SetActivePanelType(panelData);
            Debug.Log($"[SolarPanelCatalogUI] Panel terpilih: {panelData.panelName} ({panelData.wattOutput}W, {panelData.efficiency * 100f:F0}%)");
        }
    }

    /// <summary>
    /// Kembalikan data jenis panel yang sedang terpilih.
    /// </summary>
    public SolarPanelData GetSelectedType() => selectedType;
}
