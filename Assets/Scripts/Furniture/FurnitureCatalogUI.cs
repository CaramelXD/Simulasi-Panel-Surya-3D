using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Catalog panel yang menampilkan semua furnitur yang tersedia.
/// Attach ke GameObject Canvas → CatalogPanel.
/// </summary>
public class FurnitureCatalogUI : MonoBehaviour
{
    [Header("Data Furnitur")]
    [Tooltip("Daftar semua FurnitureData ScriptableObject")]
    public List<FurnitureData> furnitureList = new();

    [Header("UI References")]
    [Tooltip("Prefab satu kartu furnitur (harus punya FurnitureCardUI component)")]
    public GameObject cardPrefab;

    [Tooltip("Content object dalam ScrollView yang jadi parent kartu")]
    public Transform contentParent;

    [Header("Power Display")]
    public TextMeshProUGUI totalWattText;
    public TextMeshProUGUI maxWattText;
    public Slider powerBar;

    [Header("Kapasitas Listrik")]
    [Tooltip("Kapasitas maksimum sistem dalam Watt")]
    public float maxSystemWatt = 2000f;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        BuildCatalog();

        if (FurnitureManager.Instance != null)
            FurnitureManager.Instance.OnPowerChanged.AddListener(OnPowerChanged);

        if (maxWattText != null)
            maxWattText.text = $"/ {maxSystemWatt} W";

        UpdatePowerDisplay(0f);
    }

    // ── Catalog Builder ────────────────────────────────────────────────────

    private void BuildCatalog()
    {
        if (cardPrefab == null || contentParent == null)
        {
            Debug.LogWarning("[FurnitureCatalogUI] cardPrefab atau contentParent belum di-assign!");
            return;
        }

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var data in furnitureList)
        {
            if (data == null) continue;

            GameObject cardGO = Instantiate(cardPrefab, contentParent);
            var card = cardGO.GetComponent<FurnitureCardUI>();

            if (card != null)
                card.Initialize(data);
            else
                Debug.LogWarning("[FurnitureCatalogUI] cardPrefab tidak punya komponen FurnitureCardUI!");
        }
    }

    // ── Power Display ──────────────────────────────────────────────────────

    private void OnPowerChanged(float totalWatt)
    {
        UpdatePowerDisplay(totalWatt);
    }

    private void UpdatePowerDisplay(float totalWatt)
    {
        if (totalWattText != null)
            totalWattText.text = $"{totalWatt} W";

        if (powerBar != null)
        {
            powerBar.value = Mathf.Clamp01(totalWatt / maxSystemWatt);

            var fill = powerBar.fillRect.GetComponent<Image>();
            if (fill != null)
            {
                float ratio = totalWatt / maxSystemWatt;
                if (ratio < 0.6f)
                    fill.color = new Color(0.15f, 0.75f, 0.3f);
                else if (ratio < 0.85f)
                    fill.color = new Color(1f, 0.75f, 0f);
                else
                    fill.color = new Color(0.9f, 0.15f, 0.1f);
            }
        }

        if (totalWatt > maxSystemWatt)
            Debug.Log("[FurnitureCatalogUI] OVERLOAD! Daya melebihi kapasitas.");
    }
}
