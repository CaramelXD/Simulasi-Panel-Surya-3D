using UnityEngine;

/// <summary>
/// ScriptableObject yang menyimpan data satu jenis panel surya.
/// Buat via: klik kanan di Project > Create > Simulator > Solar Panel Data
/// </summary>
[CreateAssetMenu(fileName = "NewSolarPanel", menuName = "Simulator/Solar Panel Data")]
public class SolarPanelData : ScriptableObject
{
    [Header("Identitas")]
    public string panelName = "Monocrystalline";
    public Sprite icon;

    [Header("Prefab 3D")]
    [Tooltip("Prefab panel surya yang akan di-spawn di atap")]
    public GameObject prefab3D;

    [Header("Spesifikasi")]
    [Tooltip("Daya output per panel dalam Watt")]
    public float wattOutput = 400f;

    [Tooltip("Efisiensi panel (0-1)")]
    [Range(0f, 1f)]
    public float efficiency = 0.20f;

    [Tooltip("Luas panel dalam m2")]
    public float panelArea = 1.6f;

    [Tooltip("Irradiansi puncak dalam W/m2")]
    public float peakIrradiance = 1000f;

    [Header("Deskripsi")]
    [TextArea(2, 4)]
    public string description = "Deskripsi panel surya";
}
