using UnityEngine;

/// <summary>
/// Kamera orbit 360° — klik kanan + drag untuk putar, scroll untuk zoom.
/// Attach ke Main Camera.  Set 'target' ke titik tengah rumah.
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // Titik pusat orbit (buat Empty di tengah rumah)
    public Vector3 targetOffset = Vector3.up; // Offset supaya kamera ngarah sedikit ke atas

    [Header("Orbit Settings")]
    public float initialYaw    = 180f;        // Sudut awal horizontal (180 = menghadap rumah)
    public float initialPitch  = 35f;         // Sudut awal vertikal (35 = dari atas serong)
    public float rotationSpeed = 5f;
    public float distance      = 12f;        // Jarak awal kamera ke target
    public float minDistance    = 3f;
    public float maxDistance    = 30f;
    public float zoomSpeed     = 3f;

    [Header("Vertical Limits (derajat)")]
    public float minVerticalAngle = 10f;      // Batas bawah (jangan sampai di bawah lantai)
    public float maxVerticalAngle = 80f;      // Batas atas

    [Header("Pan (Geser)")]
    public float panSpeed = 0.3f;
    public float wasdSpeed = 500f;              // Kecepatan gerak WASD
    [SerializeField] KeyCode flyKey = KeyCode.Space;
    [SerializeField] KeyCode downKey = KeyCode.LeftControl;
    public float flyForce = 5f;
    Vector3 velocity;

    // ── Free Mode ──
    [HideInInspector]
    public bool freeMode = false;             // WASD hanya aktif saat free mode

    /// <summary>
    /// Kunci semua input kamera (orbit, zoom, pan, WASD).
    /// Set true saat simulasi berjalan, false setelah simulasi selesai.
    /// </summary>
    [HideInInspector]
    public bool inputLocked = false;

    // ── Internal ──
    private float yaw;       // rotasi horizontal
    private float pitch;     // rotasi vertikal
    private Vector3 panOffset;
    private float _initialDistance;

    // ── Smooth Transition ──
    private bool isTransitioning = false;
    private Vector3 transitionTargetPos;
    private float transitionDuration;
    private float transitionElapsed;
    private Vector3 transitionStartOffset;

    void Start()
    {
        // Pakai sudut awal yang sudah diset di Inspector
        yaw   = initialYaw;
        pitch = initialPitch;
        panOffset = Vector3.zero;
        _initialDistance = distance;

        // Kalau belum ada target, buat otomatis di (0,0,0)
        if (target == null)
        {
            GameObject pivot = new GameObject("CameraTarget");
            pivot.transform.position = Vector3.zero;
            target = pivot.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Handle smooth transition ke ruangan
        if (isTransitioning)
        {
            HandleTransition();
            return;
        }

        // Semua input diblokir saat simulasi berjalan
        if (!inputLocked)
        {
            HandleRotation();
            HandleZoom();
            HandlePan();

            // WASD hanya aktif saat free mode
            if (freeMode)
                HandleWASD();
        }

        ApplyTransform();
    }

    void HandleRotation()
    {
        // Klik kanan + drag untuk rotate di semua mode
        if (Input.GetMouseButton(1))
        {
            yaw   += Input.GetAxis("Mouse X") * rotationSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
            pitch  = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed;
            distance  = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    void HandlePan()
    {
        // Klik tengah (scroll click) + drag untuk geser
        if (Input.GetMouseButton(2))
        {
            float h = -Input.GetAxis("Mouse X") * panSpeed;
            float v = -Input.GetAxis("Mouse Y") * panSpeed;
            panOffset += transform.right * h + transform.up * v;
        }
    }

    void HandleWASD()
    {
        // Gerak kamera pakai WASD
        float h = 0f, v = 0f, upDown = 0f;

        if (Input.GetKey(KeyCode.W)) v =  1f;
        if (Input.GetKey(KeyCode.S)) v = -1f;
        if (Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.D)) h =  1f;
        if (Input.GetKey(flyKey)) upDown = 1f;
        if (Input.GetKey(downKey)) upDown = -1f;

        float speed = wasdSpeed * Time.deltaTime;

        // Shift untuk gerak lebih cepat
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= 2.5f;

        // Gerak berdasarkan arah kamera (horizontal plane)
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        panOffset += (right * h + forward * v + Vector3.up * upDown) * speed;
    }

    void ApplyTransform()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 focusPoint  = target.position + targetOffset + panOffset;
        Vector3 position    = focusPoint - rotation * Vector3.forward * distance;

        transform.rotation = rotation;
        transform.position = position;
    }

    // ── Public: Smooth transition ke posisi baru ──

    /// <summary>
    /// Pindahkan kamera fokus ke posisi tertentu dengan smooth
    /// </summary>
    public void FocusOnPosition(Vector3 worldPosition, float duration = 1.2f)
    {
        // Hitung offset yang dibutuhkan
        transitionStartOffset = panOffset;
        transitionTargetPos = worldPosition - target.position - targetOffset;
        transitionDuration = duration;
        transitionElapsed = 0f;
        isTransitioning = true;
    }

    /// <summary>
    /// Reset kamera ke posisi awal — yaw, pitch, distance, dan panOffset dikembalikan ke nilai awal.
    /// </summary>
    public void ResetToOrigin(float duration = 1.2f)
    {
        yaw      = initialYaw;
        pitch    = initialPitch;
        distance = _initialDistance;
        panOffset = Vector3.zero;
        FocusOnPosition(target.position + targetOffset, duration);
    }

    /// <summary>
    /// Langsung posisikan kamera sesuai parameter orbit saat ini (pitch, yaw, distance, target)
    /// tanpa menunggu LateUpdate. Batalkan transisi yang sedang berjalan.
    /// Gunakan ini setelah restore orbit state dari luar agar kamera tidak tertinggal di posisi lama.
    /// </summary>
    public void SnapToCurrentOrbitState()
    {
        isTransitioning = false;
        panOffset       = Vector3.zero;
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 focusPoint  = target.position + targetOffset;
        transform.rotation  = rotation;
        transform.position  = focusPoint - rotation * Vector3.forward * distance;
    }

    /// <summary>Posisi kamera yang dihitung dari parameter orbit saat ini, tanpa menggerakkan kamera.</summary>
    public Vector3 GetOrbitPosition()
    {
        if (target == null) return transform.position;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 focusPoint  = target.position + targetOffset + panOffset;
        return focusPoint - rotation * Vector3.forward * distance;
    }

    /// <summary>Rotasi kamera yang dihitung dari parameter orbit saat ini.</summary>
    public Quaternion GetOrbitRotation()
    {
        return Quaternion.Euler(pitch, yaw, 0);
    }

    void HandleTransition()
    {
        transitionElapsed += Time.deltaTime;
        float t = transitionElapsed / transitionDuration;

        // Smooth easing
        t = Mathf.Clamp01(t);
        t = t * t * (3f - 2f * t);

        panOffset = Vector3.Lerp(transitionStartOffset, transitionTargetPos, t);

        ApplyTransform();

        if (transitionElapsed >= transitionDuration)
        {
            panOffset = transitionTargetPos;
            isTransitioning = false;
        }
    }
}
