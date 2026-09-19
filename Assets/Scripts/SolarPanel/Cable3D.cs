using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Cable3D : MonoBehaviour
{
    [Header("Waypoints Kabel")]
    [Tooltip("Isi otomatis dari child Transform jika dikosongkan")]
    [SerializeField] Transform[] waypoints;

    [Header("Spesifikasi Kabel")]
    [SerializeField] float cableRadius = 0.05f;
    [SerializeField] int radialSegments = 8;
    [SerializeField] bool smoothBend = true;
    [SerializeField] int smoothStepsPerSegment = 8;
    [SerializeField] Material cableMaterial;

    [Header("Referensi Panel")]
    [SerializeField] SolarPanel solarPanel;

    [Header("Elektron")]
    [SerializeField] int electronCount = 5;
    [SerializeField] float electronSpeed = 2f;
    [SerializeField] float electronRadius = 0.06f;
    [SerializeField] Color electronColor = new Color(1f, 0.9f, 0.2f);

    [Header("Simbol Elektron")]
    [SerializeField] bool showElectronSymbol = true;
    [Tooltip("Ukuran simbol dalam world units (bebas dari skala elektron)")]
    [SerializeField] float symbolWorldSize = 1.5f;
    [SerializeField] Color symbolColor = Color.black;

    List<Vector3> splinePoints = new List<Vector3>();
    List<float> cumulativeDistances = new List<float>();
    float totalLength;

    GameObject[] electrons;
    float[] electronProgress;
    Transform[] electronLabels;  // Billboard label "−" tiap elektron

    void Start()
    {
        // Auto-collect child Transforms sebagai waypoints jika array kosong
        if (waypoints == null || waypoints.Length < 2)
        {
            var childTransforms = new List<Transform>();
            foreach (Transform child in transform)
                childTransforms.Add(child);

            waypoints = childTransforms.ToArray();
        }

        if (waypoints.Length < 2)
        {
            Debug.LogWarning("[Cable3D] Butuh minimal 2 waypoints (child Transform).");
            return;
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (cableMaterial != null)
        {
            meshRenderer.material = cableMaterial;
        }
        else
        {
            // Fallback: pakai shader URP yang tersedia
            meshRenderer.material = CreateFallbackMaterial(Color.gray);
        }

        BuildSpline();
        BuildMesh();
        CreateElectrons();
    }

    /// <summary>
    /// Membuat material fallback yang kompatibel dengan render pipeline aktif.
    /// </summary>
    Material CreateFallbackMaterial(Color color)
    {
        // Coba shader URP Lit dulu, lalu fallback ke Standard / Sprites-Default
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Standard");

        var mat = new Material(shader != null ? shader : GraphicsSettings.defaultRenderPipeline != null
            ? GraphicsSettings.defaultRenderPipeline.defaultMaterial.shader
            : Shader.Find("Standard"));

        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))      mat.SetColor("_Color", color);
        return mat;
    }

    void Update()
    {
        // Elektron tidak muncul sebelum simulasi dijalankan.
        bool simulationRunning = SimulationManager.Instance != null
                                 && SimulationManager.Instance.IsSimulationRunning;

        if (!simulationRunning)
        {
            UpdateElectrons(false);
            return;
        }

        bool isPowered = solarPanel != null && solarPanel.CurrentPower > 0f;

        if (isPowered)
        {
            // Elektron hanya mengalir ketika ada furnitur yang sedang aktif mengonsumsi daya.
            bool hasFurnitureActive = false;
            if (FurnitureManager.Instance != null && SunController.Instance != null)
            {
                float currentHour = SunController.Instance.TimeOfDay;
                hasFurnitureActive = FurnitureManager.Instance.GetActiveWattConsumed(currentHour) > 0f;
            }
            isPowered = hasFurnitureActive;
        }

        UpdateElectrons(isPowered);
    }

#if UNITY_EDITOR
    /// Preview mesh di Scene view setiap kali nilai di Inspector berubah.
    void OnValidate()
    {
        if (!Application.isPlaying && waypoints != null && waypoints.Length >= 2)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                BuildSpline();
                BuildMesh();
            };
        }
    }
