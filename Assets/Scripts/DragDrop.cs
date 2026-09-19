using UnityEngine;
using UnityEngine.EventSystems;

// Menggunakan EventSystem interfaces agar kompatibel dengan
// New Input System + Physics Raycaster yang ada di kamera
public class DragDrop : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Settings")]
    [Tooltip("Kunci posisi Y supaya objek tidak melayang saat di-drag")]
    public bool lockYPosition = true;

    [Tooltip("Offset Y dari posisi asli saat di-drag (atur jika objek melayang/amblas)")]
    public float groundOffset = 0f;

    // ── internal state ──────────────────────────────────────
    private Camera mainCam;
    private float lockedY;
    private Rigidbody rb;
    private bool wasKinematic;

    // ── highlight ────────────────────────────────────────────
    private Renderer[] renderers;
    private bool isDragging = false;

    void Start()
    {
        mainCam = Camera.main;
        rb = GetComponent<Rigidbody>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    // ── Saat mouse / touch mulai menekan objek ───────────────
    public void OnPointerDown(PointerEventData eventData)
    {
        lockedY = transform.position.y;

        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        isDragging = true;
        SetHighlight(true);
    }

    // ── Selama di-drag ───────────────────────────────────────
    public void OnDrag(PointerEventData eventData)
    {
        // Buat ray dari kamera ke posisi pointer
        Ray ray = mainCam.ScreenPointToRay(eventData.position);

        // Hitung titik di dunia pada ketinggian Y yang dikunci
        float targetY = lockYPosition ? lockedY : transform.position.y;

        // Buat plane horizontal setinggi objek
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, targetY, 0));

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            transform.position = new Vector3(hitPoint.x, targetY + groundOffset, hitPoint.z);
        }
    }

    // ── Saat mouse / touch dilepas ───────────────────────────
    public void OnPointerUp(PointerEventData eventData)
    {
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
            rb.linearVelocity = Vector3.zero;
        }

        isDragging = false;
        SetHighlight(false);
    }

    // ── Hover masuk / keluar ─────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging) SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging) SetHighlight(false);
    }

    // ── Highlight helper (emissi glow) ───────────────────────
    void SetHighlight(bool on)
    {
        foreach (var r in renderers)
        {
            if (on) r.material.EnableKeyword("_EMISSION");
            else    r.material.DisableKeyword("_EMISSION");
        }
    }
}

