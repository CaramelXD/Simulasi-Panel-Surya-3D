using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PDFViewerManager : MonoBehaviour
{
    public static PDFViewerManager Instance;

    [Header("Referensi UI Navigation")]
    public GameObject pdfPanel;
    public Image pageDisplay;
    public Button nextButton;
    public Button prevButton;
    public Button closeButton;
    public TextMeshProUGUI pageText;

    [Header("Referensi UI Zoom")]
    public Button zoomInButton;
    public Button zoomOutButton;
    public Button resetZoomButton;

    [Header("Pengaturan Zoom")]
    [Tooltip("Batas zoom terkecil (1 = ukuran asli)")]
    public float minZoom = 1.0f;

    [Tooltip("Batas zoom terbesar (3 = 300%)")]
    public float maxZoom = 3.0f;

    [Tooltip("Besar penambahan/pengurangan zoom setiap kali tombol diklik")]
    public float zoomStep = 0.25f;

    // --- FITUR: Sistem Dokumen ---
    [System.Serializable]
    public class PDFDocument
    {
        public string documentID;       // Contoh: "Materi" atau "Petunjuk"
        public List<Sprite> pages;      // Gambar halaman khusus untuk dokumen ini
    }

    [Header("Daftar Buku PDF")]
    public List<PDFDocument> documents = new List<PDFDocument>();

    private List<Sprite> currentActivePages;
    private int currentPageIndex = 0;
    private float currentZoom = 1.0f;
    private Canvas parentCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePDF);

        if (zoomInButton != null) zoomInButton.onClick.AddListener(ZoomIn);
        if (zoomOutButton != null) zoomOutButton.onClick.AddListener(ZoomOut);
        if (resetZoomButton != null) resetZoomButton.onClick.AddListener(ResetZoom);

        if (pdfPanel != null) pdfPanel.SetActive(false);

        // Cari Canvas induk untuk perhitungan responsivitas drag
        if (pageDisplay != null)
        {
            parentCanvas = pageDisplay.canvas;
            SetupDragEvents();
        }
    }

    /// <summary>
    /// Buka PDF berdasarkan ID. Contoh: OpenPDF("Materi")
    /// </summary>
    public void OpenPDF(string docID)
    {
        PDFDocument doc = documents.Find(d => d.documentID == docID);

        if (doc != null && doc.pages.Count > 0)
        {
            currentActivePages = doc.pages;
            currentPageIndex = 0;

            ResetZoom(); // Reset posisi dan skala ke normal

            pdfPanel.SetActive(true);
            UpdatePageDisplay();
        }
        else
        {
            Debug.LogWarning($"[PDFViewer] Dokumen '{docID}' tidak ditemukan atau halamannya kosong!");
        }
    }

    public void ClosePDF()
    {
        pdfPanel.SetActive(false);
        ResetZoom();
    }

    public void NextPage()
    {
        if (currentActivePages != null && currentPageIndex < currentActivePages.Count - 1)
        {
            currentPageIndex++;
            ResetZoom(); // Reset posisi & zoom saat ganti halaman
            UpdatePageDisplay();
        }
    }

    public void PrevPage()
    {
        if (currentActivePages != null && currentPageIndex > 0)
        {
            currentPageIndex--;
            ResetZoom(); // Reset posisi & zoom saat ganti halaman
            UpdatePageDisplay();
        }
    }

    // ?? FITUR ZOOM & PAN (SCROLL) ??????????????????????????????????????????

    public void ZoomIn()
    {
        currentZoom = Mathf.Min(currentZoom + zoomStep, maxZoom);
        ApplyZoom();
    }

    public void ZoomOut()
    {
        currentZoom = Mathf.Max(currentZoom - zoomStep, minZoom);
        ApplyZoom();
    }

    public void ResetZoom()
    {
        currentZoom = minZoom;

        // Kembalikan posisi gambar ke tengah
        if (pageDisplay != null)
        {
            pageDisplay.rectTransform.anchoredPosition = Vector2.zero;
        }

        ApplyZoom();
    }

    private void ApplyZoom()
    {
        if (pageDisplay != null)
        {
            pageDisplay.rectTransform.localScale = Vector3.one * currentZoom;

            // Jaga agar gambar tidak melompat keluar batas saat diperkecil
            pageDisplay.rectTransform.anchoredPosition = ClampPosition(pageDisplay.rectTransform.anchoredPosition);
        }

        if (zoomInButton != null) zoomInButton.interactable = (currentZoom < maxZoom);
        if (zoomOutButton != null) zoomOutButton.interactable = (currentZoom > minZoom);
        if (resetZoomButton != null) resetZoomButton.interactable = !Mathf.Approximately(currentZoom, minZoom);
    }

    // ?? LOGIKA DRAG / SCROLL DENGAN MOUSE ATAU SENTUHAN ??????????????????

    private void SetupDragEvents()
    {
        EventTrigger trigger = pageDisplay.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = pageDisplay.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry dragEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Drag
        };

        dragEntry.callback.AddListener((data) => { OnPageDragged((PointerEventData)data); });
        trigger.triggers.Add(dragEntry);
    }

    private void OnPageDragged(PointerEventData eventData)
    {
        // Hanya bisa digeser/discroll jika sedang di-zoom in
        if (currentZoom <= minZoom || pageDisplay == null) return;

        float scaleFactor = (parentCanvas != null) ? parentCanvas.scaleFactor : 1f;
        Vector2 delta = eventData.delta / scaleFactor;

        Vector2 targetPosition = pageDisplay.rectTransform.anchoredPosition + delta;
        pageDisplay.rectTransform.anchoredPosition = ClampPosition(targetPosition);
    }

    /// <summary>
    /// Mencegah halaman digeser melebihi batas tepi gambar.
    /// </summary>
    private Vector2 ClampPosition(Vector2 targetPos)
    {
        if (pageDisplay == null || pageDisplay.transform.parent == null) return targetPos;

        RectTransform parentRect = pageDisplay.transform.parent as RectTransform;
        RectTransform imageRect = pageDisplay.rectTransform;

        Vector2 parentSize = parentRect.rect.size;
        Vector2 imageSize = new Vector2(imageRect.rect.width * currentZoom, imageRect.rect.height * currentZoom);

        float maxX = Mathf.Max(0f, (imageSize.x - parentSize.x) / 2f);
        float maxY = Mathf.Max(0f, (imageSize.y - parentSize.y) / 2f);

        float clampedX = Mathf.Clamp(targetPos.x, -maxX, maxX);
        float clampedY = Mathf.Clamp(targetPos.y, -maxY, maxY);

        return new Vector2(clampedX, clampedY);
    }

    // ?? DISPLAY UPDATE ??????????????????????????????????????????????????????

    private void UpdatePageDisplay()
    {
        if (currentActivePages == null) return;

        if (pageDisplay != null)
        {
            pageDisplay.sprite = currentActivePages[currentPageIndex];
        }

        if (pageText != null)
        {
            pageText.text = $"{currentPageIndex + 1} / {currentActivePages.Count}";
        }

        if (prevButton != null)
        {
            prevButton.interactable = (currentPageIndex > 0);
        }

        if (nextButton != null)
        {
            nextButton.interactable = (currentPageIndex < currentActivePages.Count - 1);
        }
    }
}