using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggles a content GameObject open/closed when the attached Button is clicked.
/// The triangle icon rotates: 0° when expanded (▼), -90° when collapsed (►).
/// Forces an immediate layout rebuild so the parent ScrollRect resizes without a 1-frame delay.
/// </summary>
[RequireComponent(typeof(Button))]
public class CollapsibleSection : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The content GameObject to show or hide.")]
    [SerializeField] private GameObject content;

    [Tooltip("RectTransform of the triangle icon to rotate on state change.")]
    [SerializeField] private RectTransform triangleIcon;

    [Header("Settings")]
    [SerializeField] private bool startExpanded = true;

    private bool _isExpanded;

    private void Start()
    {
        _isExpanded = startExpanded;
        ApplyState();

        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    /// <summary>Flips the expand/collapse state.</summary>
    public void Toggle()
    {
        _isExpanded = !_isExpanded;
        ApplyState();
    }

    private void ApplyState()
    {
        if (content != null)
            content.SetActive(_isExpanded);

        if (triangleIcon != null)
        {
            Vector3 rot = triangleIcon.localEulerAngles;
            rot.z = _isExpanded ? 0f : -90f;
            triangleIcon.localEulerAngles = rot;
        }

        // Force immediate layout rebuild so the parent scroll content resizes at once.
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
}
