using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; // Wajib ditambahkan untuk TextMeshPro

[RequireComponent(typeof(UIGradient))]
[RequireComponent(typeof(Graphic))]
public class GradientButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UIGradient _uiGradient;
    private Graphic _graphic;

    private Color _originalColorTop;
    private Color _originalColorBottom;

    [Header("Pengaturan Gradien Tombol")]
    public Color hoverColorTop = new Color(0.3f, 0.7f, 1f);
    public Color hoverColorBottom = new Color(0.0f, 0.4f, 0.8f);

    [Header("Referensi Teks (Wajib TextMeshProUGUI)")]
    public TextMeshProUGUI title;
    public TextMeshProUGUI desc;
    public TextMeshProUGUI symbol;

    [Header("Pengaturan Warna Teks")]
    public Color normalTextColor = Color.gray; // Warna saat mouse tidak menyentuh
    public Color hoverTextColor = Color.white; // Warna saat mouse di atas tombol

    void Awake()
    {
        _uiGradient = GetComponent<UIGradient>();
        _graphic = GetComponent<Graphic>();

        _originalColorTop = _uiGradient.colorTop;
        _originalColorBottom = _uiGradient.colorBottom;

        // Set teks ke warna normal saat mulai
        ChangeTextColor(normalTextColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Ubah gradien
        _uiGradient.colorTop = hoverColorTop;
        _uiGradient.colorBottom = hoverColorBottom;

        // Ubah warna ketiga teks
        ChangeTextColor(hoverTextColor);

        RefreshGradient();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Kembalikan gradien
        _uiGradient.colorTop = _originalColorTop;
        _uiGradient.colorBottom = _originalColorBottom;

        // Kembalikan warna ketiga teks
        ChangeTextColor(normalTextColor);

        RefreshGradient();
    }

    /// <summary>
    /// Fungsi pembantu untuk mengubah warna ketiga teks sekaligus
    /// </summary>
    private void ChangeTextColor(Color newColor)
    {
        if (title != null) title.color = newColor;
        if (desc != null) desc.color = newColor;
        if (symbol != null) symbol.color = newColor;
    }

    private void RefreshGradient()
    {
        if (_graphic != null)
        {
            _graphic.SetVerticesDirty();
        }
    }
}