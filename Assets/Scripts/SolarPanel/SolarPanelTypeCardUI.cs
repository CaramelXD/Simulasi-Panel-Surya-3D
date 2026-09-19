using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Card UI untuk satu jenis panel surya di catalog.
/// Menampilkan info panel dan tombol untuk memilih jenis panel aktif.
/// </summary>
public class SolarPanelTypeCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI specText;
    public TextMeshProUGUI descriptionText;
    public Button selectButton;
    public TextMeshProUGUI selectButtonText;
    public GameObject hoverOverlay;
    public Image cardBackground;
    public Image buttonColor;

    private SolarPanelData data;
    private SolarPanelCatalogUI catalog;
    private bool isSelected;
    private Outline cardOutline;

    private static readonly Color SelectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);   // Abu-abu muda
    private static readonly Color DefaultColor = Color.white;                            // Putih
    private static readonly Color SelectedButtonColor = new Color(0.2f, 0.6f, 0.9f, 1f);
    private static readonly Color DefaultButtonColor = new Color(0.15f, 0.7f, 0.35f, 1f);
    private static readonly Color OutlineColor = new Color(0.35f, 0.35f, 0.35f, 1f);    // Abu-abu gelap

    /// <summary>
    /// Inisialisasi card dengan data panel surya dan referensi catalog.
    /// </summary>
    public void Initialize(SolarPanelData panelData, SolarPanelCatalogUI catalogUI)
    {
        data = panelData;
        catalog = catalogUI;

        SetupOutline();

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;

        if (nameText != null)
            nameText.text = data.panelName;

        if (specText != null)
            specText.text = $"{data.wattOutput}W | {data.efficiency * 100f:F0}%";

        if (descriptionText != null)
            descriptionText.text = data.description;

        if (hoverOverlay != null)
            hoverOverlay.SetActive(false);

        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectClicked);

        RefreshVisual(false);
    }

    /// <summary>
    /// Membuat Outline component pada card background untuk border abu-abu gelap.
    /// </summary>
    private void SetupOutline()
    {
        GameObject outlineTarget = cardBackground != null ? cardBackground.gameObject : gameObject;

        //cardOutline = outlineTarget.GetComponent<Outline>();
        //if (cardOutline == null)
        //    cardOutline = outlineTarget.AddComponent<Outline>();

        //cardOutline.effectColor = OutlineColor;
        //cardOutline.effectDistance = new Vector2(2f, 2f);
        //cardOutline.useGraphicAlpha = false;
    }

    /// <summary>
    /// Dipanggil saat tombol pilih ditekan.
    /// </summary>
    private void OnSelectClicked()
    {
        if (catalog != null)
            catalog.SelectPanelType(data);
    }

    /// <summary>
    /// Update tampilan card berdasarkan apakah jenis ini sedang terpilih.
    /// </summary>
    public void RefreshVisual(bool selected)
    {
        isSelected = selected;

        if (selectButtonText != null)
        {
            if (buttonColor != null)
            {
                buttonColor.color = isSelected ? new Color(0.15f, 0.7f, 0.35f, 1f) : new Color(0.6980392f, 0.1490196f, 0.1553267f, 1f);
            }
            selectButtonText.text = isSelected ? "Terpilih" : "Pilih";
        }

        if (selectButton != null)
        {
            var btnImage = selectButton.GetComponent<Image>();
            if (btnImage != null)
                btnImage.color = isSelected ? SelectedButtonColor : DefaultButtonColor;
        }

        //if (cardBackground != null)
        //    cardBackground.color = isSelected ? SelectedColor : DefaultColor;
    }

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
