using UnityEngine;

[CreateAssetMenu(fileName = "NewBattery", menuName = "Simulator/Battery Data")]

public class BattData : ScriptableObject
{
    [Header("Prefab 3D")]
    [Tooltip("Prefab Battery yg akan dipasang")]
    public GameObject prefab3D;

    [Header("Kapasitas")]
    [Tooltip("Daya output per panel dalam Watt")]
    public float capacity = 10f;

    [Header("Nama")]
    [Tooltip("Daya output per panel dalam Watt")]
    public string battName = "Kapasitas 10f";

    [Header("Deskripsi")]
    [TextArea(2, 4)]
    public string description = "Deskripsi Battery";
}
