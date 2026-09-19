using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// At runtime, keeps a collapsible section's actual height in sync with
/// its header + expandable content, and reports the correct preferred height
/// to the parent VerticalLayoutGroup so scrolling works correctly.
///
/// Intentionally NOT [ExecuteAlways]: the section is freely editable in the
/// Editor; the script only takes over during Play Mode.
/// </summary>
[RequireComponent(typeof(LayoutElement))]
public class DynamicSectionHeight : MonoBehaviour
{
    [Tooltip("Height of the always-visible header row (pixels).")]
    [SerializeField] private float headerHeight = 28f;

    [Tooltip("The collapsible content RectTransform whose preferred height is added when active.")]
    [SerializeField] private RectTransform contentRect;

    private LayoutElement _le;
    private RectTransform _rt;

    private void Awake()
    {
        _le = GetComponent<LayoutElement>();
        _rt = transform as RectTransform;
    }

    private void Update()
    {
        float target = CalcHeight();
        if (Mathf.Approximately(_le.preferredHeight, target)) return;

        // Update preferred height (read by Content's ContentSizeFitter for scroll)
        _le.preferredHeight = target;

        // Update actual height (read by Content's VLG for child positioning)
        _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, target);

        LayoutRebuilder.MarkLayoutForRebuild(_rt);
    }

    /// <summary>Header height + content preferred height when expanded.</summary>
    private float CalcHeight()
    {
        if (contentRect == null || !contentRect.gameObject.activeSelf)
            return headerHeight;

        return headerHeight + LayoutUtility.GetPreferredHeight(contentRect);
    }
}
