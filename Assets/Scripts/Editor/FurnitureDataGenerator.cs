using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// [EDITOR ONLY] Otomatis buat FurnitureData dari objek di scene.
/// 1. Attach ke Empty GameObject
/// 2. Drag semua furniture dari Hierarchy ke array furnitureObjects
/// 3. Klik kanan komponen → "Generate All Furniture Data"
/// 4. Hasilnya ada di Assets/Data/
/// </summary>
public class FurnitureDataGenerator : MonoBehaviour
{
    [Header("Drag semua furniture dari Hierarchy ke sini")]
    public GameObject[] furnitureObjects;

    [Header("Nama furniture (urutan sama dengan di atas)")]
    public string[] displayNames;

    [Header("Konsumsi Watt (urutan sama)")]
    public float[] wattValues;

    [ContextMenu("Generate All Furniture Data")]
    public void GenerateAll()
    {
        string folder = "Assets/Data";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Data");

        int count = 0;
        for (int i = 0; i < furnitureObjects.Length; i++)
        {
            var obj = furnitureObjects[i];
            if (obj == null) continue;

            FurnitureData data = ScriptableObject.CreateInstance<FurnitureData>();

            // Nama
            string name = (i < displayNames.Length && !string.IsNullOrEmpty(displayNames[i]))
                ? displayNames[i]
                : obj.name;
            data.furnitureName = name;

            // Posisi, rotasi, scale
            data.fixedPosition = obj.transform.position;
            data.fixedRotation = obj.transform.eulerAngles;
            data.fixedScale    = obj.transform.localScale;

            // Watt
            data.wattConsumption = (i < wattValues.Length) ? wattValues[i] : 0f;

            // Cari prefab/model sumber
            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (sourcePrefab != null)
            {
                data.prefab3D = sourcePrefab;
                Debug.Log($"  → Prefab ditemukan: {AssetDatabase.GetAssetPath(sourcePrefab)}");
            }
            else
            {
                data.prefab3D = obj; // Fallback: pakai objek scene (harus dijadikan prefab manual)
                Debug.LogWarning($"  → {name}: tidak ada prefab sumber, assign manual nanti!");
            }

            data.description = $"{name} - {data.wattConsumption}W";

            string path = $"{folder}/{name}.asset";
            AssetDatabase.CreateAsset(data, path);
            count++;
            Debug.Log($"[Generator] ✅ {name} → pos:{data.fixedPosition} rot:{data.fixedRotation} scale:{data.fixedScale}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"\n[Generator] ========== SELESAI! {count} FurnitureData dibuat di {folder} ==========");
    }
}
#endif