#endif

    // --- Spline ---

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    void BuildSpline()
    {
        splinePoints.Clear();
        cumulativeDistances.Clear();

        int n = waypoints.Length;

        if (smoothBend)
        {
            // Catmull-Rom spline � belokan halus
            for (int i = 0; i < n - 1; i++)
            {
                Vector3 p0 = waypoints[Mathf.Max(0, i - 1)].position;
                Vector3 p1 = waypoints[i].position;
                Vector3 p2 = waypoints[i + 1].position;
                Vector3 p3 = waypoints[Mathf.Min(n - 1, i + 2)].position;

                for (int j = 0; j < smoothStepsPerSegment; j++)
                {
                    float t = (float)j / smoothStepsPerSegment;
                    splinePoints.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }
        }
        else
        {
            // Linear � belokan tajam, langsung dari waypoint ke waypoint
            for (int i = 0; i < n - 1; i++)
            {
                splinePoints.Add(waypoints[i].position);
            }
        }

        splinePoints.Add(waypoints[n - 1].position);

        float dist = 0f;
        cumulativeDistances.Add(0f);
        for (int i = 1; i < splinePoints.Count; i++)
        {
            dist += Vector3.Distance(splinePoints[i], splinePoints[i - 1]);
            cumulativeDistances.Add(dist);
        }
        totalLength = dist;
    }

    // --- Mesh Tube ---

    void BuildMesh()
    {
        if (smoothBend)
            BuildMeshSmooth();
        else
            BuildMeshSharp();
    }

    /// Mesh tube untuk mode smooth � ring dibagi antar segmen, forward direction di-rata-rata di junction.
    void BuildMeshSmooth()
    {
        int pointCount = splinePoints.Count;
        int vertsPerRing = radialSegments + 1;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        Vector3[] rights = new Vector3[pointCount];
        Vector3[] ups = new Vector3[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 forward;
            if (i < pointCount - 1)
                forward = (splinePoints[i + 1] - splinePoints[i]).normalized;
            else
                forward = (splinePoints[i] - splinePoints[i - 1]).normalized;

            Vector3 refUp = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
            rights[i] = Vector3.Cross(forward, refUp).normalized;
            ups[i] = Vector3.Cross(rights[i], forward).normalized;
        }

        for (int i = 0; i < pointCount; i++)
        {
            float uvY = cumulativeDistances[i] / totalLength;

            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = (float)j / radialSegments * Mathf.PI * 2f;
                Vector3 offset = rights[i] * Mathf.Cos(angle) + ups[i] * Mathf.Sin(angle);
                vertices.Add(transform.InverseTransformPoint(splinePoints[i] + offset * cableRadius));
                uvs.Add(new Vector2((float)j / radialSegments, uvY));
            }
        }

        for (int i = 0; i < pointCount - 1; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int a = i * vertsPerRing + j;
                int b = a + 1;
                int c = (i + 1) * vertsPerRing + j;
                int d = c + 1;

                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        ApplyMesh(vertices, triangles, uvs);
    }

    /// Mesh tube untuk mode sharp � tiap segmen punya ring sendiri sehingga belokan tidak gepeng.
    void BuildMeshSharp()
    {
        int segCount = splinePoints.Count - 1;
        int vertsPerRing = radialSegments + 1;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int seg = 0; seg < segCount; seg++)
        {
            Vector3 p0 = splinePoints[seg];
            Vector3 p1 = splinePoints[seg + 1];
            Vector3 forward = (p1 - p0).normalized;

            Vector3 refUp = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Cross(forward, refUp).normalized;
            Vector3 up = Vector3.Cross(right, forward).normalized;

            float uvY0 = cumulativeDistances[seg] / totalLength;
            float uvY1 = cumulativeDistances[seg + 1] / totalLength;

            // ring awal segmen
            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = (float)j / radialSegments * Mathf.PI * 2f;
                Vector3 offset = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                vertices.Add(transform.InverseTransformPoint(p0 + offset * cableRadius));
                uvs.Add(new Vector2((float)j / radialSegments, uvY0));
            }

            // ring akhir segmen
            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = (float)j / radialSegments * Mathf.PI * 2f;
                Vector3 offset = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                vertices.Add(transform.InverseTransformPoint(p1 + offset * cableRadius));
                uvs.Add(new Vector2((float)j / radialSegments, uvY1));
            }

            // segitiga untuk segmen ini
            int baseIndex = seg * 2 * vertsPerRing;
            for (int j = 0; j < radialSegments; j++)
            {
                int a = baseIndex + j;
                int b = a + 1;
                int c = baseIndex + vertsPerRing + j;
                int d = c + 1;

                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        ApplyMesh(vertices, triangles, uvs);
    }

    void ApplyMesh(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        Mesh mesh = new Mesh { name = "Cable Mesh" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // --- Elektron ---

    void CreateElectrons()
    {
        electrons = new GameObject[electronCount];
        electronProgress = new float[electronCount];
        electronLabels = new Transform[electronCount];

        Material mat = CreateFallbackMaterial(electronColor);

        // Aktifkan emission jika shader mendukung
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", electronColor * 2.5f);
        }
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.9f);

        for (int i = 0; i < electronCount; i++)
        {
            GameObject electron = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            electron.name = $"Electron {i}";
            electron.transform.SetParent(transform);
            electron.transform.localScale = Vector3.one * electronRadius * 2f;
            electron.GetComponent<Renderer>().material = mat;
            Destroy(electron.GetComponent<Collider>());
            electron.SetActive(false);

            electrons[i] = electron;
            electronProgress[i] = (float)i / electronCount;

            // Tambah label "−" sebagai child (billboard)
            if (showElectronSymbol)
                electronLabels[i] = CreateElectronLabel(electron.transform);
        }
    }

    /// <summary>
    /// Membuat label TextMeshPro 3D berbentuk simbol minus (−) sebagai child elektron.
    /// </summary>
    Transform CreateElectronLabel(Transform parent)
    {
        GameObject labelObj = new GameObject("ElectronSymbol");
        labelObj.transform.SetParent(parent, false);
        labelObj.transform.localPosition = Vector3.zero;

        var tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = "\u2212";   // karakter minus tipografis (−)
        tmp.fontSize = 36f;    // font besar untuk kualitas crisp, ukuran diatur via localScale
        tmp.color = symbolColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 5f);

        // localScale dikompensasi dari scale parent (electron sphere),
        // sehingga world size label selalu = symbolWorldSize terlepas dari electronRadius
        // world scale = parent_world_scale * local_scale
        // parent_world_scale = electronRadius * 2  →  local_scale = symbolWorldSize / (electronRadius * 2) / (fontSize * 0.001f)
        // TMP 3D: 1 unit fontSize ≈ 0.001 world unit saat localScale = 1
        float localScale = symbolWorldSize / (electronRadius * 2f) / (36f * 0.001f);
        labelObj.transform.localScale = Vector3.one * localScale;

        return labelObj.transform;
    }

    void UpdateElectrons(bool isPowered)
    {
        if (electrons == null || totalLength <= 0f) return;

        Camera cam = Camera.main;

        for (int i = 0; i < electrons.Length; i++)
        {
            if (electrons[i] == null) continue;

            if (!isPowered)
            {
                electrons[i].SetActive(false);
                continue;
            }

            electrons[i].SetActive(true);
            electronProgress[i] = (electronProgress[i] + Time.deltaTime * electronSpeed / totalLength) % 1f;
            electrons[i].transform.position = GetPositionAtProgress(electronProgress[i]);

            // Billboard: hadapkan label ke kamera dan posisikan di permukaan depan sphere
            if (showElectronSymbol && electronLabels != null && electronLabels[i] != null && cam != null)
            {
                Vector3 towardCam = (cam.transform.position - electrons[i].transform.position).normalized;

                // Set world position: permukaan sphere + sedikit offset agar tidak terselip mesh
                electronLabels[i].position = electrons[i].transform.position
                    + towardCam * (electronRadius + 0.05f);

                // Rotasi label: forward mengarah ke kamera
                electronLabels[i].rotation = cam.transform.rotation;
            }
        }
    }

    /// <summary>
    /// Mengembalikan posisi di sepanjang kabel berdasarkan nilai progress 0�1.
    /// </summary>
    Vector3 GetPositionAtProgress(float t)
    {
        if (splinePoints.Count == 0) return transform.position;

        float targetDist = t * totalLength;

        for (int i = 1; i < cumulativeDistances.Count; i++)
        {
            if (cumulativeDistances[i] >= targetDist)
            {
                float segT = (targetDist - cumulativeDistances[i - 1]) /
                             (cumulativeDistances[i] - cumulativeDistances[i - 1]);
                return Vector3.Lerp(splinePoints[i - 1], splinePoints[i], segT);
            }
        }

        return splinePoints[splinePoints.Count - 1];
    }
}
