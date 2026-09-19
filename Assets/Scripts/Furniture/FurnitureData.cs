using UnityEngine;

/// <summary>
/// ScriptableObject yang menyimpan data satu jenis furnitur.
/// Buat via: klik kanan di Project → Create → Simulator → Furniture Data
/// </summary>
[CreateAssetMenu(fileName = "NewFurniture", menuName = "Simulator/Furniture Data")]
public class FurnitureData : ScriptableObject
{
    [Header("Identitas")]
    public string furnitureName = "Kulkas";
    public Sprite icon;                  // Gambar untuk ditampilkan di UI catalog

    [Header("Prefab 3D")]
    public GameObject prefab3D;          // Model 3D yang akan di-spawn di scene

    [Header("Posisi di Dalam Rumah")]
    [Tooltip("Posisi world-space tempat furnitur ini akan ditempatkan di rumah")]
    public Vector3 fixedPosition = Vector3.zero;
    public Vector3 fixedRotation = Vector3.zero;
    public Vector3 fixedScale = Vector3.one;    // Ukuran saat di-spawn (default 1,1,1)

    [Header("Konsumsi Listrik")]
    [Tooltip("Konsumsi daya dalam Watt")]
    public float wattConsumption = 100f;

    [Header("Deskripsi")]
    [TextArea(2, 4)]
    public string description = "Deskripsi furnitur";
}
