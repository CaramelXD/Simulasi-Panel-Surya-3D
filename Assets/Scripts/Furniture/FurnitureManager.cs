using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SolarEdu;

/// <summary>
/// Manager singleton yang mengurus semua furnitur yang sudah di-install di rumah.
/// Attach ke GameObject kosong bernama "FurnitureManager" di scene.
/// </summary>
public class FurnitureManager : MonoBehaviour
{
    public static FurnitureManager Instance { get; private set; }

    // ── Events ─────────────────────────────────────────────────────────────
    /// <summary>Dipanggil setiap total watt berubah. Parameter: total watt.</summary>
    public UnityEvent<float> OnPowerChanged;

    /// <summary>Dipanggil setiap jumlah furnitur berubah. Parameter: jumlah furnitur.</summary>
    public UnityEvent<int> OnFurnitureCountChanged;

    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Electricity Icon")]
    [Tooltip("Sprite ikon listrik yang ditampilkan di atas furnitur saat nyala.")]
    [SerializeField] private Sprite electricityIconSprite;

    // ── State ──────────────────────────────────────────────────────────────
    // Key: FurnitureData, Value: GameObject instance yang sudah di-spawn
    private Dictionary<FurnitureData, GameObject> placedFurniture = new();

    // Key: FurnitureData, Value: (jam mulai, jam selesai) dalam format 0-24
    private readonly Dictionary<FurnitureData, (float startHour, float endHour)> _usageHours = new();

    private SunController _sunController;

    public float TotalWattConsumed { get; private set; } = 0f;

    /// <summary>Jumlah furnitur yang saat ini terpasang di rumah.</summary>
    public int PlacedCount => placedFurniture.Count;

    /// <summary>Daftar FurnitureData yang saat ini terpasang di rumah.</summary>
    public IReadOnlyCollection<FurnitureData> GetPlacedFurnitureData() => placedFurniture.Keys;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Update state aktif tiap furnitur hanya saat simulasi berjalan
        if (SimulationManager.Instance == null || !SimulationManager.Instance.IsSimulationRunning)
            return;

        var sun = SunController.Instance;
        if (sun == null) return;

        float currentHour = sun.TimeOfDay;

