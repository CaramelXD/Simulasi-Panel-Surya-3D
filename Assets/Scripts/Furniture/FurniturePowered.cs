using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Komponen yang di-attach ke furnitur yang sudah di-place.
/// Menampilkan icon Electricity di atas furnitur saat battery memiliki daya.
/// Icon menggunakan Canvas world-space + Image, dengan efek bob dan billboard ke kamera.
/// </summary>
public class FurniturePowered : MonoBehaviour
{
    [Header("Icon")]
    [Tooltip("Sprite yang ditampilkan. Diisi otomatis dari Assets/_Images/Electricity.png.")]
    [SerializeField] private Sprite iconSprite;

    [Tooltip("Warna tint icon.")]
    [SerializeField] private Color iconColor = new Color(1f, 0.85f, 0.05f, 1f);

    [Tooltip("Ukuran icon dalam world units.")]
    [SerializeField] private float iconSize = 40f;

    [Tooltip("Jarak tambahan di atas bounding box furnitur (world units).")]
    [SerializeField] private float iconHeightOffset = 30f;

    // ── Runtime ────────────────────────────────────────────────────────────
    private GameObject _iconRoot;
    private bool _isPowered = false;

    /// <summary>
    /// Apakah furnitur ini sedang aktif (dalam jam pakainya).
    /// Default true — sehingga di luar simulasi, ikon tetap mengikuti state baterai.
    /// </summary>
    private bool _isFurnitureActive = true;

    private Vector3 _iconWorldPos;

    // Durasi bounce animation di FurnitureManager — icon dibuat setelah selesai
    private const float BounceAnimationDuration = 0.4f;

    // Animasi bob
    private const float BobSpeed = 2.5f;
    private const float BobAmplitude = 8f;
    private float _bobTime = 0f;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (BatteryManager.Instance != null)
        {
            BatteryManager.Instance.OnPowerStateChanged.AddListener(SetPowered);
            SetPowered(BatteryManager.Instance.HasPower);
        }
    }

    private void OnDisable()
    {
        if (BatteryManager.Instance != null)
            BatteryManager.Instance.OnPowerStateChanged.RemoveListener(SetPowered);
    }

    private void Start()
    {
        // Auto-load sprite jika belum di-assign di Inspector
        if (iconSprite == null)
            iconSprite = LoadSpriteFromAssets();

        StartCoroutine(InitIconAfterAnimation());
    }

    private void OnDestroy()
    {
        if (_iconRoot != null)
            Destroy(_iconRoot);
    }

    private void Update()
    {
        if (_iconRoot == null || !_iconRoot.activeSelf) return;
        if (Camera.main == null) return;

        // Bob naik-turun
        _bobTime += Time.deltaTime;
        Vector3 pos = _iconWorldPos;
        pos.y += Mathf.Sin(_bobTime * BobSpeed) * BobAmplitude;
        _iconRoot.transform.position = pos;

        // Billboard: selalu hadap ke kamera
        _iconRoot.transform.rotation = Camera.main.transform.rotation;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Opsional: set sprite ikon dari luar (misal dari FurnitureManager).
    /// Harus dipanggil sebelum Start() membuat ikon. Jika tidak dipanggil,
    /// sprite di-load otomatis dari Assets/_Images/Electricity.png.
    /// </summary>
    public void Initialize(Sprite sprite)
    {
        if (sprite != null)
            iconSprite = sprite;
    }

    /// <summary>Dipanggil oleh BatteryManager saat state daya berubah.</summary>
    public void SetPowered(bool powered)
    {
        if (_isPowered == powered) return;
        _isPowered = powered;
        RefreshIconVisibility();
    }

    /// <summary>
    /// Dipanggil oleh FurnitureManager setiap frame simulasi.
    /// Menentukan apakah furnitur ini sedang aktif berdasarkan jam pakainya.
    /// </summary>
    public void SetFurnitureActive(bool active)
    {
        if (_isFurnitureActive == active) return;
        _isFurnitureActive = active;
        RefreshIconVisibility();
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    private IEnumerator InitIconAfterAnimation()
    {
        yield return new WaitForSeconds(BounceAnimationDuration);

        _iconWorldPos = CalculateIconWorldPosition();
        CreateIcon();
        RefreshIconVisibility();
    }

    private void CreateIcon()
    {
        // Root tidak diparent ke furnitur agar tidak kena pengaruh scale-nya
        _iconRoot = new GameObject($"PowerIcon_{gameObject.name}");
        _iconRoot.transform.position = _iconWorldPos;
        _iconRoot.transform.rotation = Quaternion.identity;
        _iconRoot.transform.localScale = Vector3.one;

        // Canvas world-space
        Canvas canvas = _iconRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = _iconRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(iconSize, iconSize);

        // Image child
        GameObject imgObj = new GameObject("ElectricityIcon");
        imgObj.transform.SetParent(_iconRoot.transform, false);

        Image img = imgObj.AddComponent<Image>();
        img.sprite = iconSprite;
        img.color = iconColor;
        img.preserveAspect = true;
        img.raycastTarget = false;

        RectTransform imgRect = imgObj.GetComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.offsetMin = Vector2.zero;
        imgRect.offsetMax = Vector2.zero;
    }

    private Vector3 CalculateIconWorldPosition()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return transform.position + Vector3.up * iconHeightOffset;

        Bounds combined = renderers[0].bounds;
        foreach (Renderer r in renderers)
            combined.Encapsulate(r.bounds);

        return new Vector3(combined.center.x, combined.max.y + iconHeightOffset, combined.center.z);
    }

    private void SetIconVisible(bool visible)
    {
        if (_iconRoot != null)
            _iconRoot.SetActive(visible);
    }

    /// <summary>Icon muncul hanya jika baterai ada daya DAN furnitur sedang aktif.</summary>
    private void RefreshIconVisibility()
    {
        SetIconVisible(_isPowered && _isFurnitureActive);
    }

    private Sprite LoadSpriteFromAssets()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Images/Electricity.png");
#else
        return null;
#endif
    }
}
