using UnityEngine;

/// <summary>
/// Membuat RectTransform ini mengikuti pergerakan panel target.
/// Posisi awal tetap, tapi saat panel target bergeser (misal slide),
/// objek ini ikut bergeser dengan delta yang sama.
/// Saat panel target tidak aktif, posisi kembali ke awal.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FollowPanel : MonoBehaviour
{
    [Tooltip("Panel yang diikuti pergerakannya (misal UI Left)")]
    [SerializeField] private RectTransform targetPanel;

    private RectTransform _rect;
    private Vector2 _initialTargetPos;
    private Vector2 _initialSelfPos;
    private bool _initialized;

    private void Start()
    {
        _rect = GetComponent<RectTransform>();
        CacheInitialPositions();
    }

    private void CacheInitialPositions()
    {
        if (targetPanel == null || _rect == null) return;

        _initialTargetPos = targetPanel.anchoredPosition;
        _initialSelfPos = _rect.anchoredPosition;
        _initialized = true;
    }

    private void LateUpdate()
    {
        if (!_initialized || targetPanel == null || _rect == null) return;

        // Saat panel target tidak aktif, kembalikan ke posisi awal
        if (!targetPanel.gameObject.activeInHierarchy)
        {
            _rect.anchoredPosition = _initialSelfPos;
            return;
        }

        // Ikuti pergerakan panel target
        Vector2 delta = targetPanel.anchoredPosition - _initialTargetPos;
        _rect.anchoredPosition = _initialSelfPos + delta;
    }
}
