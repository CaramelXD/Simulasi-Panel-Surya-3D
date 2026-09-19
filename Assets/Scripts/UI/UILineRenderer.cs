using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Draws a smooth line through child RectTransform waypoints on a UI Canvas.
/// Uses Catmull-Rom spline interpolation for smooth curves.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : MaskableGraphic
{
    [Header("Line Settings")]
    [SerializeField] private float lineWidth = 2f;
    [SerializeField] private int smoothStepsPerSegment = 10;

    [Header("Waypoints")]
    [Tooltip("If empty, automatically uses child RectTransforms as waypoints.")]
    [SerializeField] private RectTransform[] waypoints;

    private List<Vector2> splinePoints = new List<Vector2>();

    protected override void OnEnable()
    {
        base.OnEnable();
        CollectWaypoints();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        CollectWaypoints();
        SetVerticesDirty();
    }
#endif

    /// <summary>
    /// Collects child RectTransforms as waypoints if none are explicitly assigned.
    /// </summary>
    private void CollectWaypoints()
    {
        if (waypoints != null && waypoints.Length >= 2) return;

        var children = new List<RectTransform>();
        foreach (Transform child in transform)
        {
            var rt = child.GetComponent<RectTransform>();
            if (rt != null)
                children.Add(rt);
        }
        waypoints = children.ToArray();
    }

    /// <summary>
    /// Forces a visual refresh of the line. Call this after moving waypoints at runtime.
    /// </summary>
    public void RefreshLine()
    {
        CollectWaypoints();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        CollectWaypoints();
        if (waypoints == null || waypoints.Length < 2) return;

        BuildSplinePoints();
        if (splinePoints.Count < 2) return;

        float halfWidth = lineWidth * 0.5f;

        for (int i = 0; i < splinePoints.Count - 1; i++)
        {
            Vector2 p0 = splinePoints[i];
            Vector2 p1 = splinePoints[i + 1];

            Vector2 dir = (p1 - p0).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * halfWidth;

            int baseIdx = vh.currentVertCount;

            vh.AddVert(p0 - normal, color, Vector4.zero);
            vh.AddVert(p0 + normal, color, Vector4.zero);
            vh.AddVert(p1 + normal, color, Vector4.zero);
            vh.AddVert(p1 - normal, color, Vector4.zero);

            vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
            vh.AddTriangle(baseIdx, baseIdx + 2, baseIdx + 3);
        }
    }

    /// <summary>
    /// Builds smooth Catmull-Rom spline points from waypoint positions.
    /// </summary>
    private void BuildSplinePoints()
    {
        splinePoints.Clear();

        int n = waypoints.Length;
        Vector2[] positions = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            if (waypoints[i] == null) return;
            positions[i] = GetLocalPosition(waypoints[i]);
        }

        for (int i = 0; i < n - 1; i++)
        {
            Vector2 p0 = positions[Mathf.Max(0, i - 1)];
            Vector2 p1 = positions[i];
            Vector2 p2 = positions[i + 1];
            Vector2 p3 = positions[Mathf.Min(n - 1, i + 2)];

            for (int j = 0; j < smoothStepsPerSegment; j++)
            {
                float t = (float)j / smoothStepsPerSegment;
                splinePoints.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        splinePoints.Add(positions[n - 1]);
    }

    /// <summary>
    /// Converts a waypoint's world position to local position relative to this RectTransform.
    /// </summary>
    private Vector2 GetLocalPosition(RectTransform target)
    {
        Vector3 worldPos = target.position;
        return rectTransform.InverseTransformPoint(worldPos);
    }

    /// <summary>
    /// Returns the local-space position at a given progress (0-1) along the spline.
    /// Useful for moving a marker along the line.
    /// </summary>
    public Vector2 GetPositionAtProgress(float progress)
    {
        CollectWaypoints();
        if (waypoints == null || waypoints.Length < 2) return Vector2.zero;

        BuildSplinePoints();
        if (splinePoints.Count < 2) return Vector2.zero;

        progress = Mathf.Clamp01(progress);

        // Build cumulative distances
        float totalLength = 0f;
        var distances = new List<float>(splinePoints.Count);
        distances.Add(0f);
        for (int i = 1; i < splinePoints.Count; i++)
        {
            totalLength += Vector2.Distance(splinePoints[i], splinePoints[i - 1]);
            distances.Add(totalLength);
        }

        if (totalLength <= 0f) return splinePoints[0];

        float targetDist = progress * totalLength;
        for (int i = 1; i < distances.Count; i++)
        {
            if (distances[i] >= targetDist)
            {
                float segT = (targetDist - distances[i - 1]) / (distances[i] - distances[i - 1]);
                return Vector2.Lerp(splinePoints[i - 1], splinePoints[i], segT);
            }
        }

        return splinePoints[splinePoints.Count - 1];
    }

    /// <summary>
    /// Converts a local-space position to world-space position.
    /// </summary>
    public Vector3 LocalToWorld(Vector2 localPos)
    {
        return rectTransform.TransformPoint(localPos);
    }

    /// <summary>
    /// Catmull-Rom spline interpolation for smooth curves.
    /// </summary>
    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
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
}
