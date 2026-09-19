using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catalog panel yang menampilkan semua jenis baterai yang tersedia.
/// Attach ke GameObject Canvas > UI Left Battery > CatalogPanel.
/// </summary>
public class BatteryCatalogUI : MonoBehaviour
{
    [Header("Data Baterai")]
    [Tooltip("Daftar semua BattData ScriptableObject")]
    public List<BattData> battTypes = new();

    [Header("UI References")]
    [Tooltip("Prefab card jenis baterai (harus punya komponen BatteryTypeCardUI)")]
    public GameObject cardPrefab;

    [Tooltip("Content object dalam ScrollView yang jadi parent card")]
    public Transform contentParent;

    [Header("Installer Reference")]
    [Tooltip("Referensi BatteryInstaller untuk mengubah jenis baterai aktif")]
    public BatteryInstaller batteryInstaller;

    private readonly List<BatteryTypeCardUI> cards = new();
    private BattData selectedType;

    private void Start()
    {
        // Auto-assign BatteryInstaller jika belum di-assign di Inspector
        if (batteryInstaller == null)
            batteryInstaller = BatteryInstaller.Instance;

        BuildCatalog();

        // Auto-select tipe pertama
        if (battTypes.Count > 0)
            SelectBatteryType(battTypes[0]);
    }

    /// <summary>
    /// Bangun catalog card dari daftar battTypes.
    /// </summary>
    private void BuildCatalog()
    {
        if (cardPrefab == null || contentParent == null)
        {
            Debug.LogWarning("[BatteryCatalogUI] cardPrefab atau contentParent belum di-assign!");
            return;
        }

        // Hapus card lama
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        cards.Clear();

        foreach (var data in battTypes)
        {
            if (data == null) continue;

            GameObject cardGO = Instantiate(cardPrefab, contentParent);
            var card = cardGO.GetComponent<BatteryTypeCardUI>();

            if (card != null)
            {
                card.Initialize(data, this);
                cards.Add(card);
            }
            else
            {
                Debug.LogWarning("[BatteryCatalogUI] cardPrefab tidak memiliki komponen BatteryTypeCardUI!");
            }
        }
    }

    /// <summary>
    /// Pilih jenis baterai aktif. Dipanggil dari BatteryTypeCardUI.
    /// </summary>
    public void SelectBatteryType(BattData battData)
    {
        selectedType = battData;

        // Update visual semua card
        for (int i = 0; i < cards.Count; i++)
        {
            bool isSelected = (i < battTypes.Count) && (battTypes[i] == selectedType);
            cards[i].RefreshVisual(isSelected);
        }

        // 1. Update installer dengan prefab dan spesifikasi baru
        if (batteryInstaller != null && battData != null)
        {
            batteryInstaller.SetActiveBattType(battData);
            Debug.Log($"[BatteryCatalogUI] Baterai terpilih: {battData.battName} (Kapasitas: {battData.capacity})");
        }

        // 2. TAMBAHKAN BARIS INI: Update BatteryManager agar kapasitas game ikut berubah!
        if (BatteryManager.Instance != null && battData != null)
        {
            BatteryManager.Instance.SetBatteryData(battData);
        }
    }

    /// <summary>
    /// Kembalikan data jenis baterai yang sedang terpilih.
    /// </summary>
    public BattData GetSelectedType() => selectedType;
}