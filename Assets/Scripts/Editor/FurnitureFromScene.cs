using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool: Select objek furnitur di Hierarchy → klik menu 
/// "Tools/Buat Furniture Data dari Selection" → otomatis buat Prefab + FurnitureData
/// </summary>
public class FurnitureFromScene : Editor
{
    [MenuItem("Tools/Buat Furniture Data dari Selection")]
    static void CreateFurnitureFromSelection()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Pilih satu GameObject di Hierarchy dulu!", "OK");
            return;
        }

        // Cek apakah ini objek di scene (bukan prefab asset)
        if (!selected.scene.IsValid())
        {
            EditorUtility.DisplayDialog("Error", "Pilih objek di Scene/Hierarchy, bukan dari Project!", "OK");
            return;
        }

        string furnitureName = selected.name;

        // ── 1. Buat Prefab ──
        string prefabDir = "Assets/Prefabs/Furniture";
        if (!AssetDatabase.IsValidFolder(prefabDir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "Furniture");
        }

        string prefabPath = $"{prefabDir}/{furnitureName}.prefab";
        
        // Simpan posisi, rotasi, scale asli
        Vector3 originalPos = selected.transform.position;
        Vector3 originalRot = selected.transform.eulerAngles;
        Vector3 originalScale = selected.transform.localScale;

        // Reset posisi ke origin untuk prefab
        selected.transform.position = Vector3.zero;
        selected.transform.rotation = Quaternion.identity;

        // Buat prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(selected, prefabPath);

        // Kembalikan posisi asli di scene
        selected.transform.position = originalPos;
        selected.transform.rotation = Quaternion.Euler(originalRot);

        Debug.Log($"[FurnitureFromScene] Prefab dibuat: {prefabPath}");

        // ── 2. Buat FurnitureData ScriptableObject ──
        string dataDir = "Assets/Data";
        if (!AssetDatabase.IsValidFolder(dataDir))
            AssetDatabase.CreateFolder("Assets", "Data");

        string dataPath = $"{dataDir}/{furnitureName}.asset";

        // Cek apakah sudah ada
        FurnitureData existingData = AssetDatabase.LoadAssetAtPath<FurnitureData>(dataPath);
        if (existingData != null)
        {
            // Update existing
            existingData.furnitureName = furnitureName;
            existingData.prefab3D = prefab;
            existingData.fixedPosition = originalPos;
            existingData.fixedRotation = originalRot;
            existingData.fixedScale = originalScale;
            EditorUtility.SetDirty(existingData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[FurnitureFromScene] FurnitureData UPDATED: {dataPath}");
            EditorUtility.DisplayDialog("Berhasil!", 
                $"FurnitureData '{furnitureName}' sudah di-UPDATE!\n\n" +
                $"Prefab: {prefabPath}\n" +
                $"Data: {dataPath}\n\n" +
                $"Posisi: {originalPos}\n" +
                $"Rotasi: {originalRot}\n" +
                $"Scale: {originalScale}\n\n" +
                $"Jangan lupa set Watt Consumption!",
                "OK");
            
            Selection.activeObject = existingData;
            return;
        }

        // Buat baru
        FurnitureData data = ScriptableObject.CreateInstance<FurnitureData>();
        data.furnitureName = furnitureName;
        data.prefab3D = prefab;
        data.fixedPosition = originalPos;
        data.fixedRotation = originalRot;
        data.fixedScale = originalScale;
        data.wattConsumption = 100f; // Default, user ubah sendiri
        data.description = $"Furnitur: {furnitureName}";

        AssetDatabase.CreateAsset(data, dataPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FurnitureFromScene] FurnitureData dibuat: {dataPath}");

        // Select data yang baru dibuat
        Selection.activeObject = data;

        EditorUtility.DisplayDialog("Berhasil!", 
            $"FurnitureData '{furnitureName}' berhasil dibuat!\n\n" +
            $"Prefab: {prefabPath}\n" +
            $"Data: {dataPath}\n\n" +
            $"Posisi: {originalPos}\n" +
            $"Rotasi: {originalRot}\n" +
            $"Scale: {originalScale}\n\n" +
            $"Jangan lupa set Watt Consumption di Inspector!",
            "OK");
    }

    [MenuItem("Tools/Buat Furniture Data dari Selection", true)]
    static bool ValidateCreateFurniture()
    {
        return Selection.activeGameObject != null;
    }
}
