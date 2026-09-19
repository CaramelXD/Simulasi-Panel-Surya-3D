using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Menambahkan animasi scale (membesar/mengecil) saat mouse hover dan klik pada elemen UI.
/// </summary>
public class UIButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Settings")]
    [Tooltip("Berapa kali lipat tombol membesar saat di-hover. (Contoh: 1.1 = 110%)")]
    [SerializeField] private float hoverMultiplier = 1.05f;

    [Tooltip("Berapa kali lipat tombol mengecil saat ditekan. (Contoh: 0.9 = 90%)")]
    [SerializeField] private float pressedMultiplier = 0.95f;

    [Tooltip("Durasi animasi membesar/mengecil (dalam detik).")]
    [SerializeField] private float animationDuration = 0.1f;

    private Vector3 _originalScale;
    private Vector3 _hoverScale;
    private Vector3 _pressedScale;

    private Coroutine _scaleCoroutine;
    private bool _isHovering = false;

    private void Awake()
    {
        // Simpan ukuran asli saat game dimulai
        _originalScale = transform.localScale;

        // Hitung ukuran target
        _hoverScale = _originalScale * hoverMultiplier;
        _pressedScale = _originalScale * pressedMultiplier;
    }

    private void OnDisable()
    {
        // Jika UI dimatikan (SetActiv(false)), reset ukurannya secara instan ke normal
        transform.localScale = _originalScale;
        _isHovering = false;
    }

    // ─── Event Systems ──────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        AnimateScale(_hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        AnimateScale(_originalScale);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateScale(_pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Saat klik dilepas, cek apakah mouse masih ada di atas tombol atau tidak
        AnimateScale(_isHovering ? _hoverScale : _originalScale);
    }

    // ─── Animation Logic ────────────────────────────────────────────

    private void AnimateScale(Vector3 targetScale)
    {
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }
        _scaleCoroutine = StartCoroutine(ScaleCoroutine(targetScale));
    }

    private IEnumerator ScaleCoroutine(Vector3 target)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            // Menggunakan unscaledDeltaTime agar animasi tetap jalan walau game di-pause (Time.timeScale = 0)
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);

            // Smooth-step easing agar animasinya terasa lebih empuk
            float easedT = t * t * (3f - 2f * t);

            transform.localScale = Vector3.Lerp(startScale, target, easedT);
            yield return null;
        }

        transform.localScale = target;
    }
}