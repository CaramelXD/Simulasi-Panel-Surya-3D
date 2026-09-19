using UnityEngine;
using TMPro; // 1. TAMBAHKAN INI di paling atas untuk mengontrol teks TextMeshPro

public class ToggleChildrenOpacity : MonoBehaviour
{
    // Referensi ke Parent Empty GameObject yang membawahi dinding dan perabotan
    public GameObject roomParent;

    // 2. TAMBAHKAN INI: Slot untuk memasukkan teks tombol di Inspector
    public TextMeshProUGUI buttonText;

    private bool isTransparent = false;

    public void OnButtonClick()
    {
        // Ubah status
        isTransparent = !isTransparent;
        float targetAlpha = isTransparent ? 0.5f : 1.0f;

        // 3. LOGIKA BARU: Ganti teks tombol sesuai status opasitas saat ini
        if (buttonText != null)
        {
            buttonText.text = isTransparent ? "Opasitas Rumah: 50%" : "Opasitas Rumah: 100%";
        }

        // Ambil SEMUA komponen Renderer yang menempel pada
        // roomParent dan SELURUH anaknya ke bawah.
        Renderer[] allRenderers = roomParent.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in allRenderers)
        {
            // Ambil materialnya (otomatis ter-clone agar tidak merusak material asli di project)
            Material mat = rend.material;

            // Di URP, warna utama diakses via "_BaseColor", bukan "_Color"
            if (mat.HasProperty("_BaseColor"))
            {
                // Ambil warna saat ini
                Color color = mat.GetColor("_BaseColor");

                // Ubah nilai Alpha
                color.a = targetAlpha;

                // Terapkan warna baru
                mat.SetColor("_BaseColor", color);

                // Logika paksa shader URP ganti Surface Type secara runtime
                if (isTransparent)
                {
                    // Set shader ke mode Transparent
                    mat.SetFloat("_Surface", 1f); // 1 = Transparent, 0 = Opaque
                    mat.SetFloat("_Blend", 0f); // Alpha Blend
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0); // Matikan Z-Write supaya objek di belakangnya kelihatan
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
                else
                {
                    // Kembalikan shader ke mode Opaque (Normal)
                    mat.SetFloat("_Surface", 0f); // 0 = Opaque
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1); // Aktifkan Z-Write kembali
                    mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                }
            }
        }
    }
}