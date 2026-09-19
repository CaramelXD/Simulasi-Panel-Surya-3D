using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Membuat efek bayangan lembut (Soft Shadow) pada elemen UI 
/// dengan cara menduplikasi dan menumpuk mesh dengan transparansi bertahap.
/// </summary>
[AddComponentMenu("UI/Effects/Soft Shadow")]
[RequireComponent(typeof(Graphic))]
public class UISoftShadow : BaseMeshEffect
{
    [Header("Pengaturan Bayangan")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f); // Warna hitam transparan
    public Vector2 shadowOffset = new Vector2(3f, -3f);     // Arah jatuhnya bayangan

    [Header("Pengaturan Blur (Hati-hati Performa!)")]
    [Range(1, 5)]
    [Tooltip("Berapa lapis bayangan yang dibuat. Makin besar makin blur, tapi makin berat.")]
    public int blurIterations = 3;

    [Tooltip("Seberapa menyebar blurnya.")]
    public float blurSpread = 1.5f;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        List<UIVertex> verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);

        int originalCount = verts.Count;

        // Bersihkan vertex yang ada untuk kita gambar ulang dari belakang ke depan
        vh.Clear();

        // 1. Gambar lapisan bayangan (dari yang paling luar/pudar ke yang paling dalam/tebal)
        for (int i = blurIterations; i > 0; i--)
        {
            float ratio = (float)i / blurIterations;

            // Warna makin pudar di lapisan terluar
            Color currentColor = shadowColor;
            currentColor.a = shadowColor.a * (1f - ratio);

            // Posisi makin menyebar di lapisan terluar
            Vector2 currentSpread = shadowOffset + (shadowOffset.normalized * (blurSpread * i));

            ApplyShadowLayer(verts, currentColor, currentSpread, vh, originalCount);
        }

        // 2. Terakhir, gambar elemen UI aslinya (berada di lapisan paling atas)
        ApplyShadowLayer(verts, verts[0].color, Vector2.zero, vh, originalCount, true);
    }

    private void ApplyShadowLayer(List<UIVertex> verts, Color color, Vector2 offset, VertexHelper vh, int count, bool isOriginal = false)
    {
        int startIndex = vh.currentVertCount;

        for (int i = 0; i < count; i++)
        {
            UIVertex vt = verts[i];

            // Kalau ini bukan UI asli, timpa warnanya dan geser posisinya
            if (!isOriginal)
            {
                vt.color = color;
                vt.position.x += offset.x;
                vt.position.y += offset.y;
            }

            vh.AddVert(vt);
        }

        // Hubungkan titik-titik (vertices) menjadi segitiga (triangles)
        for (int i = startIndex; i < startIndex + count; i += 3)
        {
            vh.AddTriangle(i, i + 1, i + 2);
        }
    }
}