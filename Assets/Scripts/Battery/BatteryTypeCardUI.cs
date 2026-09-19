using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach script ini ke GameObject Root dari Prefab Card Baterai.
/// </summary>
public class BatteryTypeCardUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag UI Text yang berisi 'Battery Value' ke sini")]
    public TextMeshProUGUI titleText;

    [Tooltip("Drag UI Text yang berisi 'Deskripsi baterai' ke sini")]
    public TextMeshProUGUI descriptionText;

    [Tooltip("Drag Tombol 'Pilih' ke sini")]
    [SerializeField] public Button selectButton;

    [Tooltip("Drag Tombol / Visual 'Dipilih' ke sini")]
    [SerializeField] public GameObject selectedButton; // <-- DITAMBAHKAN

    [Header("Highlight / Visual Option (Opsional)")]
    public Image cardBackground;
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.7f, 0.9f, 1f);

    private BattData _data;
    private BatteryCatalogUI _catalogUI;

    public void Initialize(BattData data, BatteryCatalogUI catalogUI)
    {
        _data = data;
        _catalogUI = catalogUI;

        // Set teks berdasarkan ScriptableObject BattData
        if (titleText != null)
            titleText.text = $"{data.battName}";

        if (descriptionText != null)
            descriptionText.text = data.description;

        // Set action ketika tombol "Pilih" diklik
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() =>
            {
                if (_catalogUI != null && _data != null)
                {
                    _catalogUI.SelectBatteryType(_data);
                }
            });
        }
    }

    /// <summary>
    /// Mengatur tampilan kartu saat dipilih / tidak terpilih.
    /// </summary>
    public void RefreshVisual(bool isSelected)
    {
        // 1. Ubah warna background (opsional)
        if (cardBackground != null)
        {
            cardBackground.color = isSelected ? selectedColor : normalColor;
        }

        // 2. Tampilkan/Sembunyikan tombol berdasarkan status terpilih
        if (selectButton != null)
        {
            selectButton.gameObject.SetActive(!isSelected); // Sembunyikan 'Pilih' jika sedang dipilih
        }

        if (selectedButton != null)
        {
            selectedButton.SetActive(isSelected); // Tampilkan 'Dipilih' jika sedang dipilih
        }
    }
}