using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menambahkan efek gradien vertikal pada elemen UI (Image/Text).
/// </summary>
[AddComponentMenu("UI/Effects/Gradient")]
[RequireComponent(typeof(Graphic))]
public class UIGradient : BaseMeshEffect
{
    [Header("Gradient Colors")]
    public Color colorTop = Color.white;
    public Color colorBottom = new Color(0.8f, 0.8f, 0.8f); // Abu-abu terang

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        // Cari batas atas dan bawah dari elemen UI
        UIVertex vertex = new UIVertex();
        vh.PopulateUIVertex(ref vertex, 0);
        float bottomY = vertex.position.y;
        float topY = vertex.position.y;

        for (int i = 1; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float y = vertex.position.y;
            if (y > topY) topY = y;
            else if (y < bottomY) bottomY = y;
        }

        float uiElementHeight = topY - bottomY;

        // Terapkan warna gradien ke setiap sudut (vertex)
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            // Hitung posisi relatif vertikal (0 sampai 1)
            float normalizedY = (vertex.position.y - bottomY) / uiElementHeight;

            // Campurkan warna sesuai posisi
            vertex.color = Color.Lerp(colorBottom, colorTop, normalizedY);

            vh.SetUIVertex(vertex, i);
        }
    }
}