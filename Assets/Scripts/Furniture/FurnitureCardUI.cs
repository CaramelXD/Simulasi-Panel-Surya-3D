using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Satu kartu furnitur di panel catalog.
/// Attach ke prefab Card UI, pasang referensi data via FurnitureCatalogUI.
/// </summary>
public class FurnitureCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI wattText;

    [Header("Button Setup")]
    [Tooltip("Tombol besar hijau saat furnitur belum dipasang")]
    public Button tambahBtn;

    [Tooltip("Wadah yang berisi tombol Konfigurasi dan Hapus saat furnitur sudah dipasang")]
    public GameObject placedButtonsContainer;

    [Tooltip("Tombol biru untuk konfigurasi di dalam container")]
    public Button configButton;

    [Tooltip("Tombol merah untuk hapus di dalam container")]
    public Button deleteBtn;

    [Header("Hover Overlay")]
    public GameObject hoverOverlay; // Panel gelap di atas card saat hover

    // Data furnitur yang diwakili kartu ini
    private FurnitureData data;

    // ── Setup ──────────────────────────────────────────────────────────────
    public void Initialize(FurnitureData furnitureData)
    {
        data = furnitureData;

        // Isi konten teks dan gambar
        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;

        if (nameText != null)
            nameText.text = data.furnitureName;

        if (wattText != null)
            wattText.text = $"{data.wattConsumption} W";

        // Sembunyikan hover overlay
        if (hoverOverlay != null)
            hoverOverlay.SetActive(false);

        // Sambungkan semua tombol
        if (tambahBtn != null)
            tambahBtn.onClick.AddListener(OnTambahClicked);

        if (deleteBtn != null)
            deleteBtn.onClick.AddListener(OnDeleteClicked);

        if (configButton != null)
            configButton.onClick.AddListener(OnConfigClicked);

        // Update tampilan awal tombol
        RefreshButtonState();

        // Dengarkan perubahan state dari sistem
        if (FurnitureManager.Instance != null)
            FurnitureManager.Instance.OnPowerChanged.AddListener(_ => RefreshButtonState());
    }

    // ── Button Logic ───────────────────────────────────────────────────────

    void OnTambahClicked()
    {
        if (FurnitureManager.Instance == null) return;

        // 1. Tambahkan furnitur
        FurnitureManager.Instance.AddFurniture(data);

        // 2. Perbarui struktur tombol (hilangkan Tambah, munculkan Konfigurasi & Hapus)
        RefreshButtonState();

        // 3. Langsung buka panel konfigurasi secara otomatis!
        OpenConfigPanel();
    }

    void OnDeleteClicked()
    {
        if (FurnitureManager.Instance == null) return;

        // Hapus furnitur dari sistem
        FurnitureManager.Instance.RemoveFurniture(data);

        // Kembalikan ke tampilan tombol Tambah
        RefreshButtonState();
    }

    void OnConfigClicked()
    {
        OpenConfigPanel();
    }

    /// <summary>
    /// Fungsi helper untuk membuka panel konfigurasi
    /// </summary>
    private void OpenConfigPanel()
    {
        if (data == null) return;

        var panel = FurnitureConfigPanel.Instance
                    ?? FindFirstObjectByType<FurnitureConfigPanel>(FindObjectsInactive.Include);

        if (panel != null)
            panel.Open(data);
        else
            Debug.LogWarning("[FurnitureCardUI] FurnitureConfigPanel tidak ditemukan di scene.");
    }

    /// <summary>
    /// Mengatur visibilitas tombol berdasarkan status furnitur di FurnitureManager
    /// </summary>
    void RefreshButtonState()
    {
        if (FurnitureManager.Instance == null) return;

        bool alreadyPlaced = FurnitureManager.Instance.IsPlaced(data);

        // Langsung hide/unhide tombolnya satu per satu
        if (tambahBtn != null) tambahBtn.gameObject.SetActive(!alreadyPlaced);
        if (configButton != null) configButton.gameObject.SetActive(alreadyPlaced);
        if (deleteBtn != null) deleteBtn.gameObject.SetActive(alreadyPlaced);
    }

    // ── Hover Effects ──────────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverOverlay != null)
            hoverOverlay.SetActive(true);

        transform.localScale = Vector3.one * 1.03f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverOverlay != null)
            hoverOverlay.SetActive(false);

        transform.localScale = Vector3.one;
    }
}