        foreach (var kvp in placedFurniture)
        {
            var (start, end) = GetUsageHours(kvp.Key);
            bool isActive = currentHour >= start && currentHour < end;

            var fp = kvp.Value != null ? kvp.Value.GetComponent<FurniturePowered>() : null;
            if (fp != null)
                fp.SetFurnitureActive(isActive);
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Cek apakah furnitur ini sudah ada di rumah</summary>
    public bool IsPlaced(FurnitureData data) => placedFurniture.ContainsKey(data);

    /// <summary>
    /// Tambahkan furnitur ke rumah.
    /// Spawn prefab di fixedPosition dan catat ke dictionary.
    /// </summary>
    public void AddFurniture(FurnitureData data)
    {
        if (IsPlaced(data))
        {
            Debug.Log($"[FurnitureManager] {data.furnitureName} sudah ada di rumah.");
            return;
        }

        if (data.prefab3D == null)
        {
            Debug.LogWarning($"[FurnitureManager] {data.furnitureName} tidak punya prefab3D!");
            return;
        }

        // Spawn di posisi dan rotasi yang sudah ditentukan
        Quaternion rot = Quaternion.Euler(data.fixedRotation);
        GameObject instance = Instantiate(data.prefab3D, data.fixedPosition, rot);
        instance.transform.localScale = data.fixedScale;
        instance.name = $"[Placed] {data.furnitureName}";

        placedFurniture[data] = instance;

        // Tambahkan komponen visual powered jika belum ada di prefab
        FurniturePowered fp = instance.GetComponent<FurniturePowered>();
        if (fp == null) fp = instance.AddComponent<FurniturePowered>();
        fp.Initialize(electricityIconSprite);

        // Hitung ulang total daya
        TotalWattConsumed += data.wattConsumption;
        OnPowerChanged?.Invoke(TotalWattConsumed);
        OnFurnitureCountChanged?.Invoke(placedFurniture.Count);

        // Animasi kemunculan
        PlayPlaceAnimation(instance);

        Debug.Log($"[FurnitureManager] {data.furnitureName} ({data.wattConsumption}W) ditambahkan. Total: {TotalWattConsumed}W");

        // ── SolarEdu Tracking ──
        if (SolarEduManager.Instance != null)
        {
            SolarEduManager.Instance.SendStatement(
                SolarEduVerb.Memasang,
                $"solaredu://furniture/{data.furnitureName.Replace(" ", "-").ToLower()}",
                $"{data.furnitureName} ({data.wattConsumption}W)"
            );
        }
    }

    /// <summary>
    /// Hapus furnitur dari rumah.
    /// Destroy instance dan kurangi watt-nya.
    /// </summary>
    public void RemoveFurniture(FurnitureData data)
    {
        if (!IsPlaced(data))
        {
            Debug.Log($"[FurnitureManager] {data.furnitureName} tidak ada di rumah.");
            return;
        }

        Destroy(placedFurniture[data]);
        placedFurniture.Remove(data);
        _usageHours.Remove(data);

        TotalWattConsumed -= data.wattConsumption;
        if (TotalWattConsumed < 0) TotalWattConsumed = 0;

        OnPowerChanged?.Invoke(TotalWattConsumed);
        OnFurnitureCountChanged?.Invoke(placedFurniture.Count);

        Debug.Log($"[FurnitureManager] {data.furnitureName} dihapus. Total: {TotalWattConsumed}W");

        // ── SolarEdu Tracking ──
        if (SolarEduManager.Instance != null)
        {
            SolarEduManager.Instance.SendStatement(
                SolarEduVerb.Mengamati,
                $"solaredu://furniture/{data.furnitureName.Replace(" ", "-").ToLower()}/hapus",
                $"Hapus {data.furnitureName}"
            );
        }
    }

    // ── Usage Hours API ────────────────────────────────────────────────────

    /// <summary>Atur jam aktif furnitur (format 0-24). Dipanggil dari FurnitureConfigPanel.</summary>
    public void SetUsageHours(FurnitureData data, float startHour, float endHour)
    {
        _usageHours[data] = (startHour, endHour);
        Debug.Log($"[FurnitureManager] {data.furnitureName} aktif pukul {startHour:0.##} - {endHour:0.##}");
    }

    /// <summary>Ambil jam aktif furnitur. Default 0-24 (sepanjang hari) jika belum dikonfigurasi.</summary>
    public (float startHour, float endHour) GetUsageHours(FurnitureData data)
    {
        return _usageHours.TryGetValue(data, out var hours) ? hours : (0f, 24f);
    }

    /// <summary>
    /// Hitung total watt furnitur yang sedang aktif pada jam simulasi tertentu.
    /// Digunakan oleh BatteryManager untuk menghitung drain per frame.
    /// </summary>
    public float GetActiveWattConsumed(float currentSimHour)
    {
        float total = 0f;
        foreach (var kvp in placedFurniture)
        {
            var (start, end) = GetUsageHours(kvp.Key);
            if (currentSimHour >= start && currentSimHour < end)
                total += kvp.Key.wattConsumption;
        }
        return total;
    }

    // ── Private Helpers ────────────────────────────────────────────────────    /// Animasi scale bounce saat furnitur muncul
    void PlayPlaceAnimation(GameObject obj)
    {
        StartCoroutine(BounceScale(obj));
    }

    System.Collections.IEnumerator BounceScale(GameObject obj)
    {
        if (obj == null) yield break;

        Vector3 originalScale = obj.transform.localScale;
        obj.transform.localScale = Vector3.zero;

        float t = 0f;
        float duration = 0.35f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;

            // Kurva overshoot (elastic kecil)
            float scale = Mathf.Sin(progress * Mathf.PI * 0.5f);
            if (progress > 0.7f)
                scale = 1f + Mathf.Sin((progress - 0.7f) / 0.3f * Mathf.PI) * 0.15f;

            obj.transform.localScale = originalScale * scale;
            yield return null;
        }

        obj.transform.localScale = originalScale;
    }
}
